# Break to Escape

**A first-person 3D incremental digging and destruction game for Android (Google Play).**

> *Break. Collect. Restore. Escape.*

You are the last worker trapped in a collapsed underground mining complex. Punch the rock into chunky physical cubes, vacuum up the rubble, feed it to a crusher for cash, spend the cash at the mine's still-running Company Store to restore old equipment, and dig through ever harder rock until you find the way out.

**Core loop:** Punch → Break → Vacuum → Shred → Earn Money → Upgrade → Dig Deeper → Escape

---

## Project status at a glance

| Area | Status |
|---|---|
| Game design | Complete first version (design document, economy model, technical plan, wireframes) |
| Code | A reworked Unity C# codebase in `UnityProject/Assets/` addresses every issue in the status report (P0-1 … P3-7). **It has never been compiled in Unity, never run, never built for Android and never tested on a phone.** |
| Verification so far | Type-checked against stand-ins of the Unity API: 0 errors, 0 warnings. Pure game logic (economy, punch patterns, voxel grid, ray traversal, rubble pool, save, escape chain, monetization rules, tiers, mine generators): **75 automated tests, all passing outside Unity**. Economy data generated from the spreadsheet; pacing reproduced (69.4 minutes). A stub type-check is not a Unity compile. |
| Unity compile / scene run / Android build / device test | **No / No / No / No** |
| Assets | Primitive placeholders only; no sounds, models or UI art yet (`docs/ASSET_REQUIREMENTS.md`). |
| Playable prototype | **Not yet demonstrated.** Next step: import into Unity 6.3 LTS following `UnityProject/README_UNITY_SETUP.md`. |
| Release | Not started (no Google Play account steps, store listing, ad or billing SDKs installed; integration guide in `docs/ANDROID_BUILD_CHECKLIST.md`) |

**Nothing in this repository should be assumed to work until it has been compiled and tested on a real Android phone.** Written code is not tested code. The full audit is in [`docs/Break_to_Escape_Development_Status_Report.md`](docs/Break_to_Escape_Development_Status_Report.md).

---

## Repository layout

```
README.md                                   This file: the complete project overview
docs/
  Game_Design_Document                      Full game design (all mechanics, story, scope)
  Technical_Budget_and_Production_Plan      Engine, performance budgets, architecture, schedule
  Break_to_Escape_Development_Status_Report.md   Verified status, code audit, roadmap, tests
  wireframes/
    WIREFRAMES.md                           Text specification of the 5 touch screens
    (wireframe images / drawing export)     The five landscape screen wireframes
  economy/
    Break_to_Escape_Economy.xlsx            Economy model with live formulas
    ECONOMY_TABLES.md                       The same numbers as readable tables
  PROGRESS.md                               Task list, test counts, verification results
  DECISIONS.md                              Choices made during implementation, with reasons
  ASSET_REQUIREMENTS.md                     Every sound, model, sprite and icon still needed
  ANDROID_BUILD_CHECKLIST.md                Player settings, URP tiers, 16 KB, ads, billing, Play Console
  ASSET_LICENCES.md                         Licence log for third-party files
codebase/                                   First-draft Unity scripts (19 .cs + economy_config.json), kept untouched as the original
UnityProject/
  README_UNITY_SETUP.md                     Step-by-step owner guide: import into Unity, build the scene, Play, build to a phone
  Assets/Scripts/Core/                      Game rules with no Unity dependency (tested)
  Assets/Scripts/...                        World, Player, Rubble, Economy, Story, UI, Save, Audio, Services, Performance, Effects, Bootstrap, Editor
  Assets/Resources/Data/                    economy.json (generated from the spreadsheet), story.json, audio.json
  Assets/Shaders/                           URP voxel and unlit vertex-colour shaders
  Assets/Tests/EditMode/                    NUnit tests (run in Unity's Test Runner and with dotnet test)
tools/
  verify/                                   Type-check harness, economy JSON generator, pacing check
  tests/                                    dotnet test project for the pure-logic tests
```

