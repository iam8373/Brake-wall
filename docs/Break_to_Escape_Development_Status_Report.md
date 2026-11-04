# Break to Escape — Complete Development Status, Code Audit and Roadmap

**Report date:** 3 November 2026
**Scope:** documentation, verification and project-status reporting only. No game code was written or modified for this report.
**Target platform:** Android phones only (Google Play)
**Engine plan:** Unity 6.3 LTS, URP, C#, IL2CPP, ARM64

> **Headline status (read this first):** The game has a complete design package and a first-draft code package written by an AI assistant (Gemini "Argon"). **The code has never been compiled in Unity, the scene has never been generated or run, no Android build exists, and nothing has been tested on a phone.** Static inspection found **at least 10 compile-breaking errors**, a **scene builder that spawns the player inside solid rock**, and a **punch system that breaks only 1 cube per normal punch**. The package is a useful starting scaffold, not a playable prototype and not "100% complete".

---

## How to read this report

Every statement is tagged by where it comes from:

| Tag | Meaning |
|---|---|
| **[V]** | **Verified** by inspecting the actual files in the ZIP (reading, search, or scripted checks) |
| **[E]** | **Earlier AI review finding** (from a previous review of this project), re-checked here where stated |
| **[C]** | **Conclusion / recommendation** by the author of this report |
| **[G]** | **Gemini Argon's claim** in its own documents (not trusted until verified) |

Feature status uses exactly these labels:

- **Implemented in code** — code exists and appears to match the design (still not run)
- **Partially implemented**
- **Present as a prototype** — placeholder or simplified version
- **Designed but not implemented**
- **Broken or incomplete**
- **Not tested or unverified**

**Important:** *Nothing* in this project has been demonstrated in a running game. Every "Implemented in code" entry means "written", not "working". Every feature is also **Not tested or unverified** until Milestone 0 and 1 are passed.

### What could and could not be verified in this review

| Check | Result |
|---|---|
| Read all seven code-generation hand-off documents, the ZIP, and the three design-note text files (`argon.txt`, `argon2.txt`, `animation.txt`) | Done. All were available. |
| Unzipped the codebase and inspected the files | Done (see file inventory in section 5). |
| Compiled the code in Unity | **Not done.** No Unity editor or C# compiler was available in the review environment. |
| Ran the one-click scene builder | **Not done.** |
| Built an Android App Bundle or APK | **Not done.** |
| Tested on a real Android phone | **Not done.** |
| Static checks (text search, bracket balance, string-literal inspection) | Done. **A static check is not a build.** The bracket-balance check passed even though real compile errors exist. |
| Compared earlier draft versions of scripts against the ZIP versions | **Partly.** The ZIP is the source of truth. Several scripts were read in full from the ZIP; others were inspected by targeted search. Section 5 lists which. |
| Godot GDScript animation experiment | **No such experiment exists in the provided materials.** See section 11. |

---

## 1. Project Overview

**Break to Escape** is a first-person 3D incremental digging and destruction game for Android phones.

You are the last worker trapped in a collapsed underground mining complex (working name: Greywater Deep). You punch rock walls into physical cubes, vacuum the rubble, feed it to a crusher for cash, spend the cash at a surviving Company Store terminal on upgrades and restored equipment, dig through harder rock layers and find the way out.

**Core loop:** Punch → Break → Vacuum → Shred → Earn Money → Upgrade → Dig Deeper → Escape

**Current state in one sentence:** complete design and economy documents exist, roughly 4,300 lines of untested C# exist, and the project has not yet reached the first compile.

---

## 2. Original Game Vision

The original concept (preserved here): the player starts locked in a cell with nothing but fists, punches walls into cubes, vacuums rubble, shreds it for money, upgrades, digs through ever harder rock, and discovers what earlier prisoners left behind, with every new game being a new prison.

The intended game includes:

- Destructible voxel walls
- Normal and charged punches
- Physical rubble that flies, falls and bounces
- A vacuum system for collecting rubble
- A shredder/crusher that converts rubble into money
- An upgrade system for fists, vacuum, shredder and other equipment
- Increasingly difficult material layers
- Dynamite and a lantern/headlamp
- Hidden rooms, notes and discoveries from earlier occupants
- Treasure and valuable resources
- An exit with a combination-lock puzzle
- Procedurally generated prison layouts and replayability

**Identity decisions made during design:**

- Name: **Break to Escape**
- Setting: a forgotten underground mining complex (Greywater Deep)
- Story: a mining operation collapsed and sealed the network; the player must recover resources, restore equipment and find an escape route
- Pillars: destruction, resource collection, equipment progression
- Escape: restore generator power, recover a winch cable, and enter a three-number gate code found in three crew logbooks
- Monetization decided so far: rewarded ads plus a one-time Remove Ads purchase. Other methods deferred.

**Scope decision to note:** the design package scopes version 1 to **one hand-built mine**, with procedurally generated prisons ("New Shift") as a later update. The original vision still calls for procedural prisons, so this report keeps that as Milestone 4 rather than removing it.

**Mobile requirement:** landscape touch controls, Android performance tiers, and a good touch-screen experience are design requirements, not afterthoughts.

---

## 3. Reference Research and Design Decisions

### 3.1 Materials reviewed (in the requested order)

| # | Material | What it actually contains | Availability / depth of review |
|---|---|---|---|
| 1 | Hand-off document 1 | First-draft scripts: wireframe voxel preview, Crusher 07, rubble and vacuum manager, rubble cube | Read in full as text |
| 2 | Hand-off document 2 | First-draft voxel world (0.25 m cubes, 5 bands), dual-thumb controller, economy manager, exit-vault controller | Read in full as text |
| 3 | Hand-off document 3 | Save, audio and haptics, treasure and map, device tier scaler, monetization | Read in full as text |
| 4 | Hand-off document 4 | Virtual joystick, hold button, one-click scene builder | Read in full as text |
| 5 | Hand-off document 5 | Gemini's claims about the "upgraded" ZIP and a setup guide | Read in full |
| 6 | Hand-off document 6 | Explains the first (18-file) and second (upgraded) lists | Read in full |
| 7 | Hand-off document 7 | The 20-file list and Gemini's status summary ("100% complete") | Read in full |
| 8 | `Break_to_Escape_Full_Unity_Codebase.zip` | 19 C# files plus 1 JSON file | Unzipped and inspected (section 5). Gemini's text refers to a `_2.zip`; the supplied file has no `_2` in its name and is assumed to be the same package. |
| 9 | `argon.txt` | Gemini's analysis of the reference videos and its animation advice | Read in full |
| 10 | `argon2.txt` | Gemini's follow-up with the first-person tool controller and dynamite scripts | Read in full |
| 11 | `animation.txt` | Another assistant's answer on which elements need animation | Read in full |
| 12 | All scripts, JSON, builder | See section 5 | Inspected; no editor available |
| 13 | Five reference videos and seven screenshots | Viewed earlier in the project as frame contact sheets and images. Audio was **not** listened to: only loudness was measured (about -20 LUFS gameplay, about -14 LUFS trailer clip). Sound design in this report is therefore a requirements list, not a description of the reference. | Visual review only |

