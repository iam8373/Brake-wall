# Break to Escape – Game Design Document

*Version 0.1 · Android only (Google Play) · Solo project · Written in plain language, no experience needed*

## 1. The game in one breath

You are the last worker trapped in a collapsed underground mining complex. **Break** the rock with your bare hands into chunky cubes, **collect** them with a vacuum, **crush** them for cash, then spend that cash at the mine's still-running Company Store to **restore** old equipment and dig deeper, until you find the way **out**.

**Tagline ideas:** *Break. Collect. Restore. Escape.* / *The mine sealed. You didn't.*

**Identity (your words, kept as the compass for every decision):** destruction, resource collection, equipment progression. If a feature does not serve one of those three, it waits.

## 2. World, story and tone

### Setting

**Greywater Deep** (working name, a made-up place): a forgotten underground mining complex. Its tunnels were sealed after a collapse. Heavy machinery, rusted rails, work lamps, mineral deposits and glowing crystal seams fill the dark.

### Story

Shift 9 at Greywater Deep never ended. A collapse sealed the network and the company walked away. You wake up in the Collapse Chamber with nothing but gloves and bad luck. Old Crusher 07 still runs on reserve power, and the automated **Company Store** terminal still works, selling you equipment at ridiculous prices with cheerful corporate messages. To get out you must recover resources, restore broken equipment and reach the Main Shaft lift.

### Who is in it

- **You (Worker 17):** silent, never shown, only your hands.
- **Dispatch:** the automated radio and Company Store voice. It appears as short on-screen text lines, cheerful and a little sinister.
- **The lost crew:** Foreman Quill, Surveyor Pike and Cook Bellamy. They left logbooks, notes on walls and hard hats with their names. They are never seen, so the game stays gentle and family-safe. Their notes hide the lift-gate code.

### Tone

Dark comedy with real hope. Short, dry captions such as *"Day 1. The mine is not impressed with you."* Nothing graphic.

### Things to keep original (do this on purpose)

The reference game you studied uses a concrete cell, skeletons, paper-note UI and a combination-lock vault door. Break to Escape instead has a mine, a corporate Company Store, hazard-stripe industrial UI, crew logbooks and a shaft lift. Keep the names, art, UI wording and story different from any other game. Search Google Play for the name before you commit.

## 3. The core loop

**Every 60 seconds:** Punch the rock, cubes spill out, vacuum them up, blow or carry them to the crusher, see cash pop up, buy something small.

**Every 10 minutes:** save up for a bigger upgrade or restored machine, which opens the next harder rock layer.

**Every session (5–10 minutes):** make visible progress, find one new thing (a room, a note, a crate), and stop at a natural pause.

**Whole game:** about 60–90 minutes from first punch to escape (the economy sheet currently predicts about 69 minutes).

## 4. Controls on a phone

Landscape only. Every control is reachable with thumbs and none cover the action.

| Area | Control |
| --- | --- |
| Left thumb | Virtual joystick to walk |
| Right thumb, empty space | Drag to look around |
| Right thumb, big button | **PUNCH**: tap to punch, hold to charge a bigger punch |
| Right thumb, second button | **VACUUM**: hold to suck, toggle to blow cubes out |
| Bottom centre | Hotbar: fists/tool, vacuum, dynamite, headlamp (tap to switch) |
| Top left | Cash and cash per second |
| Top right | Map and pause |

**Helpers (they matter on touch):**

- **Aim assist:** the punch snaps to the cube nearest the crosshair.
- **Auto-punch:** an unlockable upgrade that punches for you while you hold the button.
- **Left-handed mode:** mirrors the layout.
- **Haptics:** a short vibration for hits, a longer one for big breaks. Can be turned off.

## 5. How things work

### The wall

The mine is one big block made of cubes of 0.5 m. Each cube has a material, hit points (HP) and a cash value. Walls are about 6 m thick near the start. A punch hits a shape of cubes (a plus sign at first, growing with upgrades). A wireframe preview shows exactly which cubes will break.

