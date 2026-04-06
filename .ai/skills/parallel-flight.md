# Skill: Parallel Flight Coordination

## Purpose
Support both single-agent and intentional parallel execution.

## Concepts
- **Flight**: A unit of work being executed by an agent
- **Slot**: A claimed position on the flight board
- **Lock**: A file-level lock preventing concurrent modification
- **Board**: .ai/coordination/flight-board.json — shared state

## Flight Board Schema
```json
{
  "flights": [
    {
      "id": "flight-001",
      "agent": "claude-code",
      "task": "implement crop growth calculator",
      "status": "active",
      "locked_paths": ["Assets/_Project/Scripts/Core/Farming/"],
      "started_at": "2026-04-06T10:00:00Z",
      "estimated_duration_minutes": 30
    }
  ],
  "completed": []
}
```

## Claiming a Slot
Before starting work:
1. Read flight-board.json
2. Check no active flight locks your target paths
3. If clear: add your flight, write board
4. If conflict: wait, or negotiate with the developer

## Releasing a Slot
After completing work:
1. Move flight from "flights" to "completed"
2. Remove path locks
3. Write board

## Path Lock Rules
- Locks are at the DIRECTORY level, not file level
- A lock on Core/Farming/ means NO other agent touches that directory
- Tests/EditMode/Farming/ is implicitly locked when Core/Farming/ is locked
- Agents CAN work on unrelated directories in parallel