### 3.2 What the reference material established

The reference footage (a commercially released first-person wall-punching game) was used to understand **mechanics and pacing only**:

- Punches carve a shaped patch of cubes, with a wireframe preview of the cubes about to break
- Charged punches carve a larger patch
- Broken cubes become physical rubble that piles up on the floor
- A vacuum tool with a tank counter, a blow-out mode, and a floor shredder that pays out floating "+$" numbers
- Increasingly hard rock layers with different colours and lighting
- Hidden rooms and wall notes from earlier occupants, a day counter, tool hotbar, a hand-drawn map, and a combination-lock exit door

### 3.3 Design decisions already made

| Decision | Detail |
|---|---|
| Cube size | 0.5 m |
| Rock bands | 6 bands (hit points 10, 25, 60, 140, 260, 420; cube value $2, 3.50, 6, 10, 30, 70) |
| Upgrades | 7 lines × 10 levels, cost growth ×2.2 per level |
| Target run | About 60–90 minutes; the spreadsheet model predicts about 69 minutes |
| Animation | Code-driven transforms for rigid arm and tool movement; AI tools only for static models |
| Version 1 scope | One hand-built mine; procedural prisons later |
| Performance tiers | Low / Mid / High with rubble caps of 100 / 150–250 / 400 |
| World size | About 100 × 50 × 100 cubes (design) |

> **Important caveat [C]:** the 69-minute run-time prediction came from the spreadsheet model, which assumes 1.5–3.75 punches per second and vacuum and crusher throughput limits. **The code does not enforce those limits** (see issue P1-4), so the prediction does not currently apply to the built game.

---

## 4. What Has Been Completed

| Area | Completed | Evidence |
|---|---|---|
| Game Design Document | Written (17 sections) | Shared design document |
| Economy spreadsheet | Built with live formulas; recalculated without errors; 69-minute run | `Break_to_Escape_Economy.xlsx` |
| Technical budget and production plan | Written | Shared document |
| Touch-screen wireframes | 5 screens drawn | Shared design canvas |
| Animation approach decision | Code-driven chosen over AI skeletal animation | Section 11 |
| First-draft C# codebase | 19 scripts + 1 JSON, written by Gemini Argon | ZIP |
| Compilation in Unity | **Not done** | — |
| Scene generation and play test | **Not done** | — |
| Android build | **Not done** | — |
| Device test | **Not done** | — |
| Google Play account, store listing, ads/billing SDKs | **Not done** | — |

**Honest completion estimate [C]:** design and planning is substantially done. Implementation is at the "first untested draft" stage. Nothing has crossed the line into a verified, running feature.

---

## 5. Current Codebase and Architecture

### 5.1 File inventory (verified by unzipping)

19 C# files plus 1 JSON file.

**Size [V]:** 4,264 physical lines of C# (about 3,430 non-blank) plus 170 lines of JSON, about 4,434 lines in total. The earlier review's "about 4,400 lines" figure counts the JSON and blank lines.

| File | Lines | Role | Depth of inspection |
|---|---|---|---|
| `VoxelWorldManager.cs` | 336 | Voxel grid, mine generation, punch and dynamite damage | Read in full |
| `VoxelChunk.cs` | 93 | 16³ chunk face-culled mesh and collider | Read in full |
| `CompanyStoreEconomyManager.cs` | 333 | Upgrade tables, costs, cash, HUD, auto-punch | Read in full |
| `RubbleSuctionManager.cs` | 198 | Rubble pool, vacuum suction, blow-out, 60% sweep | Read in full |
| `RubbleCube.cs` | 83 | Single rubble cube, sleep, colour | Targeted search |
| `Crusher07Controller.cs` | 186 | Roller animation, crush queue, conveyor, cash popups | Targeted search |
| `FirstPersonToolController.cs` | 569 | Arms, punches, charge, hotbar, vacuum pose, dynamite, headlamp | Targeted search and excerpts |
| `StickyDynamite.cs` | 67 | Pooled thrown dynamite, fuse, blast event | Targeted search |
| `VoxelWireframePreview.cs` | 168 | Procedural wireframe punch preview | Targeted search |
| `MobileFirstPersonController.cs` | 100 | Walk and look with touch | Targeted search |
| `MobileVirtualJoystick.cs` | 65 | Joystick and hold-button helpers | Targeted search |
| `SaveManager.cs` | 280 | GZip JSON save, auto-save | Excerpts and targeted search |
| `AudioHapticsManager.cs` | 238 | Material punch sounds, pitch scaling, Android haptics | Targeted search |
| `TreasureAndMapManager.cs` | 297 | Buried treasure, detector radar, map and room discovery | Targeted search |
| `ExitVaultAndLogbookController.cs` | 209 | Logbooks, lift-gate code and escape sequence | Targeted search |
| `UIScreenAndAccessibilityManager.cs` | 188 | Five-screen switching, left-handed mode, tutorial | Targeted search and excerpts |
| `DeviceTierScaler.cs` | 108 | Tier detection, caps, frame-rate guard | Read in full |
| `MonetizationServicesManager.cs` | 126 | Rewarded-ad and purchase placeholders | Read in full |
| `SceneAutoBuilderEditor.cs` | 620 | Editor-only one-click scene builder | Targeted search and an earlier draft version |
| `economy_config.json` | 170 | Economy data | Parsed; fields checked |

### 5.2 Planned architecture versus what was actually written

| Planned decision | Reflected in the source? | Notes |
|---|---|---|
| Unity, URP, C# | Code targets Unity 6 APIs | Uses Unity 6 / 2023+ APIs such as `rb.linearVelocity` and `FindFirstObjectByType`. It will not compile on Unity 2022. **[V]** |
| IL2CPP, ARM64 | Not in code | Manual Player Settings; not yet configured |
| Byte-array voxel grid | **Yes** | `byte[,,]` ids plus `float[,,]` damage. **[V]** |
| 16³ chunks | **Yes** | **[V]** |
| Face-culled chunk meshes | **Yes** | No UVs, so no textures. Colours come from vertex colours. **[V]** |
| Burst jobs for chunk rebuilds | **No** | Rebuilds run synchronously on the main thread. **[V]** |
| Voxel types with hardness, value and drop info | Partly | Hardness and value exist; there is no real drop table. **[V]** |
| Pooled rubble | **Yes** | 400 cubes prewarmed; has a pool-corruption bug (P1-2). **[V]** |
| GPU instancing | Partly | Instancing flag plus per-cube colour through a property block. Not verified in a running scene. |
| Device-specific rubble limits | Yes, but wrong values | 200 / 350 / 500 instead of 100 / 150–250 / 400. **[V]** |
| Sleeping settled rubble | **Yes** | Sleeps after a 0.6 s settle. |
| Scripted vacuum movement | **Yes** | Bezier curve into the nozzle. **[V]** |
| Numbers in data files | **No** | Economy tables are hard-coded in C#. `economy_config.json` is **not read by any script**. **[V]** |
| Modules: input, world generation, voxels, tools, rubble, economy, narrative, UI, save, audio, services | Mostly present as classes | `FirstPersonToolController` is one 569-line class mixing input, animation, aiming, dynamite and hotbar. World generation and damage sit inside `VoxelWorldManager`. Many singletons and static events. **[V]** |
| Seed-based procedural prison | **No** | Hand-built. A `seed` field exists in the save class but is unused. **[V]** |

