"""Eval harness CLI for the canonical chicken_ruckus_intro_v1 slice.

Drives a full 3-turn session end-to-end through a RuntimeSessionService and
archives the resulting artifacts and metadata to disk so the style/continuity
of generated cutscenes can be eyeballed.

Usage (fake-provider path is driven by tests that inject ``runtime_service``;
the real-provider path loads Settings from the environment):

    python -m scripts.eval_chicken_ruckus_slice \\
        --output-dir /tmp/eval_out --use-real-providers
"""

from __future__ import annotations

import argparse
import json
import shutil
import sys
import time
from dataclasses import asdict, dataclass, field
from datetime import UTC, datetime
from pathlib import Path
from typing import Any

from app.runtime_models import (
    PlayableTurnEnvelope,
    RuntimeSessionCreateRequest,
    TurnOutcomeRequest,
)
from app.runtime_service import RuntimeSessionService


@dataclass
class EvalTurnSummary:
    turn_index: int
    job_id: str
    turn_id: str
    generator_id: str
    character_name: str
    cutscene_display_name: str
    shot_subtitles: list[str]
    shot_image_prompts: list[str]
    style_anchors_used: list[str]
    provider_chosen: str
    artifacts_copied: list[str] = field(default_factory=list)


def run(
    *,
    output_dir: Path,
    story_type_id: str = "chicken_ruckus_intro_v1",
    style_preset_id: str = "watercolor_intro_v1",
    runtime_service: RuntimeSessionService | None = None,
    max_turns: int = 3,
    poll_interval_seconds: float = 1.0,
    poll_timeout_seconds: float = 300.0,
) -> list[EvalTurnSummary]:
    """Drive a full session end-to-end and write outputs to output_dir.

    If ``runtime_service`` is None, constructs the production service from
    environment-based Settings. Tests should inject a pre-built service with
    fake clients to avoid real provider calls.
    """
    service = runtime_service or _build_default_runtime_service()

    create_response = service.create_session(
        RuntimeSessionCreateRequest(
            story_type_id=story_type_id,
            max_turns=max_turns,
        )
    )
    session_id = create_response.session_id
    active_job_id: str = create_response.job_id

    output_dir.mkdir(parents=True, exist_ok=True)
    summaries: list[EvalTurnSummary] = []

    for turn_index in range(max_turns):
        job = _poll_until_ready(
            service,
            active_job_id,
            poll_interval_seconds=poll_interval_seconds,
            poll_timeout_seconds=poll_timeout_seconds,
        )
        envelope = service.get_turn_envelope(session_id, job.turn_id)
        if envelope is None:
            raise RuntimeError(
                f"Turn envelope missing for session={session_id} turn_id={job.turn_id}"
            )

        turn_dir = output_dir / f"turn_{turn_index + 1:03d}"
        summary = _archive_turn(
            turn_index=turn_index,
            job_id=job.job_id,
            envelope=envelope,
            turn_dir=turn_dir,
            style_preset_id=style_preset_id,
        )
        summaries.append(summary)

        outcome_response = service.submit_outcome(
            session_id,
            job.turn_id,
            TurnOutcomeRequest(
                result="success",
                score=1.0,
                completed_objective_count=1,
                notes="eval harness dummy success",
            ),
        )
        if outcome_response is None or not outcome_response.next_job_id:
            break
        active_job_id = outcome_response.next_job_id

    _write_top_manifest(
        output_dir=output_dir,
        story_type_id=story_type_id,
        style_preset_id=style_preset_id,
        session_id=session_id,
        summaries=summaries,
    )
    return summaries


def _build_default_runtime_service() -> RuntimeSessionService:
    """Assemble the production-wired RuntimeSessionService.

    Mirrors the factory logic in ``app.main.create_app`` without binding a
    FastAPI app. Requires env-based API keys for real providers; intended for
    the --use-real-providers CLI path only.
    """
    # Imports are local so the fake-provider test path never pays the cost of
    # building the production service or requiring env variables.
    from app.config import get_settings
    from app.generated_minigames import GeneratedMinigameBeatService
    from app.generated_storyboards import GeneratedStoryboardService
    from app.minigame_generators import MinigameGeneratorCatalog
    from app.runtime_store import RuntimeSessionStore
    from app.runtime_turn_generation import RuntimeTurnGenerationService
    from app.story_mode_config import RuntimeStoryModeConfigCatalog
    from app.story_sequence_turn_director import OpenAIStorySequenceTurnDirector

    settings = get_settings()
    base_dir = Path(__file__).resolve().parents[1]
    database_path = settings.resolve_database_path(base_dir)
    package_output_path = settings.resolve_path(
        base_dir, settings.generated_storyboard_package_path
    )
    storyboard_service = GeneratedStoryboardService.from_settings(settings, base_dir)
    minigame_service = GeneratedMinigameBeatService(package_output_path=package_output_path)
    minigame_generator_catalog = MinigameGeneratorCatalog.default()
    runtime_story_mode_catalog = RuntimeStoryModeConfigCatalog.default(
        catalog=minigame_generator_catalog
    )
    runtime_store = RuntimeSessionStore(database_path)
    runtime_root = (base_dir / "data" / "runtime").resolve()
    runtime_turn_director = OpenAIStorySequenceTurnDirector.from_settings(settings)
    generation_service = RuntimeTurnGenerationService(
        runtime_root=runtime_root,
        storyboard_service=storyboard_service,
        minigame_service=minigame_service,
        catalog=minigame_generator_catalog,
        default_voice_id=settings.elevenlabs_voice_id or "voice-test",
        turn_director=runtime_turn_director,
        story_mode_catalog=runtime_story_mode_catalog,
    )
    return RuntimeSessionService(
        store=runtime_store,
        generation_service=generation_service,
        story_mode_catalog=runtime_story_mode_catalog,
    )