**Suggested reading order:**
1. This README (whole project).
2. `docs/Break_to_Escape_Development_Status_Report.md` (what is real, what is broken, what to fix first).
3. `docs/Game_Design_Document` (design details and intent).
4. `docs/economy/ECONOMY_TABLES.md` (numbers).
5. `docs/Technical_Budget_and_Production_Plan` (budgets, tiers, schedule).
6. `docs/wireframes/WIREFRAMES.md` (touch UI).
7. `docs/PROGRESS.md` and `docs/DECISIONS.md` (what was done, and why).
8. `UnityProject/README_UNITY_SETUP.md` (how to get it running in Unity).

---

## 1. Game summary

| Item | Detail |
|---|---|
| Name | **Break to Escape** |
| Platform | Android phones only, landscape, distributed through Google Play |
| Genre | First-person incremental digging and destruction with light exploration and story |
| Engine plan | Unity 6.3 LTS, URP, C#, IL2CPP, ARM64, Android App Bundle |
| Session length | 5–10 minutes per sitting; a full escape is planned at about 60–90 minutes |
| Pillars | **Destruction, resource collection, equipment progression** (a feature that serves none of these waits) |
| Setting | A forgotten underground mining complex ("Greywater Deep", a made-up place) |
| Art direction | Heavy machinery, mineral deposits, rough rock formations, industrial equipment, glowing geological discoveries; chunky cubes, warm lamp light against cold dark rock |
| Tone | Dark comedy with real hope; nothing graphic; family-safe |
| Monetization decided so far | Rewarded ads plus a one-time Remove Ads purchase. Other methods are deferred. |
| Version 1 scope | One hand-built mine. Procedurally generated mines ("New Shift") are a later feature and remain part of the long-term vision. |

---

## 2. Core loop and session structure

| Scale | What the player does |
|---|---|
| Every 60 seconds | Punch rock, cubes spill out, vacuum them up, blow or carry them to the crusher, see cash pop up, buy something small |
| Every 10 minutes | Save for a bigger upgrade or restored machine, which opens the next harder rock layer |
| Every session (5–10 minutes) | Make visible progress, find one new thing (a room, a note, a crate), stop at a natural pause |
| Whole game | About 60–90 minutes from first punch to escape (the economy model currently predicts about 69 minutes, but see the status report: the code does not yet enforce the rates the model assumes) |

---

## 3. Story, world and tone

**Story.** Shift 9 at Greywater Deep never ended. A collapse sealed the network and the company walked away. The player wakes in the Collapse Chamber with only work gloves. Crusher 07 still runs on reserve power, and the automated **Company Store** terminal still works, selling equipment at ridiculous prices with cheerful corporate messages (this is also why cash exists). To get out, the player must recover resources, restore broken equipment and reach the Main Shaft lift.

**Characters**
- **Worker 17 (the player):** silent, never shown; only the hands are visible.
- **Dispatch:** the automated radio and Company Store voice, shown as short on-screen text, cheerful and a little sinister.
- **The lost crew:** Foreman Quill, Surveyor Pike and Cook Bellamy. They are never seen. They left logbooks, wall notes and hard hats with their names. Their logbooks hide the lift-gate code.

**Tone.** Short, dry captions, for example *"Day 1. The mine is not impressed with you."*

---

## 4. Controls (landscape touch)

| Area | Control |
|---|---|
| Left thumb | Virtual joystick to walk |
| Right thumb, empty space | Drag to look around |
| Right thumb, big button | **PUNCH:** tap to punch, hold to charge a bigger punch |
| Right thumb, second button | **VACUUM:** hold to suck; toggle to blow cubes out |
| Bottom centre | Hotbar: fists/tool, vacuum, dynamite, headlamp (tap to switch) |
| Top left | Cash and cash per second |
| Top right | Map and pause |

**Helpers:** aim assist (the punch snaps to the cube nearest the crosshair); an unlockable **auto-punch** upgrade; **left-handed mode** that mirrors the layout; **haptics** (short vibration for hits, longer for big breaks, can be turned off).

Exact positions of all five screens are in [`docs/wireframes/WIREFRAMES.md`](docs/wireframes/WIREFRAMES.md).

---

## 5. Core mechanics

**The wall.** The mine is one big block of 0.5 m cubes. Each cube has a material, hit points (HP) and a cash value. Walls are about 6 m thick near the start. A punch hits a shape of cubes (a plus sign at first, growing with upgrades); a wireframe preview shows exactly which cubes will break. Damage accumulates on a cube until it reaches the cube's HP, then it breaks.