**Architecture conclusion [C]:** the broad structure matches the plan, and the planned fast path (data grid, chunk meshes, pooled rubble) is mostly there. The main deviations are: no data-driven tables, no job-based mesh building, a wall collision strategy that will not scale on phones, and a few oversized classes. None of this requires a rewrite. See section 10.

---

## 6. Features Implemented

"Implemented" here means **written in code**. **None has been run.**

| Feature | Status | Notes |
|---|---|---|
| Voxel grid, chunking, face-culled meshes | Implemented in code | 96 × 32 × 96 cubes (6 × 2 × 6 chunks). **Not tested or unverified.** |
| 6 material bands + 3 treasure materials | Implemented in code | Hit points and values match the spreadsheet. Bands are rings around the centre, not the designed directional layout. |
| Upgrade tables and cost formula | Implemented in code | All seven tables match the spreadsheet; cost = base × 2.2^(level−1); base costs 150 / 200 / 250 / 175 / 125 / 225 / 375 match. |
| Crusher with rate-limited queue and floating cash | Implemented in code | Rate comes from the crusher table. |
| Vacuum suction on a Bezier curve | Implemented in code | Rate is not limited (see P1-4). |
| Blow-out mode | Implemented in code | Tank count and stack can disagree after loading (P1-3). |
| 60% sweep-up bonus | Implemented in code | 60% ratio in code; has a double-pay bug (P1-2). |
| Rubble pooling and sleeping | Implemented in code | 400 prewarmed; has the P1-2 bug. |
| Dynamite throw, stick, fuse, blast, treasure destruction | Implemented in code | No particle effects. Dynamite removes treasure cubes without spawning rubble. |
| Hotbar and tool switching | Implemented in code | Wired by the scene builder. |
| Touch joystick, look drag, hold buttons | Implemented in code | Uses the legacy input API (P0-3). |
| Cash / income / day / tank HUD and caption banner | Implemented in code | Created by the scene builder. |
| First-person arm, vacuum and dynamite animation (code-driven) | Implemented in code | See section 11. |
| Aim assist (sphere cast) | Implemented in code | Physics-based (P1-1). |
| Auto-punch upgrade | Implemented in code | Fires synthetic button presses. Not rate-limited. |
| Treasure uncovering and pickup, detector radar | Implemented in code | No buy button or map UI is created by the builder. |

---

## 7. Features Partially Implemented

| Feature | Status | What is missing or wrong |
|---|---|---|
| Normal punch | **Broken or incomplete** | Passes "1" as the cube count, so a normal punch breaks **1 cube** (P0-5). |
| Charged punch | **Broken or incomplete** | Passes 2 or 3, producing about 4–6 cubes at most. |
| Punch Pattern upgrade (5→97 cubes) | **Broken or incomplete** | Changes only the preview radius. Even once wired, levels 5, 7, 9 and 10 would give fewer cubes than the table says (P2-1). |
| Rock Drill (3-deep tunnel) | **Broken or incomplete** | Depth loop is effectively dead, and no code unlocks the drill (P1-7). |
| Restored equipment (Pneumatic Hammer, Rock Drill, Conveyor) | **Broken or incomplete** | The flags are never set. Work Gauntlets are auto-granted at Punch Power level 3 instead of being a story unlock. |
| Company Store UI | **Partially implemented** | Buttons exist for only 4 of 7 upgrade lines (Power, Pattern, Tank, Crusher). Speed, Suction and Ore Sense have code but no buttons. Prices are not displayed. |
| Five wireframe screens | **Partially implemented** | The manager class and references exist. The builder creates only the HUD and the Store panel. No Map, Pause or Ad-offer panels are created, and the manager is never added to the scene. |
| Left-handed mode | **Partially implemented** | Mirrors rectangles only if they are assigned (they are not). The right-thumb look zone is not mirrored. |
| Dispatch tutorial (5 steps) | **Broken or incomplete** | Does not compile (P0-1); the manager is not in the scene. |
| Save / load | **Broken or incomplete** | See P1-3. |
| Lift escape (power + cable + code) | **Broken or incomplete** | Logic is written, but does not compile, and nothing ever calls the repair/recover/logbook functions (P1-5). |
| Map and room discovery | **Present as a prototype** | Room bounds use old coordinates (P0-4). No map UI is built. |
| Audio and haptics | **Partially implemented** | Code exists. **No audio clips are assigned or included.** Haptics use native Android calls; untested. |
| Device tier scaler | **Partially implemented** | Wrong cap values; some settings have no effect in URP (P1-10). |
| Monetization | **Present as a prototype** | Placeholders only, with unsafe fallbacks (P1-6). |
| Headlamp | **Present as a prototype** | A toggled spotlight. The design called for a lantern or headlamp with levels. |
| One-click scene builder | **Broken or incomplete** | Compile errors; wrong coordinates; nothing saved (P0-1, P0-4). |
| Procedural prison generation | **Designed but not implemented** | Hand-built only; no seed. |
| Prisoner rooms, notes, story text | **Designed but not implemented** | Rooms are empty boxes carved in rock; no logbook text exists. |
| Particles, camera shake, FOV kick, walking bob | **Designed but not implemented** | No `ParticleSystem` anywhere; no FOV or camera-bob code. |
| Analytics and crash reporting | **Designed but not implemented** | — |
| Ads SDK and Play Billing | **Designed but not implemented** | Only placeholder hooks. |

---

## 8. Important Code Problems and Technical Risks

### 8.1 Verification of the earlier review's specific claims