def _poll_until_ready(
    service: RuntimeSessionService,
    job_id: str,
    *,
    poll_interval_seconds: float,
    poll_timeout_seconds: float,
):
    deadline = time.monotonic() + poll_timeout_seconds
    last_status = "<unknown>"
    while True:
        job = service.get_job(job_id)
        if job is None:
            raise RuntimeError(f"Runtime job '{job_id}' not found.")
        last_status = job.status
        if job.status == "ready":
            return job
        if job.status == "failed":
            raise RuntimeError(
                f"Runtime job '{job_id}' failed: {job.error_message or '<no error message>'}"
            )
        if job.status == "cancelled":
            raise RuntimeError(f"Runtime job '{job_id}' was cancelled.")
        if time.monotonic() >= deadline:
            raise TimeoutError(
                f"Runtime job '{job_id}' did not reach ready within "
                f"{poll_timeout_seconds:.1f}s (last status: {last_status})."
            )
        if poll_interval_seconds > 0:
            time.sleep(poll_interval_seconds)


def _archive_turn(
    *,
    turn_index: int,
    job_id: str,
    envelope: PlayableTurnEnvelope,
    turn_dir: Path,
    style_preset_id: str,
) -> EvalTurnSummary:
    turn_dir.mkdir(parents=True, exist_ok=True)

    artifacts_copied: list[str] = []
    for artifact in envelope.artifacts:
        source_path = Path(artifact.stored_path)
        if not source_path.exists():
            continue
        destination = turn_dir / source_path.name
        shutil.copy2(source_path, destination)
        artifacts_copied.append(destination.name)

    shot_subtitles = [shot.subtitle_text for shot in envelope.cutscene.shots]
    shot_image_prompts = _collect_shot_image_prompts(envelope)
    style_anchors_used = _collect_style_anchors(envelope)
    provider_chosen = _pick_primary_image_provider(envelope)

    summary = EvalTurnSummary(
        turn_index=turn_index,
        job_id=job_id,
        turn_id=envelope.turn_id,
        generator_id=envelope.debug.generator_id,
        character_name=envelope.debug.character_name,
        cutscene_display_name=envelope.cutscene.display_name,
        shot_subtitles=shot_subtitles,
        shot_image_prompts=shot_image_prompts,
        style_anchors_used=style_anchors_used,
        provider_chosen=provider_chosen,
        artifacts_copied=artifacts_copied,
    )

    manifest = {
        "turn_index": turn_index,
        "job_id": job_id,
        "turn_id": envelope.turn_id,
        "session_id": envelope.session_id,
        "generator_id": envelope.debug.generator_id,
        "character_name": envelope.debug.character_name,
        "story_type_id": envelope.debug.story_type_id,
        "prompt_structure_id": envelope.debug.prompt_structure_id,
        "cutscene": {
            "beat_id": envelope.cutscene.beat_id,
            "display_name": envelope.cutscene.display_name,
            "scene_name": envelope.cutscene.scene_name,
            "style_preset_id": envelope.cutscene.style_preset_id,
        },
        "minigame": {
            "generator_id": envelope.minigame.generator_id,
            "display_name": envelope.minigame.display_name,
            "scene_name": envelope.minigame.scene_name,
            "adapter_id": envelope.minigame.adapter_id,
            "objective_text": envelope.minigame.objective_text,
            "resolved_parameters": dict(envelope.minigame.resolved_parameters),
        },
        "shots": [
            {
                "shot_id": shot.shot_id,
                "subtitle_text": shot.subtitle_text,
                "narration_text": shot.narration_text,
                "duration_seconds": shot.duration_seconds,
                "image_asset_id": shot.image_asset_id,
                "audio_asset_id": shot.audio_asset_id,
                "alignment_asset_id": shot.alignment_asset_id,
            }
            for shot in envelope.cutscene.shots
        ],
        "shot_image_prompts": shot_image_prompts,
        "style_anchors_used": style_anchors_used,
        "requested_style_preset_id": style_preset_id,
        "provider_chosen": provider_chosen,
        "artifacts_copied": artifacts_copied,
    }
    (turn_dir / "manifest.json").write_text(
        json.dumps(manifest, indent=2, sort_keys=True),
        encoding="utf-8",
    )
    return summary


