# Break to Escape — Economy Tables

These tables are generated from `Break_to_Escape_Economy.xlsx` (live formulas, recalculated without errors). They are **starting guesses** to be tuned with real playtest data. The spreadsheet is the source of truth for the formulas; this file makes the same numbers readable as plain text.

## How the model works

- Cube **hit points (HP)** accumulate damage from punches. Hits to break a cube = `ceil(HP / punch damage)`.
- **Punch throughput** (cubes per second) = `cubes per punch × punches per second × pattern efficiency (0.6) / hits to break`.
- **Overall throughput** = the minimum of punch, vacuum and crusher rates.
- **Time in band** = `cubes / throughput + (cubes / tank capacity) × trip seconds (14) + band overhead seconds`.
- **Cash earned** = `cubes × cube value × ore-sense multiplier × collection efficiency (0.9)`.
- **Cash needed** = cost of the upgrade levels planned for the start of the next band.
- Healthy earned/needed ratio is 1.0 to 2.5. Target full-run time is 60 to 90 minutes.

> **Important:** the current code does not yet enforce the punch-rate and vacuum-rate limits this model assumes (see the status report, issue P1-4), so the predicted run time does not yet apply to the built game.

## Upgrade settings

| Upgrade line | Base cost (to reach level 2) | Cost growth per level | Base effect (level 1) | Effect step | What it does |
|---|---|---|---|---|---|
| Punch Power | $150 | ×2.2 | 10 | 1.45 | Damage per punch to every cube in the pattern |
| Punch Speed | $200 | ×2.2 | 1.5 | 0.25 | Punches per second (adds per level) |
| Punch Pattern | $250 | ×2.2 | see list | see list | Cubes hit by one punch (list below) |
| Vacuum Suction | $175 | ×2.2 | 6 | 1.3 | Cubes per second the vacuum pulls in |
| Tank Capacity | $125 | ×2.2 | see list | see list | Cubes the vacuum tank holds (list below) |
| Crusher Speed | $225 | ×2.2 | 8 | 1.3 | Cubes per second the crusher processes |
| Ore Sense | $375 | ×2.2 | 1 | 0.08 | Cash multiplier on every cube (adds per level) |

Growth type: Punch Power and Vacuum Suction and Crusher Speed grow multiplicatively per level; Punch Speed and Ore Sense add a fixed step per level; Punch Pattern and Tank Capacity use hand-designed lists.

## Effect per level

| Level | Punch Power (damage) | Punch Speed (punches/s) | Punch Pattern (cubes) | Vacuum Suction (cubes/s) | Tank Capacity (cubes) | Crusher Speed (cubes/s) | Ore Sense (× cash) |
|---|---|---|---|---|---|---|---|
| 1 | 10 | 1.5 | 5 | 6 | 60 | 8 | 1 |
| 2 | 14.5 | 1.75 | 9 | 7.8 | 90 | 10.4 | 1.08 |
| 3 | 21 | 2 | 13 | 10.1 | 130 | 13.5 | 1.16 |
| 4 | 30.5 | 2.25 | 21 | 13.2 | 190 | 17.6 | 1.24 |
| 5 | 44.2 | 2.5 | 29 | 17.1 | 270 | 22.8 | 1.32 |
| 6 | 64.1 | 2.75 | 37 | 22.3 | 380 | 29.7 | 1.4 |
| 7 | 92.9 | 3 | 49 | 29 | 530 | 38.6 | 1.48 |
| 8 | 134.8 | 3.25 | 61 | 37.6 | 740 | 50.2 | 1.56 |
| 9 | 195.4 | 3.5 | 77 | 48.9 | 1030 | 65.3 | 1.64 |
| 10 | 283.3 | 3.75 | 97 | 63.6 | 1450 | 84.8 | 1.72 |

## Cumulative cash cost to reach each level

| Level | Punch Power | Punch Speed | Punch Pattern | Vacuum Suction | Tank Capacity | Crusher Speed | Ore Sense |
|---|---|---|---|---|---|---|---|
| 1 | $0 | $0 | $0 | $0 | $0 | $0 | $0 |
| 2 | $150 | $200 | $250 | $175 | $125 | $225 | $375 |
| 3 | $480 | $640 | $800 | $560 | $400 | $720 | $1,200 |
| 4 | $1,206 | $1,608 | $2,010 | $1,407 | $1,005 | $1,809 | $3,015 |
| 5 | $2,803 | $3,738 | $4,672 | $3,270 | $2,336 | $4,205 | $7,008 |
| 6 | $6,317 | $8,423 | $10,528 | $7,370 | $5,264 | $9,476 | $15,793 |
| 7 | $14,047 | $18,730 | $23,412 | $16,389 | $11,706 | $21,071 | $35,119 |
| 8 | $31,054 | $41,406 | $51,757 | $36,230 | $25,879 | $46,582 | $77,636 |
| 9 | $68,470 | $91,293 | $114,116 | $79,881 | $57,058 | $102,705 | $171,175 |
| 10 | $150,784 | $201,045 | $251,306 | $175,914 | $125,653 | $226,175 | $376,959 |

