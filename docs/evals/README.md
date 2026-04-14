# Town LLM Golden Eval

This golden set guards the playable Town dialogue slice against prompt or model
changes that would put payload formatting back on screen.

## Files

- `docs/evals/town-npc-golden-set.json`: case definitions and guardrails
- `docs/evals/town-npc-golden-example.generations.jsonl`: passing sample output
- `tools/evals/town_llm_golden_eval.py`: local evaluator

## Generation File Format

Provide one JSON object per line:

```json
{"id":"old-garrett-opening","output":"Well now, welcome to our village."}
```

The evaluator also accepts `text` instead of `output`.

## Run

```bash
python3 tools/evals/town_llm_golden_eval.py \
  --golden docs/evals/town-npc-golden-set.json \
  --generations docs/evals/town-npc-golden-example.generations.jsonl
```

## What It Checks

- no raw JSON keys like `"response"` or `"options"`
- no code fences or chat role labels
- per-case content guardrails
- repeat-question cases that demand a fresh angle instead of the same canned line
- shared-world grounding cases that mention connected people, places, or town history
- maximum sentence count per reply

Use it whenever Town prompts, model settings, or response parsing change.
