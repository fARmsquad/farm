#!/bin/bash
# Flight Slot Manager — claim/release parallel agent slots
set -e

PROJECT_PATH="$(cd "$(dirname "$0")/../.." && pwd)"
BOARD="$PROJECT_PATH/.ai/coordination/flight-board.json"
ACTION="${1:-status}"
AGENT="${2:-claude-code}"
TASK="${3:-unknown}"
PATHS="${4:-}"

case "$ACTION" in
    claim)
        echo "Claiming flight slot for $AGENT: $TASK"
        python3 -c "
import json, datetime
with open('$BOARD') as f:
    board = json.load(f)
flight = {
    'id': f'flight-{len(board.get(\"completed\", [])) + len(board.get(\"flights\", [])) + 1:03d}',
    'agent': '$AGENT',
    'task': '$TASK',
    'status': 'active',
    'locked_paths': '$PATHS'.split(',') if '$PATHS' else [],
    'started_at': datetime.datetime.utcnow().isoformat() + 'Z',
    'estimated_duration_minutes': 30
}
board.setdefault('flights', []).append(flight)
with open('$BOARD', 'w') as f:
    json.dump(board, f, indent=2)
print(f'Claimed: {flight[\"id\"]}')
"
        ;;
    release)
        echo "Releasing flight slot for $AGENT"
        python3 -c "
import json
with open('$BOARD') as f:
    board = json.load(f)
flights = board.get('flights', [])
released = [f for f in flights if f['agent'] == '$AGENT']
board['flights'] = [f for f in flights if f['agent'] != '$AGENT']
board.setdefault('completed', []).extend(released)
with open('$BOARD', 'w') as f:
    json.dump(board, f, indent=2)
print(f'Released {len(released)} flight(s)')
"
        ;;
    status)
        echo "Flight Board Status:"
        python3 -c "
import json
with open('$BOARD') as f:
    board = json.load(f)
flights = board.get('flights', [])
if not flights:
    print('  No active flights')
else:
    for f in flights:
        print(f'  [{f[\"id\"]}] {f[\"agent\"]}: {f[\"task\"]} (locks: {f.get(\"locked_paths\", [])})')
"
        ;;
    *)
        echo "Usage: $0 [claim|release|status] [agent] [task] [paths]"
        exit 1
        ;;
esac