To price a single level, subtract the previous row.

## Rock bands

| Band | Name | Look | Cube HP | Cube value | Cubes to clear | Overhead (s) | Notes |
|---|---|---|---|---|---|---|---|
| B1 | Concrete lining | Grey concrete & rebar, work lamps | 10 | $2 | 850 | 45 | Tutorial band: every punch works, fast first wins |
| B2 | Packed earth & timber | Dirt, rotten timber supports | 25 | $3.5 | 1,500 | 60 | First hidden room: old crew locker |
| B3 | Sandstone gallery | Soft layered stone, old rail tracks | 60 | $6 | 2,300 | 70 | Equipment bay: first restored machine |
| B4 | Granite core | Hard speckled granite, heavy machinery | 140 | $10 | 3,500 | 80 | Pump room and generator |
| B5 | Ore veins | Copper and iron seams, rich cubes | 260 | $30 | 5,000 | 90 | Big payday band: ore multiplier matters |
| B6 | Crystal vault | Glowing geode crystals, humming light | 420 | $70 | 7,000 | 100 | Final band before the main shaft lift |

Global settings: pattern efficiency 0.6; trip 14 s; collection efficiency 0.9; target run 60–90 minutes; healthy cash ratio 1.0–2.5.

## Planned upgrade levels at the start of each band

| Upgrade | B1 | B2 | B3 | B4 | B5 | B6 | End (all max) |
|---|---|---|---|---|---|---|---|
| Punch Power | 1 | 2 | 3 | 5 | 6 | 8 | 10 |
| Punch Speed | 1 | 2 | 3 | 4 | 5 | 7 | 10 |
| Punch Pattern | 1 | 2 | 3 | 4 | 5 | 7 | 10 |
| Vacuum Suction | 1 | 2 | 3 | 4 | 5 | 7 | 10 |
| Tank Capacity | 1 | 2 | 3 | 4 | 5 | 7 | 10 |
| Crusher Speed | 1 | 2 | 3 | 4 | 5 | 7 | 10 |
| Ore Sense | 1 | 1 | 2 | 3 | 4 | 6 | 10 |

## Predicted pacing (model output)

| Metric | B1 | B2 | B3 | B4 | B5 | B6 |
|---|---|---|---|---|---|---|
| Punches to break a cube | 1 | 2 | 3 | 4 | 5 | 4 |
| Punch throughput (cubes/s) | 4.5 | 4.7 | 5.2 | 7.1 | 8.7 | 22.1 |
| Overall throughput (cubes/s) | 4.5 | 4.7 | 5.2 | 7.1 | 8.7 | 22.1 |
| Bottleneck | Punching | Punching | Punching | Punching | Punching | Punching |
| Time in band (min) | 7.2 | 10.2 | 12.7 | 13.9 | 15.4 | 10 |
| Cumulative time (min) | 7.2 | 17.4 | 30.1 | 43.9 | 59.3 | 69.4 |
| Cash earned in band | $1,530 | $4,725 | $13,414 | $36,540 | $167,400 | $617,400 |
| Cash needed for next upgrades | $1,125 | $2,850 | $7,867 | $15,711 | $110,603 | $1,369,681 |
| Earned / needed | 1.36 | 1.66 | 1.71 | 2.33 | 1.51 | 0.45 |
| Flag | OK | OK | OK | OK | OK | Endgame: maxing out is optional |

**Total predicted run time:** 69.4 minutes (YES). Total cash earned in run: $841,009. Cash to max every upgrade: $1,507,837 (maxing out is optional).

**Observation:** punching is the bottleneck in every band, so vacuum and crusher upgrades are never the limiting factor in the model. Consider lowering planned vacuum/crusher levels or raising band difficulty. Band 6 'needed' equals the cost of maxing everything, so a ratio below 1 is expected there.

## Not modelled (by design, version 0.1)

Dynamite, treasure cubes and chests, ad boosts, side-room exploration, and player mistakes.