**Rubble.** Broken cubes become small physical cubes that bounce and pile up. To keep phones smooth there is a **rubble cap** per device tier. When the pile exceeds the cap, the oldest settled cubes are swept away and converted into an automatic cash bonus of **60%** of their value, so nothing lags and nothing is wasted.

**Vacuum.** Hold to pull cubes into a tank (the tank has a capacity). When full, go to the crusher and blow cubes out toward it. Vacuum visual tiers: Dust-buster, Ore Hoover, Suction Cannon. Cubes being sucked in follow a scripted curve into the nozzle rather than fighting physics.

**Crusher.** Crusher 07 in the Collapse Chamber pays cash for every cube, processing at a rate set by the Crusher Speed upgrade. A later conveyor feeds it automatically.

**Dynamite.** Sticks to a wall, 3-second fuse, big blast. Found in crates or earned from ads. Good for hard rock, but it **destroys treasure** in its blast radius, which creates a choice.

**Light.** Deeper rooms are dark. The headlamp has 3 simple levels (design); wall lamps and glowing crystals also light the way.

**Treasure.** Gold, copper and glowing crystal cubes hide in the rock and pay several times a normal cube. Glowing crystals light up so players can spot them. A treasure-detector upgrade adds a proximity radar.

---

## 6. Rock bands (depth layers)

Six bands. Each is harder and pays more. The numbers come from the economy model and are starting guesses to be tested.

| # | Band | Look | Cube HP | Cube value | Cubes to clear | Planned time |
|---|---|---|---|---|---|---|
| 1 | Concrete lining | Grey concrete, rebar, work lamps | 10 | $2 | 850 | ~7 min |
| 2 | Packed earth and timber | Dirt, rotten supports | 25 | $3.50 | 1,500 | ~10 min |
| 3 | Sandstone gallery | Layered stone, old rails | 60 | $6 | 2,300 | ~13 min |
| 4 | Granite core | Hard speckled rock, heavy machinery | 140 | $10 | 3,500 | ~14 min |
| 5 | Ore veins | Copper and iron seams | 260 | $30 | 5,000 | ~15 min |
| 6 | Crystal vault | Glowing geode crystals | 420 | $70 | 7,000 | ~10 min |

---

## 7. The mine (version 1: one hand-built mine)

- **Collapse Chamber:** start; Crusher 07; the Company Store terminal; the first tutorial.
- **West, Old Gallery:** dead-end tunnel with the foreman's locker and the first logbook.
- **East, Equipment Bay:** broken machines to restore; second logbook.
- **South, Pump Room:** the generator, a fuel cell, the third logbook.
- **North, Main Shaft:** the lift gate (the exit), reachable only at the end of band 6.
- **Hidden nooks:** small side rooms with treasure, dynamite and wall notes in every band.

The player digs freely in any direction. A hand-drawn in-game map fills in as rooms are discovered; unexplored spots are marked with a question mark.

**Design size:** about 100 × 50 × 100 cubes (50 × 25 × 50 m), about 500,000 cubes, roughly half a megabyte of data. The current code builds a smaller 96 × 32 × 96 world (see the status report).

**Later feature:** "New Shift" generates a different mine each time from a seed (tunnels, caves, hidden rooms, stashes).

---

## 8. Upgrades and equipment

### Seven numeric upgrades (10 levels each)

Costs: the cost of each level is `base × 2.2^(level − 2)`; costs shown are the base (cost to reach level 2). Effects from the economy model:

| Upgrade | Base cost | Level 1 → Level 10 | What it does |
|---|---|---|---|
| Punch Power | $150 | 10 → 283 damage (×1.45 per level) | Damage to every cube in the punch |
| Punch Speed | $200 | 1.5 → 3.75 punches/s (+0.25 per level) | Punches per second |
| Punch Pattern | $250 | 5 → 97 cubes (hand-designed list: 5, 9, 13, 21, 29, 37, 49, 61, 77, 97) | Cubes hit by one punch |
| Vacuum Suction | $175 | 6 → 63.6 cubes/s (×1.3 per level) | Pull rate |
| Tank Capacity | $125 | 60 → 1,450 cubes (list: 60, 90, 130, 190, 270, 380, 530, 740, 1030, 1450) | Cubes carried per trip |
| Crusher Speed | $225 | 8 → 84.8 cubes/s (×1.3 per level) | Crush rate |
| Ore Sense | $375 | ×1.00 → ×1.72 cash (+0.08 per level) | Cash multiplier on every cube |