| Earlier claim | Verdict | What was found |
|---|---|---|
| **A.** About 4,400 lines were generated and never compiled or tested | **Confirmed, and worse.** | 4,264 lines of C# (4,434 including JSON). Static inspection found **at least 10 definite compile errors in 3 files**. The scene builder was never run. |
| **B.** Old input API and old UI input module | **Confirmed.** | `Input.touchCount`, `Input.GetTouch` and `Input.GetAxisRaw` appear in `MobileFirstPersonController`; the builder creates a `StandaloneInputModule`. |
| **C.** Saved dug-out walls are not restored on load | **Confirmed**, with more problems found. | See P1-3. |
| **D.** Every punch rebuilds wall collision; aim and preview run physics every frame | **Confirmed.** | See P1-1. |
| **E.** Rubble caps 200/350/500 vs plan 100/150–250/400 | **Confirmed.** | The manager's own default is 150, and 400 objects are always prewarmed. |
| **F.** Ads/IAP placeholders and unsafe fallbacks | **Confirmed.** | The double-cash duration in code is 120 s (the design says 300 s). The JSON says 300 s with a 600 s cooldown, but **the JSON is never read**. |
| **G.** World 96 × 32 × 96 vs 100 × 50 × 100 | **Confirmed.** | 6 × 2 × 6 chunks of 16 cubes. |
| **H.** No audio, models, story, ads or billing | **Confirmed.** | See section 9. |
| "19 C# files" | Confirmed | Plus 1 JSON. |
| "JSON passed validity check" | Confirmed | **But it is unused by every script.** |
| "Balanced brackets in all 19 files" | Confirmed | **But this check is weak.** The files balance while still containing compile errors. |
| "0.5 m cubes" | Confirmed in the world | **The wireframe preview defaults to 0.25 m and the builder never sets it** (P1-9). |
| "Six rock bands" | Confirmed (plus 3 treasure types) | Laid out as concentric rings plus a north strip. |
| "Seven upgrade tables, ×2.2 cost" | Confirmed | Only 4 lines have UI buttons. |
| "60% sweep-up" | Confirmed | Double-pay bug exists. |
| "Three-part lift escape" | Logic present | Unreachable and non-compiling. |
| "Five UI screens incl. left-handed mode" | **Partially true** | Manager code only; panels not built. |
| "World size 96 × 32 × 96" | Confirmed | — |

### 8.2 Contradictions between Gemini Argon's documents and the actual code

| Gemini's claim **[G]** | Actual finding **[V]** |
|---|---|
| "Core C# Gameplay Codebase (100% Complete)" | Never compiled; compile errors exist; many systems unreachable |
| `economy_config.json` allows "runtime JSON overrides without recompiling" | No script reads it |
| RubbleSuctionManager "enforces the device-tier rubble caps (100 Low / 150–250 Mid / 400 High)" | DeviceTierScaler sets 200 / 350 / 500; the manager's default is 150 |
| Includes "all Restored Equipment story unlocks" | The Hammer, Drill and Conveyor flags are never set |
| Rock Drill "carves a 3-voxel-deep tunnel pattern" | Code exists but the depth loop never applies damage |
| Manages "all 5 screens" | Only HUD and Store are built; the manager is not added |
| "Requires all three escape conditions" | Conditions are checked, but nothing can satisfy them |
| One-click builder "builds and wires the entire playable scene" | Does not compile; spawns the player in rock; nothing saved |

---

## 9. Missing Systems and Assets

| Category | Missing items |
|---|---|
| **Sound** | No sound effect or music files. All `AudioClip` slots are empty. Needed: punch impacts per material, charged-punch sweetener, vacuum loop, crusher grind, coin chime, dynamite, ambience, music. |
| **3D models and art** | Everything is Unity primitives (cubes, cylinders, capsules). Needed: arms and gloves (2 tiers), 3 vacuum tiers, Crusher 07, vault door, dynamite, headlamp, logbooks, hard hats, generator, winch cable, fuel cell. Also textures, UI sprites and an icon. |
| **Narrative** | No logbook text, crew notes or Dispatch lines beyond placeholders. |
| **Interactables** | No generator, fuel cell, winch cable, logbook or crew-locker objects. Nothing calls the functions that would use them. |
| **UI** | Map, Pause, Settings and Ad-offer screens; upgrade buttons for 3 lines; price and level labels; detector buy button; Remove Ads and Free Dynamite buttons; safe-area handling for notches. |
| **VFX** | Dust, dynamite explosion and smoke, sparks, coin burst, ambience particles; camera shake, FOV kick, walking bob. |
| **Services** | AdMob (or similar) SDK, consent flow, Play Billing, purchase verification, restore purchases, analytics, crash reporting. |
| **Procedural generation** | Seeded prison generator, tunnels, caves, hidden rooms, stash placement. |
| **Data** | A real data-driven economy file loaded at runtime. |
| **Production** | App icon, store listing, screenshots, privacy policy page, Data Safety and content-rating answers. |

---

## 10. Required Fixes and Improvements

Priority key: **P0** prevents importing, compiling, running or progressing through the core game. **P1** breaks important gameplay or causes serious mobile performance problems. **P2** incomplete functionality or important quality problems. **P3** polish. Evidence tags: **[V]** verified in files, **[C]** recommendation.

### P0 — Critical

#### P0-1 — Compile-breaking string literals
- **Affected:** `ExitVaultAndLogbookController.cs` (lines 50, 58, 193), `UIScreenAndAccessibilityManager.cs` (lines 57, 70, 78, 86, 94), `SceneAutoBuilderEditor.cs` (lines 514–515 and 522–523). **[V]**
- **What is wrong:** eight captions put unescaped double quotes inside a string (for example `"DISPATCH: "Punch the wall."`). Two button labels contain a real line break inside a normal string instead of `\n`.
- **Why it matters:** C# will not compile, so Unity shows errors and **no script runs**. These are only the errors found by reading; the compiler may find more (missing references, type mismatches).
- **Change:** escape the inner quotes (`\"`), or remove them, and replace the real line breaks with `\n`.
- **Approach:** fix, then import the files in groups (section 12, Milestone 0), reading the first compiler error each time.
- **Verify:** Unity Console shows **0 errors**; the `BreakToEscape` menu appears.

#### P0-2 — The package has never been compiled, generated or run
- **Why it matters:** every other finding is from reading. Unknown errors will appear.
- **Change:** perform Milestone 0 in order: clean project, Git, import in groups, fix compile errors, run the builder, press Play, then Android build.
- **Verify:** checklist in section 14 (Test T0).

#### P0-3 — Input system compatibility
- **Affected:** `MobileFirstPersonController.cs` (legacy `Input.*`) and the `StandaloneInputModule` created in `SceneAutoBuilderEditor.cs`. **[V]**
- **What is wrong:** new Unity 6 projects default to the new Input System, so legacy input calls throw errors unless the setting is changed. A forum post reports the default is the new Input System and that calls throw an `InvalidOperationException` until Active Input Handling is set to Both.
- **Is "Both" sufficient? [C]** Yes for the **prototype**: it allows the existing legacy calls and the old UI module to run. It is a workaround, not a migration.
- **Change (now):** Project Settings → Player → Other Settings → Active Input Handling → **Both**; restart Unity.
- **Change (before release):** migrate touch to the new Input System (on-screen stick, enhanced touch, the Input System UI input module), and remove the `StandaloneInputModule`.
- **Verify:** no `InvalidOperationException` in the Console; joystick, look drag and buttons work in Play and on the phone.

