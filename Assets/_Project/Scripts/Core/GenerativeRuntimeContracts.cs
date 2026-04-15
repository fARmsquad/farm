using System;

namespace FarmSimVR.Core
{
    [Serializable]
    public sealed class GenerativeRuntimeSessionCreateResponse
    {
        public string session_id;
        public string job_id;
        public string status;
    }

    [Serializable]
    public sealed class GenerativeRuntimeJobSnapshot
    {
        public string job_id;
        public string session_id;
        public int turn_index;
        public string turn_id;
        public string status;
        public string error_message;
    }

    [Serializable]
    public sealed class GenerativeRuntimeSessionStateSnapshot
    {
        public int beat_cursor;
        public string[] world_state = Array.Empty<string>();
    }

    [Serializable]
    public sealed class GenerativeRuntimeTurnSummary
    {
        public string turn_id;
        public int turn_index;
        public string status;
        public string entry_scene_name;
        public string generator_id;
        public string character_name;
        public string summary;
    }

    [Serializable]
    public sealed class GenerativeRuntimeSessionDetail
    {
        public string session_id;
        public string status;
        public GenerativeRuntimeSessionStateSnapshot state;
        public GenerativeRuntimeJobSnapshot active_job;
        public GenerativeRuntimeTurnSummary last_ready_turn;
    }

    [Serializable]
    public sealed class GenerativeRuntimeJobStepSnapshot
    {
        public string step_name;
        public string status;
        public string error_message;
    }

    [Serializable]
    public sealed class GenerativeRuntimeTrackerSessionSummary
    {
        public string session_id;
        public string status;
        public string package_display_name;
        public string current_stage;
        public int ready_turn_count;
        public int max_turns;
        public string updated_at;
    }

    [Serializable]
    public sealed class GenerativeRuntimeTrackerJobDetail
    {
        public string job_id;
        public int turn_index;
        public string status;
        public string error_message;
        public string current_step_name;
        public GenerativeRuntimeJobStepSnapshot[] steps = Array.Empty<GenerativeRuntimeJobStepSnapshot>();
        public string updated_at;
    }

    [Serializable]
    public sealed class GenerativeRuntimeTrackerTurnDetail
    {
        public string turn_id;
        public int turn_index;
        public string character_name;
        public string generator_id;
        public string scene_name;
        public string objective_text;
        public string summary;
        public string[] image_asset_ids = Array.Empty<string>();
        public int artifact_count;
        public int fallback_artifact_count;
        public string created_at;
    }

    [Serializable]
    public sealed class GenerativeRuntimeTrackerSessionDetail
    {
        public string session_id;
        public string status;
        public string package_display_name;
        public int beat_cursor;
        public int max_turns;
        public string current_stage;
        public int ready_turn_count;
        public GenerativeRuntimeTrackerJobDetail active_job;
        public GenerativeRuntimeTrackerTurnDetail[] turns = Array.Empty<GenerativeRuntimeTrackerTurnDetail>();
        public string updated_at;
        public string created_at;
    }

    [Serializable]
    public sealed class GenerativeArtifactDescriptor
    {
        public string asset_id;
        public string artifact_type;
        public string beat_id;
        public string shot_id;
        public string mime_type;
    }

    [Serializable]
    public sealed class GenerativeCutsceneShotContract
    {
        public string shot_id;
        public string subtitle_text;
        public string narration_text;
        public float duration_seconds;
        public string image_asset_id;
        public string audio_asset_id;
        public string alignment_asset_id;
    }

    [Serializable]
    public sealed class GenerativeCutsceneContract
    {
        public string beat_id;
        public string display_name;
        public string scene_name;
        public string next_scene_name;
        public string style_preset_id;
        public GenerativeCutsceneShotContract[] shots = Array.Empty<GenerativeCutsceneShotContract>();
    }

    [Serializable]
    public sealed class GenerativeMinigameParameterEntry
    {
        public string Name;
        public string ValueType;
        public string StringValue;
        public int IntValue;
        public float FloatValue;
        public bool BoolValue;
    }

    [Serializable]
    public sealed class GenerativeMinigameContract
    {
        public string beat_id;
        public string display_name;
        public string scene_name;
        public string adapter_id;
        public string objective_text;
        public int required_count;
        public float time_limit_seconds;
        public string generator_id;
        public string minigame_id;
        public string[] fallback_generator_ids = Array.Empty<string>();
        public GenerativeMinigameParameterEntry[] resolved_parameter_entries = Array.Empty<GenerativeMinigameParameterEntry>();
    }

    [Serializable]
    public sealed class GenerativeContinuityContract
    {
        public string[] reference_image_asset_ids = Array.Empty<string>();
        public string[] world_state = Array.Empty<string>();
        public string[] present_character_names = Array.Empty<string>();
        public string prior_story_summary;
    }

    [Serializable]
    public sealed class GenerativePlayableTurnEnvelope
    {
        public string contract_version;
        public string session_id;
        public string turn_id;
        public string status;
        public string entry_scene_name;
        public GenerativeCutsceneContract cutscene;
        public GenerativeMinigameContract minigame;
        public GenerativeArtifactDescriptor[] artifacts = Array.Empty<GenerativeArtifactDescriptor>();
        public GenerativeContinuityContract continuity;
    }

    [Serializable]
    public sealed class GenerativeOutcomeResponse
    {
        public string next_job_id;
        public GenerativeRuntimeSessionSnapshot session_state;
    }

    [Serializable]
    public sealed class GenerativeRuntimeSessionSnapshot
    {
        public string session_id;
        public string status;
        public string active_job_id;
        public string last_ready_turn_id;
        public GenerativeRuntimeSessionStateSnapshot state;
    }
}