Full per-level effect and cumulative cost tables, the planned upgrade levels at the start of each band, and the predicted pacing are in [`docs/economy/ECONOMY_TABLES.md`](docs/economy/ECONOMY_TABLES.md).

### Restored equipment (story unlocks)

Broken machines found in rooms are repaired at the Company Store for cash plus a found part.

| Equipment | Where found | Effect |
|---|---|---|
| Work Gauntlets | Old Gallery (band 2) | New hand model, small power boost |
| Pneumatic Hammer | Equipment Bay (band 3) | Faster punches, tighter pattern |
| Rock Drill | Pump Room (band 5) | Long narrow tunnel pattern for deep digging |
| Ore Hoover, then Suction Cannon | Bought in the store | New vacuum look and range |
| Conveyor | Equipment Bay | Feeds cubes to the crusher automatically |

Other store items: dynamite packs, headlamp levels, auto-punch, treasure detector.

---

## 9. The escape

To ride the lift out of the Main Shaft the player needs:
1. **Power:** repair the generator in the Pump Room with a fuel cell.
2. **Cable:** recover the winch cable in the Ore veins.
3. **Gate code:** three numbers, one in each crew logbook (Quill, Pike, Bellamy).

Then a short ending plays: daylight, a dry final line from Dispatch, and a credits screen with total time and cubes broken, plus a "New Shift" teaser.

---

## 10. Ads and purchases

Decided so far: **rewarded ads plus a one-time Remove Ads purchase.** Other earning methods are deferred.

- **Rewarded ads (always optional):** "Double cash for 5 minutes" (at most once per 10 minutes); "Free dynamite crate" at the Company Store (a few times per session).
- **Remove Ads (one time):** never shows ad prompts; grants the ad rewards for free at a gentler rate.
- **Rules:** no ads in the first 5 minutes; no pop-up ads that interrupt digging; a clear privacy and consent screen; the "No thanks" button is never smaller than "Watch ad".

---

## 11. Teaching the player (first 3 minutes)

Short side captions in the game's voice, one idea at a time, each shown only when the player does the thing:
1. *"Punch the wall. It has it coming."*
2. *"Cubes are cash. Hold VACUUM."*
3. *"Crusher 07 is hungry. Feed it."*
4. *"The Company Store is open. Of course it is."* (first upgrade, affordable in under 90 seconds)
5. *"Dig deeper."*

Rule: the first cash payout and the first upgrade must happen within 2 minutes.

---

## 12. Look and sound

**Look:** chunky cubes; warm lamp light against cold dark rock; a clear colour per band; glowing crystal accents; industrial hazard-stripe UI in black and yellow; readable at phone size.

**Sound (requirements, no recordings yet):** satisfying punch impacts that change with the material; a vacuum loop whose pitch rises as the tank fills; a crusher grind; a coin chime that grows with the payout; low cave ambience; a deep dynamite boom; a calm music bed that never fights the effects. All sounds must be royalty-free or original, with licences recorded.

**Accessibility:** left-handed layout, vibration on/off, adjustable look sensitivity, large readable text, no information shown only by colour, reduce-flashing option for explosions.

---

## 13. Version 1 scope

| In version 1 | Not in version 1 |
|---|---|
| One hand-built mine, six bands | Random mines ("New Shift") |
| Seven upgrades, four restored tools | Multiplayer or leaderboards |
| Vacuum, crusher, dynamite, headlamp | Cosmetic shop |
| Three crew logbooks and the lift escape | Many languages (English only) |
| Rewarded ads and Remove Ads purchase | Other purchases |
| Local save | Cloud save (after launch) |

---

## 14. Technical decisions

### Engine and settings

