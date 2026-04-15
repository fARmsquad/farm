#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
PYTHON_BIN="$ROOT_DIR/.venv/bin/python"
REQUIREMENTS_PATH="$ROOT_DIR/requirements.txt"
HOST="${1:-127.0.0.1}"
PORT="${2:-8012}"
LOG_PATH="${3:-/tmp/story-orchestrator-${PORT}.log}"
ENV_PATH="$ROOT_DIR/.env.local"

mkdir -p "$(dirname "$LOG_PATH")"
exec >>"$LOG_PATH" 2>&1

echo "=== launcher $(date -u +"%Y-%m-%dT%H:%M:%SZ") ==="
echo "ROOT_DIR=$ROOT_DIR HOST=$HOST PORT=$PORT"

resolve_system_python() {
    if command -v python3 >/dev/null 2>&1; then
        command -v python3
        return 0
    fi

    if command -v python >/dev/null 2>&1; then
        command -v python
        return 0
    fi

    return 1
}

ensure_virtualenv() {
    if [ -x "$PYTHON_BIN" ]; then
        return 0
    fi

    local system_python
    system_python="$(resolve_system_python)" || {
        echo "ERROR: Could not find python3 or python on PATH."
        return 1
    }

    echo "Creating local virtualenv with $system_python"
    rm -rf "$ROOT_DIR/.venv"
    "$system_python" -m venv "$ROOT_DIR/.venv"
}

ensure_requirements() {
    if "$PYTHON_BIN" -c "import fastapi, uvicorn, pydantic_settings, httpx" >/dev/null 2>&1; then
        echo "Python requirements already available."
        return 0
    fi

    echo "Installing backend requirements from $REQUIREMENTS_PATH"
    "$PYTHON_BIN" -m pip install -r "$REQUIREMENTS_PATH"
}

ensure_virtualenv || exit 1
ensure_requirements || exit 1

cd "$ROOT_DIR"
if [ -f "$ENV_PATH" ]; then
    set -a
    # shellcheck disable=SC1090
    source "$ENV_PATH"
    set +a
    echo "Loaded local provider config from $ENV_PATH"
else
    echo "WARNING: Missing provider config at $ENV_PATH"
fi

for key in OPENAI_API_KEY GEMINI_API_KEY ELEVENLABS_API_KEY; do
    if [ -z "${!key:-}" ]; then
        echo "WARNING: $key is not set."
    fi
done

nohup "$PYTHON_BIN" -m uvicorn app.main:app --host "$HOST" --port "$PORT" >>"$LOG_PATH" 2>&1 &
echo "Started story-orchestrator pid=$!"