def _collect_shot_image_prompts(envelope: PlayableTurnEnvelope) -> list[str]:
    prompts_by_shot: dict[str, str] = {}
    for artifact in envelope.artifacts:
        if artifact.artifact_type != "image":
            continue
        raw_prompt = artifact.metadata.get("prompt") if isinstance(artifact.metadata, dict) else None
        if raw_prompt:
            prompts_by_shot[artifact.shot_id] = str(raw_prompt)
    return [prompts_by_shot.get(shot.shot_id, "") for shot in envelope.cutscene.shots]


def _collect_style_anchors(envelope: PlayableTurnEnvelope) -> list[str]:
    anchors: list[str] = []
    seen: set[str] = set()
    for artifact in envelope.artifacts:
        if artifact.artifact_type != "image":
            continue
        metadata = artifact.metadata if isinstance(artifact.metadata, dict) else {}
        raw_paths = metadata.get("reference_image_paths") or metadata.get("style_anchor_paths")
        if not isinstance(raw_paths, list):
            continue
        for path in raw_paths:
            path_str = str(path)
            if path_str and path_str not in seen:
                seen.add(path_str)
                anchors.append(path_str)
    return anchors


def _pick_primary_image_provider(envelope: PlayableTurnEnvelope) -> str:
    for artifact in envelope.artifacts:
        if artifact.artifact_type == "image" and artifact.provider_name:
            return artifact.provider_name
    return ""


def _write_top_manifest(
    *,
    output_dir: Path,
    story_type_id: str,
    style_preset_id: str,
    session_id: str,
    summaries: list[EvalTurnSummary],
) -> None:
    aggregated_anchors: list[str] = []
    seen_anchors: set[str] = set()
    providers: list[str] = []
    seen_providers: set[str] = set()
    for summary in summaries:
        for anchor in summary.style_anchors_used:
            if anchor not in seen_anchors:
                seen_anchors.add(anchor)
                aggregated_anchors.append(anchor)
        if summary.provider_chosen and summary.provider_chosen not in seen_providers:
            seen_providers.add(summary.provider_chosen)
            providers.append(summary.provider_chosen)

    manifest: dict[str, Any] = {
        "generated_at": datetime.now(UTC).isoformat(),
        "story_type_id": story_type_id,
        "style_preset_id": style_preset_id,
        "session_id": session_id,
        "style_anchors_used": aggregated_anchors,
        "providers_used": providers,
        "turns": [asdict(summary) for summary in summaries],
    }
    (output_dir / "manifest.json").write_text(
        json.dumps(manifest, indent=2, sort_keys=True),
        encoding="utf-8",
    )


def _main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Run the canonical chicken-ruckus slice end-to-end and archive the "
            "resulting cutscene artifacts and metadata for style verification."
        )
    )
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--story-type-id", default="chicken_ruckus_intro_v1")
    parser.add_argument("--style-preset-id", default="watercolor_intro_v1")
    parser.add_argument("--max-turns", type=int, default=3)
    parser.add_argument(
        "--use-real-providers",
        action="store_true",
        help=(
            "Construct the production RuntimeSessionService via Settings. "
            "Requires env-based API keys; real provider calls will be made."
        ),
    )
    parser.add_argument("--poll-interval-seconds", type=float, default=1.0)
    parser.add_argument("--poll-timeout-seconds", type=float, default=300.0)
    args = parser.parse_args(argv)

    if not args.use_real_providers:
        parser.error(
            "Refusing to run without --use-real-providers; this CLI does not "
            "bundle a fake provider path. Tests inject a fake service via run()."
        )

    summaries = run(
        output_dir=args.output_dir,
        story_type_id=args.story_type_id,
        style_preset_id=args.style_preset_id,
        max_turns=args.max_turns,
        poll_interval_seconds=args.poll_interval_seconds,
        poll_timeout_seconds=args.poll_timeout_seconds,
    )
    for summary in summaries:
        print(
            f"turn {summary.turn_index + 1:02d}: {summary.generator_id} "
            f"[{summary.cutscene_display_name}] "
            f"anchors={len(summary.style_anchors_used)} "
            f"provider={summary.provider_chosen or '<none>'}"
        )
    return 0


if __name__ == "__main__":
    sys.exit(_main())
