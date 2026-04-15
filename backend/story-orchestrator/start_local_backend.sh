#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
PYTHON_BIN="$ROOT_DIR/.venv/bin/python"
HOST="${1:-127.0.0.1}"
PORT="${2:-8012}"
LOG_PATH="${3:-/tmp/story-orchestrator-${PORT}.log}"

if [ ! -x "$PYTHON_BIN" ]; then
    echo "Missing Python virtualenv at $PYTHON_BIN" >&2
    exit 1
fi

cd "$ROOT_DIR"
nohup "$PYTHON_BIN" -m uvicorn app.main:app --host "$HOST" --port "$PORT" >>"$LOG_PATH" 2>&1 &
