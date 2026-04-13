#!/bin/bash
# Unity CLI Test Runner — FarmSim VR
set -euo pipefail

SKIPPED_EXIT_CODE=2
MODE="${1:-all}"
PROJECT_PATH="$(cd "$(dirname "$0")/../.." && pwd)"
RESULTS_DIR="$PROJECT_PATH/TestResults"
ALLOW_PROJECT_COPY_FALLBACK="${FARMSIM_TEST_ALLOW_PROJECT_COPY:-1}"

mkdir -p "$RESULTS_DIR"

find_unity_path() {
    local unity_path

    unity_path=$(find /Applications/Unity/Hub/Editor -name Unity -type f 2>/dev/null | head -1 || true)
    if [ -n "$unity_path" ]; then
        echo "$unity_path"
        return 0
    fi

    echo "ERROR: Unity editor not found in /Applications/Unity/Hub/Editor/" >&2
    echo "Falling back to Unity Hub default..." >&2
    echo "/Applications/Unity/Hub/Editor/6000.1.3f1/Unity.app/Contents/MacOS/Unity"
}

UNITY_PATH="$(find_unity_path)"

batchmode_test_runner_method() {
    local platform="$1"

    case "$platform" in
        EditMode)
            echo "FarmSimVR.Editor.BatchmodeTestRunner.RunEditMode"
            ;;
        PlayMode)
            echo "FarmSimVR.Editor.BatchmodeTestRunner.RunPlayMode"
            ;;
        *)
            echo ""
            ;;
    esac
}

project_is_open_in_editor() {
    local project_path="$1"

    ps ax -o command= \
        | grep -F "/Unity.app/Contents/MacOS/Unity" \
        | grep -F -- "$project_path" \
        | grep -E -- "-project[Pp]ath" >/dev/null
}

unity_lock_error_logged() {
    local log_file="$1"

    [ -f "$log_file" ] && grep -q "another Unity instance is running with this project open" "$log_file"
}

copy_project_for_tests() {
    local platform="$1"
    local temp_root temp_dir temp_project
    local platform_slug

    temp_root="${TMPDIR:-/tmp}"
    platform_slug="$(printf '%s' "$platform" | tr '[:upper:]' '[:lower:]')"
    temp_dir="$(mktemp -d "${temp_root%/}/farmsimvr-${platform_slug}-tests.XXXXXX")"
    temp_project="$temp_dir/project"

    if command -v ditto >/dev/null 2>&1; then
        echo "Cloning disposable Unity test copy at $temp_project" >&2
        ditto --clone --noqtn --noextattr --noacl "$PROJECT_PATH" "$temp_project"
    else
        if ! command -v rsync >/dev/null 2>&1; then
            echo "ERROR: neither ditto nor rsync is available for project-copy test fallback." >&2
            return 1
        fi

        mkdir -p "$temp_project"
        echo "Creating disposable Unity test copy at $temp_project" >&2
        rsync -a \
            --exclude=".git/" \
            --exclude="Library/" \
            --exclude="Logs/" \
            --exclude="Temp/" \
            --exclude="TestResults/" \
            --exclude="obj/" \
            --exclude="Build/" \
            --exclude="Builds/" \
            --exclude=".vs/" \
            --exclude=".idea/" \
            "$PROJECT_PATH/" "$temp_project/"
    fi

    chmod -R u+rwX "$temp_project"
    rm -rf "$temp_project/Temp" "$temp_project/Logs" "$temp_project/TestResults"
    mkdir -p "$temp_project/Temp" "$temp_project/Logs" "$temp_project/TestResults"

    if [ -d "$temp_project/Library" ]; then
        rm -rf \
            "$temp_project/Library/Bee" \
            "$temp_project/Library/ScriptAssemblies"

        find "$temp_project/Library" -maxdepth 2 -type f \
            \( -name '*lock*' -o -name '*.index-lock' \) -delete
    fi

    echo "$temp_project"
}

cleanup_temp_project_copy() {
    local temp_project="$1"
    local temp_root
    local attempt

    if [ -z "$temp_project" ]; then
        return 0
    fi

    temp_root="$(dirname "$temp_project")"

    for attempt in 1 2 3 4 5 6 7 8 9 10; do
        rm -rf "$temp_root" 2>/dev/null && return 0
        sleep 1
    done

    echo "WARNING: Could not remove disposable Unity test copy at $temp_root" >&2
    return 0
}

run_unity_command() {
    local project_path="$1"
    local platform="$2"
    local results_file="$3"
    local log_file="$4"
    local execute_method

    execute_method="$(batchmode_test_runner_method "$platform")"
    if [ -z "$execute_method" ]; then
        echo "ERROR: Unsupported test platform '$platform'" >&2
        return 1
    fi

    rm -f "$results_file" "$log_file"

    set +e
    "$UNITY_PATH" \
        -batchmode -nographics \
        -projectPath "$project_path" \
        -logFile "$log_file" \
        -executeMethod "$execute_method" \
        -batchTestResults "$results_file" \
        2>&1
    local unity_exit=$?
    set -e

    return "$unity_exit"
}

parse_results_if_present() {
    local platform="$1"
    local results_file="$2"

    if [ -f "$results_file" ]; then
        python3 "$PROJECT_PATH/.ai/scripts/parse-test-results.py" "$results_file"
        return $?
    fi

    echo "WARNING: No ${platform} test results found"
    return 1
}

run_unity_tests() {
    local platform="$1"
    local results_file="$2"
    local log_file="$3"
    local run_project="$PROJECT_PATH"
    local copied_project=""
    local unity_exit=0

    echo "━━━ Running ${platform} Tests ━━━"

    if [ "$ALLOW_PROJECT_COPY_FALLBACK" != "0" ] && project_is_open_in_editor "$PROJECT_PATH"; then
        echo "Detected an active Unity editor for $PROJECT_PATH"
        copied_project="$(copy_project_for_tests "$platform")"
        run_project="$copied_project"
        echo "Running ${platform} tests against disposable copy: $run_project"
    fi

    if run_unity_command "$run_project" "$platform" "$results_file" "$log_file"; then
        unity_exit=0
    else
        unity_exit=$?
    fi

    if [ -n "$copied_project" ]; then
        cleanup_temp_project_copy "$copied_project"
    fi

    if [ -f "$results_file" ]; then
        parse_results_if_present "$platform" "$results_file"
        return $?
    fi

    if [ "$run_project" = "$PROJECT_PATH" ] \
        && [ "$ALLOW_PROJECT_COPY_FALLBACK" != "0" ] \
        && unity_lock_error_logged "$log_file"; then
        echo "Primary ${platform} run was blocked by the live editor. Retrying with a disposable copy."

        copied_project="$(copy_project_for_tests "$platform")"
        run_project="$copied_project"
        echo "Running ${platform} tests against disposable copy: $run_project"

        if run_unity_command "$run_project" "$platform" "$results_file" "$log_file"; then
            unity_exit=0
        else
            unity_exit=$?
        fi

        cleanup_temp_project_copy "$copied_project"

        if [ -f "$results_file" ]; then
            parse_results_if_present "$platform" "$results_file"
            return $?
        fi
    fi

    if [ "$run_project" = "$PROJECT_PATH" ] && unity_lock_error_logged "$log_file"; then
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

main() {
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
}

main "$@"
