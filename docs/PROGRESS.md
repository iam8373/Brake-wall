# Break to Escape — Progress

Status words used here: *Implemented in code* · *Partially implemented* · *Present as a prototype* · *Designed but not implemented* · *Broken or incomplete* · *Not tested or unverified* · *Logic tested outside Unity* (pure C# logic that ran under `dotnet test`).

**Nothing has been compiled in Unity, run in a scene, built for Android or tested on a phone.** Every item below is also *Not tested or unverified* in Unity.

## Verification results

| Check | Result |
|---|---|
| Unity compile | **No** (Unity not available) |
| Scene generated and run | **No** |
| Android build | **No** |
| Device test | **No** |
| Type-check harness, baseline (untouched `codebase/`) | 75 compiler errors, all from the 10 broken string literals of P0-1. With only those literals fixed (scratch copy), 0 errors. |
| Type-check harness, final (`tools/verify/run.sh all`) | **0 errors, 0 warnings** in game code in all three configurations (player/legacy input, player/Input System, editor). Against hand-written stubs — not a Unity compile. |
| `dotnet test` (`tools/tests`) | **75 tests, 75 passed, 0 failed** |
| Economy JSON vs spreadsheet (`generate_economy_json.py --check`) | Matches |
| Pacing reproduction (`pacing_check.py`) | Matches the spreadsheet for all 6 bands; total 69.4 minutes |

### Harness error count per phase

| Phase | runtime | inputsystem | editor | Tests |
|---|---|---|---|---|
| 0 — baseline (original scripts) | 75 (original project) | — | — | — |
| 1–2 — core and runtime port | 0 | 0 | 0 | 66 passed |
| 2 — scene builder | 0 (1 stub gap fixed) | 0 | 0 | 66 passed |
| 3–4 — procedural generator, review fixes | 0 | 0 | 0 | 75 passed |
| Review passes | 0, 0 warnings | 0, 0 warnings | 0, 0 warnings | 75 passed |

Stub gaps found and fixed during the work (these were missing declarations in the stand-ins, not game bugs): `Mathf.Atan2`, `Rect` equality, `GameObject.GetComponents<T>`, `Transform` sibling methods, `EventSystem.RaycastAll`.

### Tests by area

| Area | Tests |
|---|---|
| Economy (costs ×70, effects ×70, formulas, bands, pacing, store state, equipment, first-upgrade time) | 9 |
| Punch patterns (counts, symmetry, charged cap, wall break, Rock Drill depth) | 7 |
| Voxel grid (damage, shell, chunk dirtying, edit/damage restore, index layout) | 6 |
| Grid ray traversal (axis, miss, negative axes, diagonal, inside solid, boundary, aim assist) | 7 |
| Mine layout (contract, directions and bands, determinism, treasure model) | 4 |
| Procedural mine (determinism, 8 seeds × contract and reachability) | 9 |
| Rubble pool and tank (idempotent return, settled-only sweep, 10,000-step stress, in-flight capacity, runs) | 6 |
| Limiters (punch cooldown, buffer, token bucket, rate meter) | 4 |
| Save (round trip, runs, atomic write + corruption recovery, interrupted write, garbage input, clamping, v1 migration, JSON) | 8 |
| Story and escape (content, placement vs text, all conditions, wrong code, restore, tutorial) | 6 |
| Monetization (release safety, development fallback, grace period, boost and cooldown, session cap, data) | 6 |
| Device tiers (classification, caps vs plan, thermal guard order) | 3 |

## Task checklist

### Phase 0 — orientation and tooling

| Task | Status |
|---|---|
| PROGRESS, DECISIONS, ASSET_LICENCES created | Done |
| Type-check harness with Unity stubs (`tools/verify`) | Done |
| Baseline run on the original scripts | Done (75 errors, all P0-1) |
| Read all documents and all 19 scripts + JSON | Done. New issues P1-12, P1-13, P2-14, P2-15, P3-6, P3-7 added to the status report |

### Phase 1 — P0

| ID | Status | Where |
|---|---|---|
| P0-1 Compile-breaking literals | Implemented in code; type-checked. Captions rewritten and moved to `story.json`; button labels use `\n` | `Resources/Data/story.json`, `Editor/SceneAutoBuilderEditor.cs` |
| P0-2 Validation scaffolding | Implemented: `README_UNITY_SETUP.md` with grouped import and what to send back | `UnityProject/README_UNITY_SETUP.md` |
| P0-3 Input | Implemented in code: single `PlayerInputSource`; legacy and Input System paths both type-check; builder adds the matching UI module | `Player/PlayerInputSource.cs` |
| P0-4 Scene builder | Implemented in code: layout object, placement from layout at build and at runtime, asset files, scene save, all screens and Settings, all buttons wired | `Core/World/MineLayout.cs`, `Editor/SceneAutoBuilderEditor.cs`, `Bootstrap/GameBootstrap.cs` |
| P0-5 Punch pattern wiring | Logic tested outside Unity: explicit shapes, real pattern level, charged rule, per-layer Rock Drill | `Core/World/PunchPatterns.cs`, `Player/PunchAction.cs` |

### Phase 2 — P1

| ID | Status |
|---|---|
| P1-1 Grid ray traversal; no per-frame wall physics; collider queue with budget, double-buffered collider mesh; lazy chunk build | Logic tested outside Unity (ray); queue Implemented in code |
| P1-2 Rubble pool states, idempotent return, settled-only sweep, in-flight tank reservations | Logic tested outside Unity |
| P1-3 Save v2 (all fields), atomic write + backup, migration, load order, background write | Logic tested outside Unity (codec, store, migration); Unity wiring Implemented in code |
| P1-4 Punch cooldown, vacuum token bucket, rate overlay, pacing script | Logic tested outside Unity; overlay Implemented in code |
| P1-5 Interactables, story text, escape chain, ending, credits | Logic tested outside Unity (state machine, content); scene objects Present as a prototype (primitive placeholders) |
| P1-6 Release-safe monetization, service interfaces, guarded adapters, guide | Logic tested outside Unity (rules); adapters Not tested or unverified (no SDKs) |
| P1-7 Equipment repair (cost + part), gauntlets from the locker, parts in the layout | Logic tested outside Unity |
| P1-8 Store: 7 cards + items, prices and disabled states from data | Implemented in code |
| P1-9 Preview cube size from the world; no 0.25 m left | Implemented in code (grep clean) |
| P1-10 Single frame-rate owner, URP tier settings, caps 100/200/400, pool sized by tier, guard order, manual override, detection logging | Logic tested outside Unity (policy); URP application Implemented in code |
| P1-11 URP vertex-colour shaders with shadow caster, fallback and error log | Implemented in code, Not tested or unverified (never compiled) |
| P1-12 (new) `UIHoldButton` lived in `MobileVirtualJoystick.cs`; Unity cannot save a MonoBehaviour whose file name differs, so PUNCH/VACUUM would lose their hold behaviour after the scene reloads | Implemented in code (own file); every MonoBehaviour file name now matches its class |
| P1-13 (new) Dynamite blast damage was 15, so a blast could not break any rock past band 1 | Implemented in code (500, from data; D12) |

### Phase 3 — P2 and P3

| ID | Status |
|---|---|
| P2-1 Pattern shapes | Logic tested outside Unity |
| P2-2 Caps from data, logging | Implemented in code |
| P2-3 Data-driven economy from the spreadsheet | Logic tested outside Unity |
| P2-4 One treasure system, modelled | Logic tested outside Unity (model in test and script) |
| P2-5 Mine layout per the design map | Logic tested outside Unity (layout contract) |
| P2-6 Safe area, mirrored look zone, ≥ 48 dp targets | Implemented in code |
| P2-7 Tool controller split, timings in the inspector | Implemented in code |
| P2-8 Explicit initialisation order | Implemented in code (`GameBootstrap`) |
| P2-9 Mesh hot loop, flat normals, UVs, index format | Implemented in code |
| P2-10 Tutorial from real actions, 2-minute rule logged | Logic tested outside Unity (sequence, time estimate) |
| P2-11 Audio safe with missing clips, material read before damage, clips by name | Implemented in code |
| P2-12 Tier detection tuning hooks | Implemented in code; thresholds in data, still to tune on phones |
| P2-13 Map with real bounds and "?" rooms | Implemented in code |
| P2-14 (new) Per-cube property blocks on rubble defeat batching | Implemented in code (shared material per voxel material) |
| P2-15 (new) Run time and day timer were not saved, so the ending time only counted since the last launch | Logic tested outside Unity (save round trip) |
| P3-1 Modern vibration API | Implemented in code |
| P3-2 Headlamp levels | Implemented in code |
| P3-3 Singletons | Kept for the prototype (recorded) |
| P3-4 UI text consistency | Done for all new text |
| P3-5 Ending and credits text | Done (original text in `story.json`) |
| P3-6 (new) Dynamite was thrown (16 m/s) and stuck to any collider, including rubble and the player | Implemented in code (reach-and-place on a grid-ray hit) |
| P3-7 (new) README mentions a ZIP and `original-scripts/` that are not in the repository | Recorded; README corrected |

### Phase 4 — systems without final assets

| Task | Status |
|---|---|
| Effects (dust, explosion and smoke, sparks, coins, motes), camera shake, FOV kick, walking bob, intensity slider, Reduce Flashing | Implemented in code |
| Animation polish (inspector timings and curves, readable thrust ≥ 0.07 s, charge wind-up, vacuum recoil, swap, headlamp gesture, reach-and-place dynamite) | Implemented in code |
| Headlamp levels and flicker | Implemented in code |
| Seeded procedural generator behind a flag | Logic tested outside Unity (determinism, reachability over 8 seeds) |
| Audio requirements and clip mapping | Done (`docs/ASSET_REQUIREMENTS.md`, `Resources/Data/audio.json`) |
| Model and UI art requirements | Done |
| Analytics events | Implemented in code (interface, event list, development logging) |

### Phase 5 — documentation

| Task | Status |
|---|---|
| `docs/ANDROID_BUILD_CHECKLIST.md` | Done |
| `UnityProject/README_UNITY_SETUP.md` | Done |
| Status report updated | Done |
| Root README updated | Done |

## Blockers and items not done

| Item | Reason |
|---|---|
| Unity compile, Play mode, Android build, device tests | No Unity editor or Android device in this environment. |
| Ads SDK, Play Billing, consent, analytics provider | SDKs cannot be installed here; adapters are written behind defines and not compiled. |
| Receipt verification for Remove Ads | Needs a key or server; marked `TODO(P1-6)`. |
| Real models, sounds, sprites, icon | Asset creation is outside code; requirements listed. |
| Performance numbers, rubble caps and thresholds tuned on phones | Needs devices. |
| URP quality levels | Unity has no public API to create quality levels; manual step documented. |
| Branch name `game-implementation` | The session's repository rules only permit the assigned branch (D3). |

## Review passes

| Pass | What was done |
|---|---|
| Review pass 1 (hostile code review) | Found and fixed: UI-over-button test unreliable with the Input System module (now a UI raycast); rubble colour property blocks defeating batching (shared materials); sweep refunds bypassing the income meter; static event handlers not removed on scene reload; dynamite blasts not applied to the world (subscription added); crusher roller axis; unused fields and usings; null guards in monetization. Re-ran harness, tests and greps. |
| Review pass 2 (owner importing into Unity) | Walked `README_UNITY_SETUP.md` against the files; checked every serialized reference the builder must assign; checked namespaces and folder paths; checked that each MonoBehaviour is in a file of the same name (split `UIHoldButton` out); checked the TMP and URP prerequisites are detected with clear messages. |
| Documentation pass | Status report, README, decisions and this file checked against the code. |
