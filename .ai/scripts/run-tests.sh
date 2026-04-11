#!/bin/bash
# Unity CLI Test Runner — FarmSim VR
set -e

MODE="${1:-all}"
PROJECT_PATH="$(cd "$(dirname "$0")/../.." && pwd)"
RESULTS_DIR="$PROJECT_PATH/TestResults"
mkdir -p "$RESULTS_DIR"

SKIPPED_EXIT_CODE=2

UNITY_PATH=$(find /Applications/Unity/Hub/Editor -name Unity -type f 2>/dev/null | head -1)
if [ -z "$UNITY_PATH" ]; then
    echo "ERROR: Unity editor not found in /Applications/Unity/Hub/Editor/"
    echo "Falling back to Unity Hub default..."
    UNITY_PATH="/Applications/Unity/Hub/Editor/6000.1.3f1/Unity.app/Contents/MacOS/Unity"
fi

run_unity_tests() {
    local platform="$1"
    local results_file="$2"
    local log_file="$3"

    rm -f "$results_file" "$log_file"

    echo "━━━ Running ${platform} Tests ━━━"
    set +e
    "$UNITY_PATH" \
        -batchmode -nographics -quit \
        -projectPath "$PROJECT_PATH" \
        -runTests \
        -testPlatform "$platform" \
        -testResults "$results_file" \
        -logFile "$log_file" \
        2>&1
    local unity_exit=$?
    set -e

    if [ -f "$results_file" ]; then
        python3 "$PROJECT_PATH/.ai/scripts/parse-test-results.py" "$results_file"
        return $?
    fi

    if [ -f "$PROJECT_PATH/Temp/UnityLockfile" ]; then
        echo "SKIPPED: Unity editor already has the project open."
        echo "Check $log_file for details"
        return $SKIPPED_EXIT_CODE
    fi

    if [ -f "$log_file" ] && grep -q "another Unity instance is running with this project open" "$log_file"; then
        echo "SKIPPED: Unity editor already has the project open."
        echo "Check $log_file for details"
        return $SKIPPED_EXIT_CODE
    fi

    if [ "$unity_exit" -eq 0 ]; then
        echo "WARNING: No ${platform} test results found"
    else
        echo "ERROR: Unity returned exit code $unity_exit for ${platform}"
    fi
    echo "Check $log_file for details"
    return 1
}

run_editmode() {
    run_unity_tests "EditMode" "$RESULTS_DIR/editmode-results.xml" "$RESULTS_DIR/editmode.log"
}

run_playmode() {
    run_unity_tests "PlayMode" "$RESULTS_DIR/playmode-results.xml" "$RESULTS_DIR/playmode.log"
}

case "$MODE" in
    editmode|edit)
        run_editmode
        ;;
    playmode|play)
        run_playmode
        ;;
    all)
        run_editmode
        run_playmode
        ;;
    *)
        echo "Usage: $0 [editmode|playmode|all]"
        exit 1
        ;;
esac
