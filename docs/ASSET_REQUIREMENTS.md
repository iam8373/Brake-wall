# Break to Escape — Asset Requirements

No art, model or sound files exist yet. The code runs with primitive placeholders and silent, empty clip slots. This list says exactly what to make or source. Every third-party file must be added to `docs/ASSET_LICENCES.md` before it is committed.

**Style reminders (from the design):** chunky low-poly shapes, warm lamp light against cold dark rock, a clear colour per band, glowing crystal accents, black-and-yellow hazard-stripe UI, readable at phone size. Nothing copied from another game.

## 1. Sound effects and music

Put clips in `UnityProject/Assets/Resources/Audio/` with the exact file names below (any Unity-supported format; OGG recommended). Names are mapped to game events in `Assets/Resources/Data/audio.json`; add or rename variants there without touching code. Missing clips are skipped silently.

| Clip name(s) | Purpose | Length | Loop? | Mix level | Notes and suggested sources |
|---|---|---|---|---|---|
| `punch_concrete_01..03` | Punch on band 1 concrete | 0.15–0.3 s | One-shot | −10 dB | Dull thud with a little grit; record a fist on a concrete slab through a glove, or layer a low thump with gravel. |
| `punch_earth_01..03` | Band 2 packed earth and timber | 0.15–0.3 s | One-shot | −10 dB | Softer, woody crunch. |
| `punch_sandstone_01..03` | Band 3 sandstone | 0.15–0.3 s | One-shot | −10 dB | Brittle crumble. |
| `punch_granite_01..03` | Band 4 granite | 0.15–0.3 s | One-shot | −9 dB | Hard, ringing knock. |
| `punch_ore_01..03` | Band 5 ore veins | 0.15–0.3 s | One-shot | −9 dB | Metallic clank on top of stone. |
| `punch_crystal_01..03` | Band 6 crystal | 0.2–0.4 s | One-shot | −10 dB | Glassy chime + crack. |
| `punch_treasure_01..02` | Any treasure seam | 0.3 s | One-shot | −8 dB | Bright sparkle layer. |
| `punch_charged_01` | Sweetener on charged punches | 0.4 s | One-shot | −8 dB | Whoosh + deep boom tail. |
| `vacuum_loop` | Vacuum running (pitch rises as the tank fills) | 2–4 s | Loop | −14 dB | Seamless loop; code shifts pitch 0.72–1.45. |
| `crusher_loop` | Crusher 07 grinding | 2–4 s | Loop | −14 dB | Heavy rollers and gravel. |
| `coin_01..02` | Cash payout (pitch grows with value) | 0.2 s | One-shot | −12 dB | Short metallic chime; avoid a recognisable "coin" sound from another game. |
| `dynamite_blast_01..02` | Dynamite explosion | 1.5–2.5 s | One-shot | −4 dB | Deep boom with rubble rain tail. |
| `radar_ping` | Treasure detector ping | 0.15 s | One-shot | −16 dB | Soft sonar blip. |
| `ui_click` | Button press | 0.05 s | One-shot | −16 dB | Dry mechanical click. |
| `cave_ambience_loop` | Background cave tone | 30–60 s | Loop | −22 dB | Low drips, distant creaks, air movement. |
| `music_calm_loop` | Calm music bed | 60–120 s | Loop | −20 dB | Must never fight the effects; sparse, low-mid. |

Sources: record your own, or use royalty-free libraries whose licence allows commercial mobile games (for example Freesound items under CC0 or CC-BY with attribution noted, Kenney audio packs under CC0, or purchased Unity Asset Store packs). Record each file's licence in `docs/ASSET_LICENCES.md`.

## 2. 3D models

Import as FBX, scale 1 unit = 1 metre, Y up, facing +Z. Low-poly, flat-shaded or simple vertex-coloured; one material per model where possible. Triangle budgets are maximums.