### Rubble

Broken cubes become small physical cubes that bounce and pile up. To keep phones running smoothly there is a **rubble cap** (about 150 cubes at once to start). If the pile grows past the cap, the oldest settled cubes are swept away and turned into a small automatic cash bonus (about 60% value), so nothing is wasted and nothing lags.

### Vacuum

Hold to pull cubes into a tank. The tank has a size. When full, walk to the crusher and blow the cubes out toward it, or drop them in. Upgrades make it stronger, wider and bigger: Dust-buster, Ore Hoover, Suction Cannon.

### Crusher

Crusher 07 in the Collapse Chamber pays cash for every cube. Upgrades raise its speed. Later a conveyor appears so cubes feed it automatically.

### Dynamite

Stuck on a wall, 3-second fuse, big blast. Found in crates or earned from ads. Great for clearing hard rock, but it destroys treasure in its blast radius, so there is a choice.

### Light

Deeper rooms are dark. The headlamp has 3 simple levels. Lamps on walls and glowing crystals light the way.

## 6. The rock layers (bands)

Six bands. Each is harder and pays more. Numbers come from the Economy spreadsheet and are starting guesses to be tested in the prototype.

| # | Band | Look | Cube HP | Cube value | Cubes to clear | Planned time |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Concrete lining | Grey concrete, rebar, work lamps | 10 | $2 | 850 | \~7 min |
| 2 | Packed earth and timber | Dirt, rotten supports | 25 | $3.50 | 1,500 | \~10 min |
| 3 | Sandstone gallery | Layered stone, old rails | 60 | $6 | 2,300 | \~13 min |
| 4 | Granite core | Hard speckled rock, heavy machinery | 140 | $10 | 3,500 | \~14 min |
| 5 | Ore veins | Copper and iron seams | 260 | $30 | 5,000 | \~15 min |
| 6 | Crystal vault | Glowing geode crystals | 420 | $70 | 7,000 | \~10 min |

**Treasure cubes:** gold, copper and glowing crystal cubes hide in the rock and pay several times normal value. Glowing crystals light up so players can spot them.

## 7. The mine map (version 1: one hand-built mine)

- **Collapse Chamber:** start, Crusher 07, the Company Store terminal and the first tutorial.
- **West: Old Gallery:** a dead-end tunnel with the foreman's locker and the first logbook.
- **East: Equipment Bay:** broken machines to restore, second logbook.
- **South: Pump Room:** the generator, fuel cell and third logbook.
- **North: Main Shaft:** the lift gate (the exit), reachable only at the end of band 6.
- **Hidden nooks:** small side rooms with treasure, dynamite and wall notes in every band.

The player digs freely in any direction. A hand-drawn in-game map slowly fills in as they explore, and unexplored spots are marked with a question mark.

**Later update:** a "New Shift" mode generates a different mine each time from a random seed. It is a big feature, so it is deliberately not in version 1.

## 8. Upgrades and equipment

### Seven numeric upgrades (10 levels each)

These are priced in the Economy spreadsheet.

| Upgrade | What it does |
| --- | --- |
| Punch Power | More damage to every cube in the punch |
| Punch Speed | More punches per second |
| Punch Pattern | Wider punch shape (5 cubes up to 97) |
| Vacuum Suction | Pulls cubes faster |
| Tank Capacity | Carries more cubes per trip |
| Crusher Speed | Crushes faster |
| Ore Sense | Every cube pays a bit more |

### Restored equipment (story unlocks)

Broken machines found in rooms are repaired at the Company Store for cash plus a found part. Each changes how digging feels and looks.

| Equipment | Where found | Effect |
| --- | --- | --- |
| Work Gauntlets | Old Gallery (band 2) | New hand model, small power boost |
| Pneumatic Hammer | Equipment Bay (band 3) | Faster punches, tighter pattern |
| Rock Drill | Pump Room (band 5) | Long narrow tunnel pattern for deep digging |
| Ore Hoover, then Suction Cannon | Bought in the store | New vacuum look and range |
| Conveyor | Equipment Bay | Feeds cubes to the crusher automatically |

