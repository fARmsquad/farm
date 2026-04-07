#!/bin/bash
# Asset Import Check — validates 3D asset conventions
set -e

PROJECT_PATH="$(cd "$(dirname "$0")/../.." && pwd)"
ART_DIR="$PROJECT_PATH/Assets/_Project/Art"
ERRORS=0
WARNINGS=0
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo "━━━ ASSET IMPORT CHECK ━━━"

# Check naming convention
echo -e "\n${YELLOW}[1/3] Naming Convention${NC}"
find "$ART_DIR" -type f \( -name "*.fbx" -o -name "*.png" -o -name "*.tga" -o -name "*.wav" \) 2>/dev/null | while read -r file; do
    basename=$(basename "$file")
    # Check if file follows Category_Name_Variant pattern
    if ! echo "$basename" | grep -qE '^[A-Z][a-zA-Z]+_[A-Z]'; then
        echo -e "  ${YELLOW}⚠ Non-standard name: $basename${NC}"
        WARNINGS=$((WARNINGS+1))
    fi
done
echo -e "  ${GREEN}✓ Naming convention checked${NC}"

# Check texture sizes (rough check via file size)
echo -e "\n${YELLOW}[2/3] Texture Sizes${NC}"
find "$ART_DIR/Textures" -type f \( -name "*.png" -o -name "*.tga" \) -size +5M 2>/dev/null | while read -r file; do
    echo -e "  ${YELLOW}⚠ Large texture (>5MB): $(basename "$file")${NC}"
    WARNINGS=$((WARNINGS+1))
done
echo -e "  ${GREEN}✓ Texture sizes checked${NC}"

# Check for required directories
echo -e "\n${YELLOW}[3/3] Directory Structure${NC}"
for dir in Models Textures Audio Animations; do
    if [ ! -d "$ART_DIR/$dir" ]; then
        echo -e "  ${YELLOW}⚠ Missing directory: Art/$dir${NC}"
    fi
done
echo -e "  ${GREEN}✓ Directory structure checked${NC}"

echo -e "\n━━━━━━━━━━━━━━━━━━━━━"
echo -e "${GREEN}ASSET CHECK COMPLETE — $WARNINGS warnings${NC}"