| Choice | Decision |
|---|---|
| Engine | Unity 6.3 LTS (or the newest LTS on the start date); the code uses Unity 6 APIs and will not compile on Unity 2022 |
| Graphics | URP (Universal Render Pipeline) |
| Code style | Normal GameObjects and C# scripts; **no ECS/DOTS** (too complex for this project's needs) |
| Build | Android App Bundle, IL2CPP, ARM64 only |
| Orientation | Landscape only |
| Minimum Android | Android 8 (API 26) to start; check Unity's own minimum |
| Input (prototype) | Active Input Handling set to **Both** (the draft code uses the legacy input API); migrate to the new Input System before release |
| Version control | Git |

### Device tiers (targets to validate by profiling, not guarantees)

| Tier | Represents | Target | Resolution scale | Rubble cap | Shadows |
|---|---|---|---|---|---|
| Low | Budget phone, about 3–4 GB RAM | Stable 30 fps | 0.7× | 100 cubes | Fake blob shadows only |
| Mid | Current mid-range, 6–8 GB RAM (main target) | 60 fps if sustainable, otherwise stable 30 fps | 0.85–1.0× | 150–250 cubes | One soft shadow |
| High | Recent flagship | 60 fps | 1.0× | 400 cubes | Full shadows |

### Performance budgets

