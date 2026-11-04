# Break to Escape — Touch-Screen Wireframes (text specification)

This file describes the five landscape wireframes in words and coordinates, so the layout can be read without opening the images. The exported wireframe images or drawing sit in the same folder.

- **Format:** landscape phone, drawn on an **844 × 390** artboard. Positions are in artboard pixels (origin top-left).
- **Style of the wireframes:** light grey background, dark outlines, yellow for the primary controls. The final look is industrial: black and yellow hazard-stripe UI, readable at phone size.
- **Design goal:** every control reachable with thumbs, nothing important under the thumbs, the centre of the play screen kept clear.

## Global rules

| Rule | Detail |
|---|---|
| Tap targets | At least 48 dp (about 9 mm) |
| Notches and cut-outs | Keep a safe margin; handle the device safe area |
| Left-handed mode | Mirrors every screen, including the look-drag zone |
| Centre of the play screen | Stays free of controls |
| Ad popup | **No thanks** is never smaller or less visible than **Watch ad** |

## Screen 1 — Play screen

| Element | Position and size | Behaviour |
|---|---|---|
| Game view area | Centre, about 324 × 150 at (260, 90) | Dashed region: keep clear |
| Crosshair | Small ring at about (416, 158) | Aim point for punches |
| Cash box | (14, 12), 170 × 46 | Shows cash (example "$ 1,317") and income per second (example "+$10.7 / s") |
| Tank gauge | (352, 12), 140 × 40 | "TANK 54 / 150" (cubes held / capacity) |
| MAP button | Top right, 56 × 56, 78 px from the right edge | Opens the map |
| Pause button (II) | Top right, 56 × 56, 14 px from the right edge | Opens the pause screen |
| MOVE stick | Bottom left circle, 132 × 132 at (24, 234) | Virtual joystick to walk |
| DRAG TO LOOK zone | Right side, 270 × 150 at (560, 84) | Drag to rotate the camera |
| PUNCH button | Bottom right circle, 122 × 122, 28 px from right, top 244 | Tap = punch; hold = charge (label "hold = charge") |
| VACUUM button | Circle 84 × 84, 172 px from right, top 290 | Hold to suck; a toggle switches to blow-out mode |
| Hotbar | Bottom centre, four 56 × 56 slots with 8 px gaps, starting at (300, 322) | Slots: FIST (highlighted), VAC, DYN ×3, LAMP; tap to switch |

Note: the wireframe does not draw the suction/blow toggle as a separate button; the design says the vacuum has a hold-to-suck action and a toggle for blow-out.

## Screen 2 — Company Store (upgrades)

| Element | Position and size | Behaviour |
|---|---|---|
| Title "COMPANY STORE" | (14, 12), 300 × 44 | — |
| Cash box | (330, 12), 170 × 44 | Current cash |
| CLOSE button | Top right, 88 × 44, 14 px from right | Closes the store |
| Card grid | Starts at (14, 70), width 816, **4 columns**, 10 px gap, rows 146 px high | 8 cards in 2 rows |

Cards (7 upgrade lines plus 1 ad card). Each upgrade card shows the line name, "Lv a → b", and a **BUY $price** button (120 × 44):

1. Punch Power
2. Punch Speed
3. Punch Pattern
4. Vacuum Suction
5. Tank Capacity
6. Crusher Speed
7. Ore Sense
8. Ad boost card (highlighted): "Double cash, 5 min" with a WATCH AD button

## Screen 3 — Map

A hand-drawn paper map (panel at (162, 12), 520 × 340) with the title "MAP". Rooms are boxes:

| Room | Position and size | Label |
|---|---|---|
| Main Shaft | (330, 64), 184 × 64 | "locked – lift gate" |
| Collapse Chamber | (330, 148), 184 × 84, highlighted | "you are here" |
| Old Gallery | (196, 160), 110 × 60 | "dead end" |
| Equipment Bay | (538, 160), 126 × 60 | "door?" |
| Pump Room | (350, 252), 144 × 60 | "found" |
| Unknown rooms | (538, 252) and (196, 252), 60 × 48 | "?" |

Footer text: "Tap anywhere or MAP to put it away". Undiscovered rooms stay marked with a question mark until entered.

## Screen 4 — Pause

A centred panel at (260, 60), 324 × 270 over the dimmed play view:

- Title "PAUSED"
- **RESUME** (primary, yellow), 240 × 52
- **SETTINGS**, 240 × 52
- **QUIT TO MENU**, 240 × 52
- Small note: left-handed mode and vibration are inside Settings

## Screen 5 — Rewarded ad offer

A dimmed overlay with a centred card at (232, 80), 380 × 230:

- Title: "Double cash for 5 minutes?"
- Subtitle: "Watch a short ad. Totally optional."
- Two buttons of **equal size** (150 × 56): **WATCH AD** (primary) and **NO THANKS**
- Caption: both buttons are the same size on purpose

## Settings screen (required, not yet drawn)

Left-handed mode, vibration on/off, look sensitivity, reduce flashing, graphics quality tier. This screen is listed in the design but has no wireframe yet.

## Relationship to the implementation

The current code does not yet build the Map, Pause, Settings or Ad-offer panels, and builds only a first version of the HUD and Store. See `docs/Break_to_Escape_Development_Status_Report.md` (issues P0-4, P1-8, P2-6).