#### P0-4 — Scene builder places everything for a different world
- **Affected:** `SceneAutoBuilderEditor.cs` versus `VoxelWorldManager.cs`. **[V]**
- **What is wrong:** the builder spawns the player at (7, 1.65, 1.6), the crusher at (7, 1.02, 2.2), the vault door at (7, 2, 33.2) and the room bounds around (7, 2, 2) and (7, 2, 8). Those coordinates belong to the **old 0.25 m world**. In the current world the Collapse Chamber is centred at about (24 m, y 1–5 m, 24 m). The player therefore spawns **inside solid rock**, and the crusher, vault and treasure sit buried in walls. The builder also saves nothing: no scene file, no material assets, so materials and generated objects may not survive a reload. It does not add `UIScreenAndAccessibilityManager` or build the Map, Pause or Ad panels.
- **Change:** compute spawn, crusher, vault and room bounds from the generated world (expose the chamber and room positions from `VoxelWorldManager`), place objects **after** the world is built, create material assets, add the missing manager and panels, and save the scene.
- **Verify:** press Play: the player stands in the chamber with the crusher in front of them; reopening the scene keeps everything.

#### P0-5 — Punch pattern wiring mismatch: a normal punch breaks one cube
- **Affected:** `FirstPersonToolController.cs` (lines 263 and 303–304) versus `VoxelWorldManager.HandlePunchStrike`. **[V]**
- **What is wrong:** the tool sends a "pattern tier" of **1** (normal) or **2 / 3** (charged), but the world reads that number as a **cube count** (5 … 97). It therefore applies about 1 cube of damage for a normal punch and about 4–6 for a charged punch. The tool never reads the player's pattern level, so the Pattern upgrade changes only the preview.
- **Why it matters:** the core loop (punch a patch of wall) is broken, and the economy and run-time model are invalid.
- **Change:** have the tool pass `GetCurrentPatternCubes()` (and a defined charged multiplier) and let the world use that number consistently.
- **Verify:** an edit-mode test per pattern level counting cells hit (expected 5, 9, 13, 21, 29, 37, 49, 61, 77, 97), and a play-mode count of cubes removed by one punch.

### P1 — High

#### P1-1 — Wall collision and per-frame physics cost on phones
- **Affected:** `VoxelChunk.RebuildMesh`, `VoxelWorldManager.BuildAllChunks`, `VoxelWireframePreview.Update`, `FirstPersonToolController`. **[V]**
- **What is wrong:** every rebuild of a chunk resets and reassigns the `MeshCollider`, which forces collision to be rebuilt; this also runs for all 72 chunks at start-up. A punch can touch several chunks, and each is fully re-scanned. The wireframe preview runs a ray, a sphere cast and up to dozens of `CheckBox` overlap tests **every frame** against those mesh colliders; the dynamite ghost does another ray every frame.
- **Why it matters (estimate [C]):** these are expected hitches and heat sources on low- and mid-range Android devices. Exact cost is **unmeasured**.
- **Change:** aim, preview, punch and dynamite should **read the voxel grid directly** with a grid ray traversal instead of asking physics. Keep colliders only for player and rubble contact, and bake them off the critical path (spread over frames, or asynchronously).
- **Approach:** keep the existing grid and chunk design; add a grid raycast function; limit rebuilds per frame; consider Burst jobs later.
- **Verify:** Unity Profiler on the low-tier phone: frame time and physics time while punching continuously; no spikes above the frame budget.

#### P1-2 — Rubble pool corruption and double payment
- **Affected:** `RubbleSuctionManager.SpawnRubbleCube`, `BezierSuctionCurveRoutine`, `Crusher07Controller.GrindSingleCubeRoutine`. **[V]**
- **What is wrong:** when the cap is reached, the manager sweeps the oldest cube (`activeRubble[0]`) and pays 60%. That cube may be mid-flight to the vacuum or in the crusher. Those coroutines later finish and return the same cube to the pool **again** (and pay again), so the same object can sit in the pool twice. Also, in-flight cubes are not counted against tank capacity, so a full tank discards a cube and its cash.
- **Change:** only sweep cubes that are settled and not being processed; make the return-to-pool function ignore a cube that is already inactive; count in-flight cubes against the tank; refund cash if a cube is rejected.
- **Verify:** stress test (spawn many cubes while vacuuming near a full tank): pool size stays constant, cash matches expected totals.

#### P1-3 — Save and load problems
- **Affected:** `SaveManager.cs`, `VoxelWorldManager.cs`. **[V]**
- **What is wrong:**
  - Broken-cube edits are **recorded and written** but, on load, only stored back into a dictionary. **Nothing applies them to the world**, so walls return while cash and upgrades persist.
  - The save class stores only 4 of the upgrade levels. `punchSpeedLevel`, `vacuumSuctionLevel`, `oreSenseLevel`, restored-equipment flags, generator/cable/code progress, `adsRemoved` (the field exists but is never filled) and cubes broken are **not saved**.
  - On load, tank capacity is hard-coded (50 / 150 / 350) instead of the 60–1450 table, and crusher speed uses a different formula.
  - The tank **count** is saved but the tank **contents** (the stack) are not, so after loading the HUD shows cubes that cannot be blown out.
  - Partial wall damage is not saved. The write is not atomic (a crash during writing can corrupt the file). The version number exists but there is no migration.
- **Change:** because generation is deterministic from code, restore by regenerating the world and then applying saved edits **before** the first mesh build; save every upgrade and story flag; derive stats from the economy tables after load; write to a temporary file and then replace; keep and test a version-migration step.
- **Verify:** dig, buy upgrades, force-close, relaunch: walls stay dug, stats match, no tank mismatch. Also test a corrupted file and an older save version.

#### P1-4 — The economy model is not enforced by the code
- **Affected:** `FirstPersonToolController`, `RubbleSuctionManager`, `CompanyStoreEconomyManager`. **[V]**
- **What is wrong:** there is no punch cooldown, so tapping alternates two arms with no limit from the "punches per second" upgrade (the arm animation is only about 0.15 s). The vacuum can pull every cube in its cone each frame, so the "cubes per second" table is not applied. Only the crusher is rate-limited.
- **Why it matters:** the 69-minute prediction assumes a 1.5–3.75 punch/s ceiling and vacuum and crusher limits. As written, a player can dig far faster, so pacing and the upgrade value are untested.
- **Change:** enforce a punch interval from the speed table and a vacuum rate from the suction table, or deliberately change the model and re-run the spreadsheet.
- **Verify:** measure punches/s, cubes vacuumed/s and cubes crushed/s in a test scene against the tables.

#### P1-5 — The escape and story chain is unreachable
- **Affected:** `ExitVaultAndLogbookController.cs`, scene builder. **[V]**
- **What is wrong:** `RepairPumpRoomGenerator()` and `RecoverWinchCable()` have no callers, `OpenCrewLogbook()` is never called, and no generator, fuel cell, cable or logbook objects exist. The gate code is also written into the code as constants (14, 03, 96). The vault object is placed at the old coordinates (P0-4).
- **Change:** create the interactable objects and wire them; place the vault at the real Main Shaft; put story text in data; make the clue chain readable on a phone.
- **Verify:** a full manual run from start to ending screen.

