#!/bin/bash
# AI Wiring Audit — validates harness internal consistency

set -e
ERRORS=0
YELLOW='\033[1;33m'
RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

PROJECT_PATH="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$PROJECT_PATH"

echo "━━━ AI WIRING AUDIT ━━━"

# 1. AGENTS.md exists and references valid workflows
echo -e "\n${YELLOW}[1/6] AGENTS.md references${NC}"
if [ -f "AGENTS.md" ]; then
    MISSING=0
    for wf in feature bugfix performance security deployment ai-architecture-change story-handoff; do
        if ! grep -q "$wf" AGENTS.md; then
            echo -e "  ${RED}✗ AGENTS.md missing reference to $wf workflow${NC}"
            ERRORS=$((ERRORS+1))
            MISSING=$((MISSING+1))
        fi
    done
    if [ $MISSING -eq 0 ]; then
        echo -e "  ${GREEN}✓ AGENTS.md workflow references OK${NC}"
    fi
else
    echo -e "  ${RED}✗ AGENTS.md not found${NC}"
    ERRORS=$((ERRORS+1))
fi

# 2. All referenced workflow files exist
echo -e "\n${YELLOW}[2/6] Workflow files${NC}"
WF_MISSING=0
for wf in feature bugfix performance security deployment ai-architecture-change story-handoff playtest-checkpoint; do
    if [ ! -f ".ai/workflows/$wf.md" ]; then
        echo -e "  ${RED}✗ Missing .ai/workflows/$wf.md${NC}"
        ERRORS=$((ERRORS+1))
        WF_MISSING=$((WF_MISSING+1))
    fi
done
if [ $WF_MISSING -eq 0 ]; then
    echo -e "  ${GREEN}✓ All workflow files present${NC}"
fi

# 3. All referenced agent files exist
echo -e "\n${YELLOW}[3/6] Agent files${NC}"
for agent in tdd-agent spec-writer implementer refactorer verifier architect security-agent xr-specialist finalizer; do
    if [ ! -f ".ai/agents/$agent.md" ]; then
        echo -e "  ${YELLOW}⚠ Missing .ai/agents/$agent.md${NC}"
    fi
done
echo -e "  ${GREEN}✓ Agent files checked${NC}"

# 4. All referenced skill files exist
echo -e "\n${YELLOW}[4/6] Skill files${NC}"
for skill in tdd-cycle story-lookup spec-driven-delivery unity-test-runner preflight git-finalization parallel-flight asset-pipeline scene-assembly quest-build-profile greybox-world glb-ingest; do
    if [ ! -f ".ai/skills/$skill.md" ]; then
        echo -e "  ${YELLOW}⚠ Missing .ai/skills/$skill.md${NC}"
    fi
done
echo -e "  ${GREEN}✓ Skill files checked${NC}"

# 5. All referenced scripts exist and are executable
echo -e "\n${YELLOW}[5/6] Script files${NC}"
for script in run-tests.sh parse-test-results.py preflight.sh finalize.sh flight_slot.sh git_finalize_guard.sh check_ai_wiring.sh asset_import_check.sh; do
    if [ ! -f ".ai/scripts/$script" ]; then
        echo -e "  ${YELLOW}⚠ Missing .ai/scripts/$script${NC}"
    elif [ ! -x ".ai/scripts/$script" ] && [[ "$script" == *.sh ]]; then
        echo -e "  ${YELLOW}⚠ .ai/scripts/$script not executable${NC}"
    fi
done
echo -e "  ${GREEN}✓ Script files checked${NC}"

# 6. Orchestrator consistency
echo -e "\n${YELLOW}[6/6] Orchestrator consistency${NC}"
ORCH_OK=1
if [ ! -f ".ai/memory/completion-learnings.md" ]; then
    echo -e "  ${RED}✗ .ai/memory/completion-learnings.md not found${NC}"
    ERRORS=$((ERRORS+1))
    ORCH_OK=0
fi
for orch in claude codex; do
    if [ -f ".ai/$orch.md" ]; then
        if ! grep -q "AGENTS.md" ".ai/$orch.md"; then
            echo -e "  ${RED}✗ .ai/$orch.md doesn't reference AGENTS.md${NC}"
            ERRORS=$((ERRORS+1))
            ORCH_OK=0
        fi
        if ! grep -q "SINGLE_SOURCE_OF_TRUTH" ".ai/$orch.md"; then
            echo -e "  ${RED}✗ .ai/$orch.md doesn't reference SINGLE_SOURCE_OF_TRUTH${NC}"
            ERRORS=$((ERRORS+1))
            ORCH_OK=0
        fi
        if ! grep -q "completion-learnings" ".ai/$orch.md"; then
            echo -e "  ${RED}✗ .ai/$orch.md doesn't reference completion-learnings.md${NC}"
            ERRORS=$((ERRORS+1))
            ORCH_OK=0
        fi
    fi
done
if [ $ORCH_OK -eq 1 ]; then
    echo -e "  ${GREEN}✓ Orchestrators consistent${NC}"
fi

echo -e "\n━━━━━━━━━━━━━━━━━━━━━"
if [ $ERRORS -gt 0 ]; then
    echo -e "${RED}WIRING AUDIT FAILED — $ERRORS issues${NC}"
else
    echo -e "${GREEN}WIRING AUDIT PASSED${NC}"
fi
exit $ERRORS