## 9. The escape

To ride the lift out of the Main Shaft you need three things:

1. **Power:** repair the generator in the Pump Room with a fuel cell.
2. **Cable:** find the winch cable in the Ore veins.
3. **Gate code:** three numbers, one in each crew logbook.

Then a short ending plays: daylight, a dry final line from Dispatch, and a credits screen with the player's total time and cubes broken. A "New Shift" teaser tells players more is coming.

## 10. Ads and purchases (decided so far)

You chose **rewarded ads plus a remove-ads purchase**. Other ways to earn money will be decided later.

- **Rewarded ads (always optional, always the player's choice):**
  - "Double cash for 5 minutes" button, available at most once per 10 minutes.
  - "Free dynamite crate" at the Company Store, a few times per session.
- **Remove Ads purchase (one time):** never shows ad prompts and gives the ad rewards for free at a gentler rate.
- **Rules:** no ads during the first 5 minutes, no pop-up ads that interrupt digging, and a clear privacy and consent screen in line with Google's rules.

## 11. Teaching the player (first 3 minutes)

Short side captions in the game's voice, one idea at a time. Each appears only when the player does the thing:

1. *"Punch the wall. It has it coming."*
2. *"Cubes are cash. Hold VACUUM."*
3. *"Crusher 07 is hungry. Feed it."*
4. *"The Company Store is open. Of course it is."* (first upgrade, made affordable in under 90 seconds)
5. *"Dig deeper."*

Rule: the first cash payout and first upgrade must happen within 2 minutes.

## 12. Look and sound

**Look:** chunky cubes, warm lamp light against cold dark rock, a clear colour for each band, glowing crystal accents, industrial hazard-stripe UI in black and yellow, readable at phone size.

**Sound (requirements, not recordings yet):** satisfying punch impacts that change with the material, a vacuum loop whose pitch rises as the tank fills, a crusher grind, a coin-style cash chime that grows with the payout, low cave ambience, a deep dynamite boom, a calm music bed that never fights the effects. All sounds must be royalty-free or created by you, with licences saved.

## 13. Accessibility

Left-handed layout, vibration on/off, adjustable look sensitivity, large readable text, no information shown only by colour (icons and shapes as well), and a reduce-flashing option for explosions.

## 14. What is in version 1 and what is not

| In version 1 | Not in version 1 |
| --- | --- |
| One hand-built mine, six bands | Random mines ("New Shift") |
| Seven upgrades, four restored tools | Multiplayer or leaderboards |
| Vacuum, crusher, dynamite, headlamp | Cosmetic shop |
| Three crew logbooks and the lift escape | Many languages (English only) |
| Rewarded ads and remove-ads purchase | Other purchases |
| Local save | Cloud save (added after launch) |

## 15. Risks to watch

- **Too many cubes for phones:** solved by the rubble cap and the quality tiers in the technical plan. Tested first.
- **Touch aiming feels bad:** aim assist and a charged punch preview are built early and playtested.
- **A first Unity project is big:** the schedule has a learning phase and a cut list.
- **Players farm easy rock forever:** each band pays modest amounts and the side rock is thin.
- **Looking too much like the reference game:** original setting, names, UI and story, checked before release.

## 16. How we will know it is fun

Give the prototype to five people who have never seen it and watch silently. Success means each one:

- breaks a wall and picks up cubes within 60 seconds without help;
- says "one more upgrade" at least once;
- wants to keep playing after 10 minutes.

## 17. Glossary

- **Cube / voxel:** one block of wall, 0.5 m wide.
- **Band:** one rock layer, harder than the last.
- **Rubble:** broken cubes that bounce and pile up.
- **Prototype:** a rough, ugly version that only tests whether the core feels good.
- **Rubble cap:** the maximum bouncing cubes the phone has to handle at once.
- **Rewarded ad:** an ad the player chooses to watch in return for a bonus.