| Item | Limit |
|---|---|
| Frame time | 33 ms at 30 fps; 16.6 ms at 60 fps |
| Draw calls | 150 or fewer on the low tier |
| Triangles on screen | 200,000 or fewer (low), 400,000 (mid) |
| Memory | 1.2 GB or less on the low tier |
| Download size | Aim for 150 MB or less (verify Google's current limits) |
| Heat | After 20 minutes the phone still holds 30 fps |
| Start-up | 8 seconds or less to first punch |
| Save file | Under 1 MB |

### Architecture (eleven parts, one job each)

| # | Part | Job |
|---|---|---|
| 1 | Input | Touches to walk, look, punch, vacuum, tool switch |
| 2 | World | Mine as a grid of cubes (material, HP, value); which are broken |
| 3 | Mesh builder | Draw only visible surface faces; redraw a small area when punched |
| 4 | Tools | Fists, vacuum, dynamite, headlamp |
| 5 | Rubble | Bouncing cubes: pool, cap, pickup, sweep |
| 6 | Economy | Cube values, crusher, shop, upgrade levels, driven by data tables |
| 7 | Story | Rooms, logbooks, Dispatch captions, lift-gate puzzle |
| 8 | UI | Cash, tank gauge, shop, map, pause, settings |
| 9 | Save | Upgrades, broken cubes, story progress |
| 10 | Audio | Effects, ambience, music |
| 11 | Services | Ads, purchases, analytics, crash reports behind wrappers |

**Key technical decisions**
- The world is a byte-array grid cut into **16-cube chunks**; interior cube faces are never drawn; a punch redraws only the touched chunks.
- Rubble shares one mesh and material (GPU instancing), is pooled and capped, and settled cubes sleep. Vacuumed cubes move on a scripted curve.
- Numbers live in data files, not in code, so balance changes never need a rebuild (not yet true in the draft code).
- Saving stores the mine as the list of broken cubes per chunk plus upgrades and story progress.
- Animation is **code-driven** (rigid arm, vacuum and dynamite movement), not skeletal. Static 3D models are created or sourced separately and imported; they need clean, low-polygon meshes (about 3–5k triangles for arms, under 2k for small props) and a blocky low-poly style.

### Go / no-go gates

| Gate | Question |
|---|---|
| 1 | Punching a wall into cubes holds 30 fps with 100 rubble cubes on the low phone |
| 2 | Vacuuming feels good with a thumb |
| 3 | The 10-minute loop makes five strangers say "one more upgrade" |
| 4 | The whole mine runs about 70 minutes with no dead ends |
| 5 | Release candidate: no crash and stable frame rate in a 20-minute heat test on three phones |

---

## 15. The codebase

**Working code:** `UnityProject/Assets/` (see the repository layout above). Start with `Assets/Scripts/Bootstrap/GameBootstrap.cs`, which owns the start-up order, and `UnityProject/README_UNITY_SETUP.md`.

**Original first draft (kept untouched):** the `codebase/` folder holds the first-draft Unity codebase: 19 C# scripts and 1 JSON file (4,264 lines of C#). It was written from the design documents and has not been compiled, run or tested. The table below describes those original files; each was ported (fixed) into the matching folder under `UnityProject/Assets/Scripts/`.

| File | Role |
|---|---|
| `VoxelWorldManager.cs` | Voxel grid, hand-built mine generation, punch and dynamite damage |
| `VoxelChunk.cs` | 16³ chunk: face-culled mesh and collider |
| `VoxelWireframePreview.cs` | Procedural wireframe punch preview |
| `FirstPersonToolController.cs` | Arms, punches, charge, hotbar, vacuum pose, dynamite, headlamp (code-driven animation) |
| `StickyDynamite.cs` | Pooled thrown dynamite, fuse, blast event |
| `RubbleCube.cs`, `RubbleSuctionManager.cs` | Rubble pooling, vacuum suction, blow-out, 60% sweep |
| `Crusher07Controller.cs` | Rollers, crush queue, conveyor, floating cash |
| `CompanyStoreEconomyManager.cs` | Upgrade tables and costs, cash, HUD, auto-punch |
| `economy_config.json` | Economy data (currently not read by any script) |
| `MobileFirstPersonController.cs`, `MobileVirtualJoystick.cs` | Touch walk and look, joystick, hold buttons |
| `UIScreenAndAccessibilityManager.cs` | Screen switching, left-handed mode, tutorial captions |
| `TreasureAndMapManager.cs` | Buried treasure, detector radar, map and room discovery |
| `ExitVaultAndLogbookController.cs` | Logbooks and the lift-gate escape |
| `SaveManager.cs` | Compressed local save |
| `AudioHapticsManager.cs` | Material punch sounds, pitch scaling, Android haptics |
| `DeviceTierScaler.cs` | Tier detection, rubble caps, frame-rate guard |
| `MonetizationServicesManager.cs` | Placeholder ad and purchase hooks |
| `SceneAutoBuilderEditor.cs` | Editor-only one-click scene and UI builder |

### How to import it safely
Follow `UnityProject/README_UNITY_SETUP.md`: clean Unity 6.3 LTS Universal 3D project, Active Input Handling **Both**, TextMeshPro essentials, copy `UnityProject/Assets` in three groups (core and data; runtime scripts and shaders; editor builder), read the first compile error after each group, run **BreakToEscape > Build Complete Playable Scene**, press Play, then build to a phone.

---

## 16. Critical issues found in the first draft (summary)

All of these now have a code change in `UnityProject/Assets/` (status per issue in the status report's "Update 4 November 2026" section). None has been verified in Unity yet. The most important, as found in the first draft:

| ID | Issue |
|---|---|
| P0-1 | At least 10 compile-breaking string literals in three files (unescaped inner quotes in 8 captions; real line breaks inside 2 button labels) |
| P0-2 | The code has never been compiled, run or built |
| P0-3 | Legacy input API and old UI input module (works with the "Both" setting for the prototype; migrate later) |
| P0-4 | The scene builder places the player, crusher, vault and rooms using coordinates from an older, smaller world; the player spawns inside solid rock; nothing is saved |
| P0-5 | A normal punch breaks 1 cube; the Punch Pattern upgrade does not change what a punch breaks |
| P1-1 | Wall collision is rebuilt on every punch and physics queries run every frame |
| P1-2 | Rubble pool can return and pay the same cube twice |
| P1-3 | Saved dug-out walls are not restored; several upgrades are not saved |
| P1-4 | The code does not enforce the punch and vacuum rates the economy model assumes |
| P1-5 | The escape and story chain is unreachable (nothing calls the repair, cable or logbook functions) |
| P1-6 | Ad and purchase fallbacks would give rewards and Remove Ads for free in a release build |
| P1-7 | Pneumatic Hammer, Rock Drill and Conveyor can never be unlocked |
| P1-8 | Three of seven upgrade lines have no shop button |

Still missing after the implementation pass: all audio clips, all production 3D models and UI art, real ad and billing SDKs (adapters and guide exist), and every Unity and device test. Story text, code-built particle effects and a seeded procedural generator (behind a flag) now exist.

---

## 17. Roadmap

| Milestone | Goal | Exit gate |
|---|---|---|
| 0 Project validation | Clean project, input setting, import in groups, fix compile errors and builder placement, first Android build | Zero compile errors; player spawns in the chamber; build installs on a phone |
| 1 Core destruction prototype | Voxel wall, punches, rubble, touch controls, grid-based aiming, profiling | **Gate 1** on a real low-tier phone |
| 2 Vacuum and economy | Vacuum, tank, crusher, cash, enforced rates, shop | **Gate 2** |
| 3 Progression and tools | Bands, all upgrades, dynamite, headlamp, treasure, restored equipment | **Gate 3** |
| 4 Mine and story | Hand-built mine, interactables, logbooks, escape puzzle; then seeded generation | **Gate 4** |
| 5 UI, animation, presentation | Final HUD, screens, tutorial, tool animation, particles, haptics, lighting | — |
| 6 Save system | Full save/restore, migration, replay | Save/load tests pass |
| 7 Audio and assets | Sounds, music, final models and textures | Licence log complete |
| 8 Performance | Tier settings, rubble limits, collision, thermal and long-session tests | **Gate 5** |
| 9 Monetization and release | Ads with consent, Remove Ads, analytics, Play Console, closed test, production | Google approval |

---

## 18. Google Play requirements (checked 3 November 2026; re-verify before release)

| Requirement | Finding |
|---|---|
| Target API level | New apps and updates must target Android 16 (API 36) from 31 August 2026 |
| Play Billing Library | Use version 8 or newer |
| 16 KB page size | Native code must support 16 KB page sizes (official page: new apps and updates targeting Android 15+ since 1 November 2025); verify early with an internal-test upload |
| New personal developer accounts | A closed test with at least 12 testers opted in continuously for 14 days is required before applying for production access |

Not yet verified from official sources: Data Safety form, content-rating questionnaire, ads declaration, store-listing asset sizes, app size limits, registration fee. Check these in Play Console.

---

## 19. Working agreements

- **Preserve design decisions.** Keep the concept, loop, setting, identity pillars and the decisions in this README. Do not rewrite working systems without a reason; fix the issues in the status report in priority order (P0, then P1).
- **Do not claim what has not been verified.** A feature counts as working only after it compiles, runs in the editor and passes a test on a real Android phone. Static checks are not builds.
- **Keep the numbers in one place.** The economy spreadsheet and the data files must agree; change both together.
- **Small, reviewable steps.** The project owner is a solo developer who is new to Unity. Explain steps in plain language, make one change group at a time, keep the project in Git, and always leave the game in a runnable state.
- **Test early on real phones.** Use three tiers (low, mid, high). Profile before optimising; treat all performance numbers as targets until measured.
- **Update the status report** at every gate, replacing "implemented in code" with "demonstrated" only after a real test.
- **Cut order if behind schedule:** Rock Drill, Conveyor, extra treasure types, dynamite, third logbook. **Never cut:** punch, vacuum, crusher, the seven upgrades, the lift escape.

---

## 20. Originality and publishing considerations

The game was researched against an existing commercial game to understand mechanics and pacing only. The original concept wording was noted to be very close to that game's store description, so the project must develop its own identity:

- Own name, art direction, UI design, mine layout, characters, story text, visual effects, audio, store listing and marketing.
- Never use another game's protected assets, branding, text, models, textures, sounds or distinctive presentation.
- Keep a **licence log** (asset, source, licence, date, link) for every third-party file.
- Search Google Play for the name before committing to it.
- Distinguishing identity to keep: the Greywater Deep mine, the Company Store, the crew logbooks, the hazard-stripe industrial UI, the shaft lift escape.

---

## 21. Glossary

- **Cube / voxel:** one block of wall, 0.5 m wide.
- **Band:** one rock layer, harder than the last.
- **Rubble:** broken cubes that bounce and pile up.
- **Rubble cap:** the maximum bouncing cubes a phone handles at once.
- **Chunk:** a 16 × 16 × 16 block of cubes that is redrawn as a unit.
- **Tier:** a device class (low, mid, high) with its own quality settings.
- **Prototype:** a rough version that only tests whether the core feels good.
- **Gate:** a go / no-go checkpoint.
- **Rewarded ad:** an ad the player chooses to watch in return for a bonus.
