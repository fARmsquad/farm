# Contributing Guide

How-to guides for working in this project. Read the [README](README.md) for project overview and [SPECS.md](SPECS.md) for feature specs.

---

## Git Setup

### Git LFS (required)

This project uses Git LFS for all binary files (models, textures, audio, `.asset` files). You must install it before cloning.

```bash
# Install
brew install git-lfs   # macOS
git lfs install        # one-time setup

# Verify
git lfs version
```

If you see `git-lfs: command not found` errors, add Homebrew to your PATH:
```bash
export PATH="/opt/homebrew/bin:$PATH"
```

### Branch Naming

```
feature/<spec-id>-short-name     # new features
fix/<description>                # bug fixes
chore/<description>              # cleanup, docs, config
```

Examples:
```
feature/L2-001-soil-system
feature/L2-006-inventory
fix/crop-growth-negative-values
chore/update-readme
```

---

## Code Organization

### Assembly Definitions

The project uses Unity Assembly Definitions to enforce clean dependencies:

```
FarmSimVR.Core           --> Assets/_Project/Scripts/Core/
FarmSimVR.Interfaces     --> Assets/_Project/Scripts/Interfaces/
FarmSimVR.MonoBehaviours --> Assets/_Project/Scripts/MonoBehaviours/
FarmSimVR.Editor         --> Assets/_Project/Editor/
FarmSimVR.Tests.EditMode --> Assets/Tests/EditMode/
FarmSimVR.Tests.PlayMode --> Assets/Tests/PlayMode/
```

### Where to Put Code

| What you're writing | Where it goes | Assembly |
|---|---|---|
| Pure C# logic (no Unity API) | `Scripts/Core/Farming/` | FarmSimVR.Core |
| Interfaces shared between assemblies | `Scripts/Interfaces/` | FarmSimVR.Interfaces |
| MonoBehaviours (attach to GameObjects) | `Scripts/MonoBehaviours/` | FarmSimVR.MonoBehaviours |
| ScriptableObjects (data containers) | `Scripts/ScriptableObjects/` | FarmSimVR.Core |
| Editor-only tools and menu items | `Editor/` | FarmSimVR.Editor |
| Unit tests | `Tests/EditMode/` | FarmSimVR.Tests.EditMode |
| Play mode integration tests | `Tests/PlayMode/` | FarmSimVR.Tests.PlayMode |

### Dependency Rules

```
Core         --> no dependencies (pure C#)
Interfaces   --> no dependencies
MonoBehaviours --> can reference Core, Interfaces
Editor       --> can reference Core, MonoBehaviours, URP
Tests        --> can reference everything
```

**Core must stay pure.** No `using UnityEngine;` in Core classes. This keeps the farming logic testable without Play mode.

---

## Running the Scene Builder

The project includes an editor script that builds the farm scene from code. This is how the greybox layout (L1-001) and sky/lighting (L1-002) were created.

### Menu Items

| Menu | What it does |
|---|---|
| `FarmSim > Create Farm Scene (New)` | Creates a fresh scene with full L1 layout and saves to `Assets/_Project/Scenes/FarmMain.unity` |
| `FarmSim > Build Farm Layout (Greybox)` | Adds the farm layout to the current open scene (with confirmation dialog) |
| `FarmSim > Apply Sky & Lighting (L1-002)` | Applies skybox, sun, ambient, reflection probe, and shadow settings |

### Running from Command Line

You can also run the scene builder without opening the Unity GUI:
```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -projectPath "/path/to/farm" \
  -executeMethod "FarmSimVR.Editor.FarmSceneBuilder.CreateFarmScene" \
  -logFile /tmp/unity-build.log
```

---

## Running Tests

### From Unity

1. Open Unity
2. Window > General > Test Runner
3. Select **EditMode** tab
4. Click **Run All**

### From Command Line

```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -projectPath "/path/to/farm" \
  -runTests \
  -testPlatform EditMode \
  -testResults /tmp/test-results.xml \
  -logFile /tmp/unity-test.log \
  -batchmode \
  -nographics
```

### Writing Tests

Tests live in `Assets/Tests/EditMode/`. Follow existing patterns:

```csharp
using NUnit.Framework;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class MyFeatureTests
    {
        [Test]
        public void MethodName_Condition_ExpectedResult()
        {
            // Arrange
            var sut = new MyClass();

            // Act
            var result = sut.DoThing();

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}
```

**Test naming:** `MethodName_Condition_ExpectedResult` (e.g., `CalculateGrowth_WithMaxMoisture_ReturnsDoubleRate`)

---

## Working with Specs

See [SPECS.md](SPECS.md) for the full workflow. Quick reference:

### Claiming
1. Check SPECS.md for an `Open` spec whose dependencies are `Done`
2. Update the table: set Status to `Claimed`, add your name
3. Copy the spec to `Assets/Specs/Assignments/<YourName>/`

### Building
1. Create branch: `git checkout -b feature/<spec-id>-short-name`
2. Read the spec's Task Breakdown and implement in order
3. Write tests for Core/ classes
4. Check all Acceptance Criteria

### Submitting
1. Push your branch
2. Create a PR with the spec link in the body
3. Update SPECS.md status to `In Review`
4. After merge, update status to `Done`

---

## PR & Merge Workflow

```bash
# 1. Start from latest main
git checkout main && git pull

# 2. Create feature branch
git checkout -b feature/L2-001-soil-system

# 3. Do your work, commit often
git add <files>
git commit -m "[feature] implement soil state machine"

# 4. Push
git push -u origin feature/L2-001-soil-system

# 5. Create PR
gh pr create --title "[feature] implement L2-001 soil system" \
  --body "Implements Assets/Specs/Features/L2-001-soil-system.md"

# 6. After review approval, merge
gh pr merge --merge

# 7. Update SPECS.md status to Done
```

### Commit Message Format

```
[type] short description

Types:
  [feature]  - new functionality
  [fix]      - bug fix
  [tests]    - adding or updating tests
  [refactor] - code restructure, no behavior change
  [spec]     - adding or updating specs
  [chore]    - docs, config, cleanup
```

---

## Adding Art Assets

### 3D Models
- Format: `.glb` (preferred) or `.fbx`
- Place in: `Assets/_Project/Art/Models/Source/`
- Unity imports via glTFast automatically
- Keep source files under 10MB where possible (Quest budget)

### Textures
- Format: `.png` (transparency) or `.jpg` (opaque)
- Max resolution: 2048x2048 for Quest
- Place in: `Assets/_Project/Art/Textures/`

### Audio
- Format: `.wav` (SFX) or `.ogg` (music/ambient)
- Place in: `Assets/_Project/Art/Audio/`

All binary files are tracked by Git LFS via `.gitattributes`.

---

## Troubleshooting

### "git-lfs: command not found"
```bash
export PATH="/opt/homebrew/bin:$PATH"  # add to ~/.zshrc
```

### Unity won't open the project
- Make sure you have Unity **6000.4.1f1** (exact version matters)
- Install via Unity Hub > Installs > Add > Archive

### LFS pointer errors on merge
If you see "files that should have been pointers but weren't":
```bash
git lfs checkout
```
If that doesn't work, restore the files and re-commit:
```bash
git checkout HEAD -- <file>
```

### Tests won't compile
Check that assembly references are correct in the `.asmdef` file. The test assembly needs to reference whatever assembly contains the class you're testing.
