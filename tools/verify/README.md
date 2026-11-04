# Verification harness

Unity cannot run in the environment where this code was written, so this folder checks the code as much as possible **without Unity**. Nothing here is a Unity build, a scene run or a device test.

## What it checks

| Tool | What it does | What it does **not** do |
|---|---|---|
| `run.sh` (type-check harness) | Compiles every game script against hand-written stubs of the Unity API in three configurations: player build with the legacy input path (`runtime`), player build with the new Input System (`inputsystem`), and editor build including the scene builder (`editor`). Catches syntax errors, unknown identifiers, wrong argument counts, missing members and type mismatches. | It does not prove the code compiles in Unity (the stubs can be wrong or too permissive), it never runs anything, and it cannot check serialization, shaders, scenes or behaviour. |
| `../tests` (`dotnet test`) | Runs the NUnit tests for the Unity-independent logic in `UnityProject/Assets/Scripts/Core` (economy, patterns, grid, ray traversal, rubble pool, limiters, save, escape chain, monetization rules, tiers, mine generators). The same test files are Unity edit-mode tests. | It does not test any MonoBehaviour, rendering, physics or input. |
| `generate_economy_json.py` | Generates `Assets/Resources/Data/economy.json` from the unchanged spreadsheet (standard library only) plus `economy_extras.json`. `--check` fails if the JSON is out of date. | It reads the spreadsheet's cached values; it does not recalculate formulas. |
| `pacing_check.py` | Reproduces the spreadsheet's Pacing sheet from `economy.json` and compares every band with the spreadsheet's own results; reports the treasure payout the spreadsheet does not model. | It is a model, not a playtest. |
| `Harness.Original.csproj` | Baseline: compiles the untouched first-draft scripts in `codebase/`. | — |

## How to run

```bash
# .NET 8 SDK required (https://dot.net). Then, from the repository root:
tools/verify/run.sh all                      # type-check, prints errors per configuration
tools/verify/run.sh original                 # baseline on codebase/ (expected to fail, see below)
(cd tools/tests && dotnet test)              # pure-logic tests
python3 tools/verify/generate_economy_json.py --check
python3 tools/verify/pacing_check.py
```

Logs go to `.verify-build/` (ignored by Git).

## Results recorded so far

See `docs/PROGRESS.md` for the per-phase table. Baseline on the original scripts: **75 compiler diagnostics, all caused by the 10 broken string literals of issue P0-1**; with only those literals fixed (in a scratch copy), the original scripts type-check against the stubs with 0 errors.

## Stub approximations

The stubs in `UnityStubs/` declare only what the game uses. Bodies are empty. Where an exact Unity signature was uncertain the stub follows the closest documented signature; these are the places a real Unity compile may still disagree:

| Stub | Approximation |
|---|---|
| `ParticleSystem` modules | Reduced to the properties used (`main`, `emission`, `shape`, `EmitParams`). Module structs are returned by value in Unity too; assigning their properties works on the real modules. |
| `UniversalRenderPipelineAsset` | Only `renderScale` and `shadowDistance`, both with setters (public in URP 17 as far as known). |
| `Mesh.SetIndices(List<int>, MeshTopology, int, bool, int)` | Matches the documented overload. |
| `UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches` | Stubbed as `IReadOnlyList<Touch>`; the real type is `ReadOnlyArray<Touch>` (same `Count` and indexer). |
| `UnityEditor.Events.UnityEventTools` | The overloads used (`AddPersistentListener`, `AddBoolPersistentListener`, `AddIntPersistentListener`). |
| `TMP_Settings.instance` | Used only to detect missing TextMeshPro essentials. |
| `Object` implicit `bool` | Mirrors Unity's "destroyed object compares to null" behaviour only at the type level. |
| `AndroidJavaObject` / `AndroidJavaClass` | Only the generic `Call`, `GetStatic`, `CallStatic` members. |

Files compiled only behind optional SDK defines (`ADMOB_SDK`, `UNITY_IAP`) are **not** compiled by the harness, because the SDK assemblies are not available. They are marked *Not tested or unverified* in their headers.