| Model | Replaces placeholder | Triangles | Texture | Naming | Notes |
|---|---|---|---|---|---|
| Work glove (left, right) | `BasicGlove` cubes | 3–5k each | 256² or vertex colour | `SM_Glove_L`, `SM_Glove_R` | Rigid; animated by code (no skeleton needed). |
| Work gauntlet (left, right) | `WorkGauntlet` cubes | 3–5k each | 256² | `SM_Gauntlet_L`, `SM_Gauntlet_R` | Heavier, hazard-yellow trim. |
| Vacuum: Dust-buster | `DustBuster` cylinder | ≤ 2k | 256² | `SM_Vacuum_T1` | Pivot at the grip; nozzle tip as a child `Nozzle`. |
| Vacuum: Ore Hoover | `OreHoover` cube | ≤ 2.5k | 256² | `SM_Vacuum_T2` | Bigger tank. |
| Vacuum: Suction Cannon | `SuctionCannon` capsule | ≤ 3k | 256² | `SM_Vacuum_T3` | Industrial, purple-grey. |
| Crusher 07 | Frame plate and two rollers | ≤ 2k | 256² | `SM_Crusher07`, `SM_Crusher07_Roller` | Rollers as separate meshes that spin about their long axis. |
| Lift gate | Frame and bars | ≤ 2k | 256² | `SM_LiftGate`, `SM_LiftGate_Door` | Door slides up 2.8 m. |
| Dynamite stick | Red cylinder | ≤ 500 | 128² | `SM_Dynamite` | With a fuse tip child. |
| Headlamp | none (helmet light) | ≤ 500 | 128² | `SM_Headlamp` | Optional; only seen in the gesture. |
| Logbook | Brown box on a crate | ≤ 500 | 128² | `SM_Logbook` | Name on the cover is in-game text, not texture. |
| Hard hat | Yellow half sphere | ≤ 800 | 128² | `SM_HardHat` | Names (Quill, Pike, Bellamy) shown by UI text. |
| Crew locker | Grey box | ≤ 800 | 128² | `SM_Locker` | Door can open later. |
| Generator | Dark box | ≤ 2k | 256² | `SM_Generator` | Lamp position child `Lamp`. |
| Winch cable drum | Cylinder | ≤ 1.5k | 128² | `SM_CableDrum` | |
| Fuel cell | Green capsule | ≤ 500 | 128² | `SM_FuelCell` | Emissive green. |
| Repair parts (piston, motor, drill bit) | Orange cylinder | ≤ 800 each | 128² | `SM_Part_Piston`, `SM_Part_Motor`, `SM_Part_DrillBit` | |
| Dynamite crate | Red box | ≤ 300 | 128² | `SM_DynamiteCrate` | |
| Company Store terminal | Yellow box with screen | ≤ 1.5k | 256² | `SM_StoreTerminal` | Screen as a separate emissive material. |
| Wall note | Paper on a post | ≤ 200 | 128² | `SM_WallNote` | |
| Named treasures (Shift Bell, Brass Level, Founder's Medal, Singing Geode) | Gold disc | ≤ 1k each | 128² | `SM_Treasure_*` | |

**Import settings (Android):** Read/Write off, mesh compression Medium, Optimize Mesh on, generate lightmap UVs off, normals Import (flat-shaded look), animation type None, textures ASTC 6×6, max size 256 (512 for UI atlases), mipmaps on for world textures and off for UI.

Wire real models by replacing the placeholder meshes in `PlaceholderFactory` (story objects), the scene builder (viewmodel, crusher) and the prefabs in `Assets/Generated/Prefabs`.

## 3. Voxel textures (optional)

Chunk meshes already carry per-face UVs (0..1 per face). A later texture atlas can hold one 32 × 32 tile per material (6 bands + 3 treasure seams); the voxel shader currently uses vertex colours only.

## 4. UI sprites

Hazard-stripe black-and-yellow style; 9-sliced where marked. Import as Sprite (2D and UI), ASTC, no mipmaps.

| Sprite | Size | Use |
|---|---|---|
| `ui_panel_dark` (9-slice) | 64² | Panels, cash box, tank gauge |
| `ui_panel_hazard` (9-slice) | 64² | Primary buttons (yellow with stripe edge) |
| `ui_button_round` | 256² | PUNCH and VACUUM buttons |
| `ui_joystick_ring`, `ui_joystick_knob` | 256², 128² | Move stick |
| `ui_icon_fist`, `ui_icon_vacuum`, `ui_icon_dynamite`, `ui_icon_lamp` | 96² | Hotbar (icons plus text, never colour only) |
| `ui_icon_map`, `ui_icon_pause`, `ui_icon_store` | 96² | Top-right buttons |
| `ui_crosshair_ring` | 32² | Crosshair |
| `ui_map_paper` | 512² | Map background |
| `ui_room_box`, `ui_room_unknown` | 64² | Map rooms (known, "?") |
| `ui_detector` | 96² | Treasure detector signal |
| `ui_card_upgrade` (9-slice) | 128² | Store cards |
| Font | — | A bold, readable sans or slab face with a licence for apps (SIL Open Font Licence recommended); create a TextMeshPro font asset. |

## 5. App icon and store art

| Item | Size | Notes |
|---|---|---|
| App icon (adaptive) | 512 × 512 store icon; foreground and background layers 432 × 432 with safe zone | A gloved fist punching through rock with a hazard stripe. Original artwork. |
| Feature graphic | 1024 × 500 | No text that copies another game's branding. |
| Phone screenshots | Landscape, at least 4 | Taken from the real build. |

Verify current sizes in Play Console before producing final files.