#### P1-6 — Advertisement and purchase placeholders are unsafe for release
- **Affected:** `MonetizationServicesManager.cs`. **[V]**
- **What is wrong:** with no ad SDK connected, tapping the rewards grants them **instantly**. The Remove Ads else-branch ("Editor test fallback") has **no editor-only guard**, so in a release build with no billing hook it grants the purchase **for free**. `adsRemoved` is not saved. The boost lasts 120 s (design: 300 s) and has no cooldown (design: once per 10 minutes). The Free Dynamite reward has no session cap. There is no purchase verification or restore flow. No ad or billing SDK is installed.
- **Change:** guard test fallbacks with `#if UNITY_EDITOR || DEVELOPMENT_BUILD`; read durations and cooldowns from data; persist purchases; integrate the ad SDK with a consent flow and Play Billing (see section 15).
- **Verify:** release build: reward cannot be obtained without a completed ad; Remove Ads cannot be obtained without a real purchase; purchase restores after reinstall.

#### P1-7 — Restored equipment cannot be obtained
- **Affected:** `CompanyStoreEconomyManager.cs`, `VoxelWorldManager.cs`. **[V]**
- **What is wrong:** `pneumaticHammerRestored`, `rockDrillRestored` and `conveyorRestored` are never set to true. Work Gauntlets turn on automatically at Punch Power level 3, not through a story unlock. The Rock Drill's depth loop shares a hit counter across depth layers, so after the first layer it applies no damage even if it were unlocked.
- **Change:** implement the repair interaction (cost plus found part) that sets these flags; fix the depth loop to apply per layer.
- **Verify:** restore each machine in play; confirm punch depth, speed and conveyor behaviour.

#### P1-8 — Missing upgrade and shop buttons
- **Affected:** `SceneAutoBuilderEditor.cs`. **[V]**
- **What is wrong:** no buttons exist for Punch Speed, Vacuum Suction, Ore Sense, the Treasure Detector, Free Dynamite (ad) or Remove Ads; prices and levels are not displayed.
- **Change:** build the store from the wireframe (seven cards with level, price, Buy).
- **Verify:** buy every upgrade line through the UI and see stats change.

#### P1-9 — Wireframe preview uses the wrong cube size
- **Affected:** `VoxelWireframePreview.cs` default `voxelSize = 0.25f`; the builder never sets it. **[V]**
- **What is wrong:** the world uses 0.5 m, so the preview boxes and snapping do not match the cubes that will break.
- **Change:** read the size from the world.
- **Verify:** the preview outline exactly overlays the cubes that disappear.

#### P1-10 — Frame-rate conflicts and quality settings that do nothing in URP
- **Affected:** `DeviceTierScaler.cs`, `MobileFirstPersonController.cs`. **[V]**
- **What is wrong:** both set `Application.targetFrameRate` in `Awake`; the controller sets 60, so on a Low-tier phone the result depends on which `Awake` runs last, and may override the intended 30. `QualitySettings.shadows` is set per tier, but URP takes shadow settings from its pipeline asset, so this has likely **no effect**. There is no resolution (render) scale although the plan called for 0.7× on Low. Time step 1/30 on Low makes rubble physics coarse.
- **Change:** one owner for frame rate; configure shadows and render scale through URP assets per tier; review the physics step on Low.
- **Verify:** log the actual frame rate and settings per tier on each test phone.

#### P1-11 — Voxel shader choice is unverified
- **Affected:** `SceneAutoBuilderEditor.CreateVertexColorMaterial`. **[V]** (code); **[C]** (risk)
- **What is wrong:** it first picks the Built-in-pipeline shader "Particles/Standard Unlit", which is likely **not supported by URP** and could render invisible or magenta walls. Chunks have no UVs, so no textures can show.
- **Change:** use a small URP shader (Shader Graph with a vertex colour node) or a URP-supported unlit/lit shader that reads vertex colours.
- **Verify:** the walls render in colour on the phone in an Android build.

### P2 — Medium

| ID | File / system | Problem and impact | Change / approach | Verify |
|---|---|---|---|---|
| P2-1 | `VoxelWorldManager.HandlePunchStrike` | Cube counts above 25 cannot be reached: radius is capped at 5 (61 cells), diamond sizes do not match the table (for example 29, 49, 77, 97), and partial outer rings fill from one side first, giving lopsided patches. | Define the pattern shapes explicitly (data), apply symmetric outer rings. | Test counts and symmetry per level. |
| P2-2 | Rubble caps (E) | Caps 200/350/500 differ from plan 100/150–250/400; default 150; 400 always prewarmed. | Start from the planned values, set via profiling, size the pool by tier. | Profile on three phones; record the highest stable cap. |
| P2-3 | `economy_config.json` | Not loaded; tables hard-coded; JSON values (300 s boost, 600 s cooldown) disagree with C#. | Load tables from one data source; remove duplicates. | Change a value in the file and see it in-game. |
| P2-4 | Treasure | Two parallel treasure systems (treasure voxel types, and separate treasure objects). Voxel treasure hashes at about 1 in 128 cubes, which adds unmodelled cash. | Pick one system; model its payout in the spreadsheet. | Spreadsheet matches observed income. |
| P2-5 | World generation | Bands are concentric rings, not the designed directional layout; rooms are isolated boxes with no tunnels. | Rework to the map design during Milestone 4. | Compare against the design map. |
| P2-6 | UI scaling and touch | No safe-area (notch) handling; left-handed mode does not mirror the look zone; tap targets unverified. | Add safe-area handling; mirror look zone; test on small and tall screens. | Screenshot tests on three aspect ratios. |
| P2-7 | `FirstPersonToolController` | 569-line class doing input, animation, aim, dynamite, hotbar; hard-coded constants. | Split by responsibility; expose animation curves to the inspector. | Same behaviour, easier tuning. |
| P2-8 | Execution order | No order attributes. `SaveManager.Start` loads while other `Start` methods apply stats and can overwrite loaded values. | Define initialization order. | Load test (P1-3). |
| P2-9 | Mesh generation | No UVs; synchronous rebuilds; full normal recalculation. | Add UVs or colour atlas; move to jobs if profiling demands. | Profiler. |
| P2-10 | Tutorial | Dispatch tutorial never starts (manager not in scene; compile errors). | Add manager; fix strings. | First 3 minutes play-through. |
| P2-11 | Audio | No clips; wall material for sound is read after the voxel breaks (may read as air). | Add clips; read material before damage. | Listen test per material. |
| P2-12 | Tier detection | Thresholds guessed (RAM, cores, graphics memory); an 8 GB phone may report less than 7,500 MB and be classed Mid. | Tune from measured devices; allow manual override. | Log results from test phones. |
| P2-13 | Map | Room bounds use old coordinates; no map UI; no fog sprites. | Rebuild with the real layout. | Rooms reveal when entered. |

### P3 — Low

| ID | Problem | Suggestion |
|---|---|---|
| P3-1 | Native vibration call uses an older system-service lookup on newer Android | Move to the current vibration manager API |
| P3-2 | Headlamp has no levels; the design said lantern/headlamp with 3 levels | Decide the design, then implement |
| P3-3 | Many singletons and static events | Keep for the prototype; consider later |
| P3-4 | Typographic and naming consistency in UI text | Final copy pass |
| P3-5 | Credits and ending text are placeholder | Write final text |

---

## 11. Android Performance and Compatibility

### Targets (to be validated through profiling, **not guaranteed**)

| Tier | Target | Rubble cap (plan) | Notes |
|---|---|---|---|
| Low | Stable 30 fps | 100 | Reduced shadows, resolution scale about 0.7, 30 Hz physics step |
| Mid | 60 fps if sustainable, otherwise stable 30 fps | 150–250 | Main target |
| High | 60 fps | 400 | — |

### Android build settings to apply (manual)
Landscape only; IL2CPP; ARM64 only; Android App Bundle; minimum Android 8 (API 26) to start (check Unity's own minimum); **target API 36**; Unity version whose release notes state 16 KB page-size support.

### Risks
- **Wall collision and physics cost** (P1-1) and **rubble count** are the main performance risks. Unmeasured.
- **Heat:** a 20-minute continuous-play test is required on each phone.
- **16 KB page size:** community reports describe Unity AABs being rejected by Play Console even on a Unity version documented as supported. Treat this as a real build risk and upload an internal-test build **early** to see Google's check.
- **Plugins:** every ad, billing and analytics SDK must also be 16 KB compatible.
- **Memory:** about 1.5 MB of voxel data is small, but 400 prewarmed rubble objects, colliders for 72 chunks, and the Android 4 GB devices need measurement.

### Animation and tool-viewmodel review (as requested)

**Documents reviewed:** `animation.txt` (another assistant's answer), `argon.txt`, `argon2.txt`, and the tool-controller code.

**Decision recorded:** rigid arm, vacuum and dynamite movement is driven by code. AI tools are used only for static models, which need decimation (suggested about 3–5k triangles for arms, under 2k for small props) and a blocky low-poly look. `animation.txt` suggested skeletal animation for the arms; that advice was **not adopted**.

| Animation | Logic that exists **[V]** | Missing or unverified |
|---|---|---|
| Idle breathing | Sine-based offset on arms and vacuum | Tuning |
| Left / right jab | Alternating coroutines; thrust/return times derived from punch speed (clamped to roughly 0.025–0.07 s thrust) | Likely too fast to read at 60 fps; no real models; unverified feel |
| Charged wind-up / release | Hold detection, arm shake while charging, release coroutine | Charge visuals, tuning, sound |
| Vacuum hold and recoil | Jitter and pose on hold | Real model, nozzle effects, recoil tuning |
| Dynamite | Throw coroutine; ghost preview on the wall; pooled stick with fuse spark; blast event | Design says "place on wall"; the code throws and sticks. No explosion particles. |
| Tool switching | Lower/raise coroutine per slot | Models, polish |
| Headlamp activation | Gesture coroutine; toggles a spotlight | Levels, light tuning |
| Camera shake, FOV kick, walking bob | Not found | Not implemented |

All of this is **Not tested or unverified.**

**Godot:** the only mention of Godot in the materials is a question in `argon.txt` asking which engine (Unity, Unreal or Godot) is being used. **No Godot GDScript animation-preview experiment exists in the provided files, and the project has not been migrated to Godot.** The production codebase is Unity C#.

---

## 12. Development Roadmap and Milestones

Sizes are relative effort (S/M/L), not calendar promises. Each milestone ends with an **exit gate**; do not start the next one until the gate passes.

### Milestone 0 — Project validation (Size: M)
Install Unity 6.3 LTS with Android Build Support; create a clean URP project; set Landscape Left; set Active Input Handling to **Both**; import TextMeshPro essentials; create the folder structure; initialise Git and commit the original ZIP untouched; import scripts **in groups** (data and world, then rubble and tools, then economy and UI, then services, then the editor builder); fix compile errors (P0-1) one at a time; fix scene placement (P0-4); run the builder; first Android test build.
**Exit gate:** zero compile errors; scene builds and the player spawns in the chamber; an Android build installs and launches on a phone.

### Milestone 1 — Core destruction prototype (Size: L)
Voxel world; wall destruction; normal and charged punch (fix P0-5); rubble spawning and pooling (fix P1-2); touch movement and look; aim assist; grid-based aiming (P1-1); profiling on three phones.
**Exit gate (Gate 1):** punching a wall into cubes holds 30 fps with 100 rubble cubes on the low phone, and the feel is judged good by a stranger. This proves the hardest mechanic.

### Milestone 2 — Vacuum and economy (Size: M)
Vacuum suction and blow-out; tank capacity; crusher; floating cash; enforce rates (P1-4); initial upgrade UI (P1-8); load tables from data (P2-3); re-run the spreadsheet with measured rates.
**Exit gate (Gate 2):** one loop (punch → vacuum → crush → upgrade) is enjoyable for 10 minutes.

### Milestone 3 — Progression and tools (Size: M)
Band progression; punch, vacuum and crusher upgrades through all levels; dynamite; headlamp; treasure and detector; hotbar polish; restored equipment (P1-7).
**Exit gate (Gate 3):** a tester says "one more upgrade" unprompted.

### Milestone 4 — Prison generation and story (Size: L)
Hand-built v1 mine layout per the design map; interactables (generator, fuel cell, cable, logbooks); story text; exit puzzle (P1-5); then seed-based procedural generation (tunnels, caves, hidden rooms, stashes) as the long-term goal.
**Exit gate (Gate 4):** a full start-to-escape run takes roughly 60–90 minutes with no dead ends.

### Milestone 5 — UI, animation and presentation (Size: L)
Final HUD; shop, map, pause and ad-offer screens; tutorial; tuned tool animations; particles; haptics; lighting; safe-area and left-handed mode (P2-6).

### Milestone 6 — Save system and progression (Size: M)
Fix P1-3; test save/load, corruption and version migration; new-run flow; replay.

### Milestone 7 — Audio and production assets (Size: L)
Material punch sounds, vacuum, crusher, coin, dynamite, ambience, music; final models and textures; Android optimisation of all assets; licence log.

### Milestone 8 — Performance and mobile optimisation (Size: M)
Tier settings through URP (P1-10); rubble caps from profiling; collision optimisation; instancing; memory; battery and thermal tests; 20-minute sessions.
**Exit gate (Gate 5):** no crash and stable frame rate in a 20-minute heat test on all three phones.

### Milestone 9 — Monetization and Google Play preparation (Size: L)
Rewarded ads with consent; Remove Ads purchase with verification and restore; analytics; crash reporting; privacy policy; Data Safety; content rating; API 36; 16 KB compatibility; Play Billing version; AAB; internal test; closed test; production (section 15).

---

## 13. Priority Task List

| Order | Task | ID | Priority |
|---|---|---|---|
| 1 | Create clean URP project, Git, set input handling to Both | M0 | P0 |
| 2 | Fix the 10 string-literal compile errors; import in groups | P0-1 | P0 |
| 3 | Fix scene-builder coordinates, save scene, add missing manager and panels | P0-4 | P0 |
| 4 | Fix punch pattern wiring | P0-5 | P0 |
| 5 | Run Play mode, then first Android build | M0 | P0 |
| 6 | Verify voxel shader renders on device | P1-11 | P1 |
| 7 | Grid-based aim and preview; asynchronous collider baking; profile | P1-1 | P1 |
| 8 | Fix rubble pool double-return and tank overflow | P1-2 | P1 |
| 9 | Fix preview cube size; frame-rate ownership; URP tier settings | P1-9, P1-10 | P1 |
| 10 | Enforce punch and vacuum rates; re-run spreadsheet | P1-4 | P1 |
| 11 | Complete store UI and equipment restoration | P1-7, P1-8 | P1 |
| 12 | Fix save/load fully | P1-3 | P1 |
| 13 | Interactables, story text, escape chain | P1-5 | P1 |
| 14 | Make monetization release-safe | P1-6 | P1 |
| 15 | Data-driven economy; fix pattern shapes; treasure model | P2-1, P2-3, P2-4 | P2 |
| 16 | Audio, models, particles, UI polish | M5, M7 | P2 |
| 17 | Procedural generation | M4 | P2 |
| 18 | Play Console, store listing, testing, release | M9 | — |

---

## 14. Testing Strategy

**Principle:** a feature counts as "working" only when it passes a test on a real device. Written code and a static check do not count.

### Test layers

| Layer | Tool | Examples |
|---|---|---|
| Edit-mode unit tests | Unity Test Framework | Upgrade cost formula; pattern cell counts per level; save round trip; economy tables vs spreadsheet |
| Play-mode smoke tests | Unity Play mode | Spawn in chamber; punch removes cubes; rubble count never exceeds cap; pool size constant |
| Device profiling | Unity Profiler on a connected phone | Frame time, physics time, memory, GC spikes while punching and vacuuming |
| Thermal and long-session | Real phones | 20-minute continuous play, three tiers |
| Interruptions | Real phones | Phone call, app switch, rotate, lock screen, low battery |
| Screen variety | Real phones or devices | Notch, tall and short aspect ratios |
| Play-side checks | Play Console | Pre-launch report; upload a test AAB early |

### Key tests

| ID | Test | Pass condition |
|---|---|---|
| T0 | Import and compile | 0 compile errors; builder menu runs; Play shows the chamber |
| T1 | Punch pattern | One punch at each level breaks the expected cube count |
| T2 | Rubble stress | 400 spawns with vacuum active: no pool duplicates, cash correct |
| T3 | Save | Dig, upgrade, close, reopen: all state restored |
| T4 | Rates | Punch, vacuum and crusher rates match the tables |
| T5 | Escape run | Start to ending with all three requirements |
| T6 | Release safety | Release build cannot grant rewards or Remove Ads without a real ad or purchase |
| T7 | Performance | Gate 1 and Gate 5 numbers on low, mid and high phones |

Keep a "known problems" list; it must be cleared before each gate.

---

## 15. Google Play Release Requirements

**Verified against official Google pages in this review (checked 3 November 2026):**

| Requirement | Finding | Source type |
|---|---|---|
| **Target API level** | New apps and updates must target **Android 16 (API 36)** or higher (from 31 August 2026). Existing apps need API 35 or higher to stay available to new users on newer devices. | Official Play Console Help |
| **Play Billing Library** | Version 7 was the last accepted for new apps and updates until 31 August 2026 (extension to 1 November 2026, now passed). **Use version 8 or newer** (version 8's deadline is 31 August 2027). | Official developer documentation |
| **16 KB page size** | Official page: since 1 November 2025, new apps and updates targeting Android 15+ must support 16 KB page sizes on 64-bit devices. **Third-party sources quote a 1 February 2027 date for updates; this conflicts and was not confirmed on an official page here, so treat 16 KB support as required now.** | Official developer documentation plus third-party reports |
| **Closed test for personal accounts** | Personal developer accounts created after 13 November 2023 must run a **closed test with at least 12 testers opted in continuously for 14 days** before applying for production access. | Official Play Console Help |

**Not verified from official documents in this review (check in Play Console before relying on them):** Data Safety form details, content-rating questionnaire, ads declaration, ad-SDK and consent requirements, store-listing asset sizes, app size limits, and the registration fee.

**Release checklist**

- Developer account created and identity verified (and the closed-test plan, if it is a personal account)
- Target API 36; Unity version that supports 16 KB; IL2CPP; ARM64; AAB
- Ads SDK and Play Billing (version 8 or newer) installed and **16 KB compatible**; consent flow
- Remove Ads purchase verified and restorable; restore-purchases path
- Analytics and crash reporting
- Privacy policy page; Data Safety form; content rating; ads declaration; target audience not primarily children
- Store listing: name, descriptions, icon, feature graphic, screenshots
- Internal testing → closed testing (12 testers, 14 days if required) → production with staged rollout
- Final originality check (section 16.2)

---

## 16. Current Project Status and Next Steps

### 16.1 Status in four answers

1. **What have we actually completed?** The design package, the economy model and a first-draft code scaffold of about 4,300 lines. Nothing has been compiled, built or tested.
2. **What problems exist in Gemini Argon's code?** Compile-breaking strings, a scene builder that spawns the player in rock, a punch that breaks one cube, unreachable story and equipment, unsafe monetization fallbacks, unrestored save data, and an economy model the code does not enforce (sections 8 and 10).
3. **What exactly needs to be fixed?** The P0 items first (section 13, rows 1–5), then P1.
4. **What remains before release?** Milestones 0–9, then Google Play closed testing and review.

### 16.2 Originality and publishing considerations

- The original concept text was noted to be **very close to an existing game's store description**. Rewrite it in your own words.
- Develop original: game identity, art direction, UI design, prison/mine layout, narrative and characters, story text, visual effects, audio, and the store listing and marketing.
- The reference footage was used to understand mechanics and pacing only. **Do not use another game's protected assets, branding, text, models, textures, sounds or distinctive presentation.**
- Gemini's code was written to mimic reference-clip behaviour (for example timestamps and "exact" movements). Behaviour inspiration is acceptable; keep all art, text and audio your own, and keep a **licence log** for every asset.
- Keep the Greywater Deep setting, the Company Store, the crew logbooks and the hazard-stripe UI as the distinguishing identity.

### 16.3 Immediate next steps

1. Create the clean Unity 6.3 LTS project and a Git repository; commit the original ZIP unmodified.
2. Set Active Input Handling to **Both**.
3. Import the scripts in groups and fix the compile errors (P0-1).
4. Fix the builder's placement and punch wiring (P0-4, P0-5), then press Play.
5. Build to a real phone and record the first measurements.
6. Update this document at each gate, replacing statuses with **demonstrated** results.

### 16.4 Notes for this repository

- File location: `docs/Break_to_Escape_Development_Status_Report.md`, linked from the root README.
- Suggested repository hygiene: a Unity `.gitignore`, and Git LFS only if large binary assets are added.
