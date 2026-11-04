# Break to Escape – Technical Budget and Production Plan

*Version 0.1 · Android only (Google Play) · Solo, first game, no Unity experience yet · Companion to the Game Design Document and the Economy spreadsheet*

No code is written in this plan. Code starts in Phase 1, and only when you say so.

# Part A – Technical budget

## A1. What a "budget" means here

A phone has limited speed, memory and battery. A technical budget is a list of limits the game must stay inside, so that it runs smoothly on a cheap phone and does not overheat. Every feature has to fit inside these limits or be made smaller.

## A2. Tools and settings (decided now, so you never have to guess)

| Choice | Decision | Why |
| --- | --- | --- |
| Engine | Unity 6.3 LTS (or the newest long-term-support version on the day you start) | Best mobile tooling, biggest beginner community. LTS means it is supported for years. |
| Graphics setup | URP (Universal Render Pipeline), the mobile-friendly one | Fast on phones |
| Code style | Normal Unity GameObjects and C# scripts | **Not** ECS/DOTS. DOTS is for huge simulations and is hard even for experts. Our cube counts are small enough without it. |
| Language | C# | Unity's language |
| Code editor | Visual Studio Community (free) or VS Code | Free and works well with Unity |
| Build format | Android App Bundle (.aab), IL2CPP, 64-bit ARM only | What Google Play expects |
| Orientation | Landscape only | Needed for the thumb controls |
| Minimum Android | Android 8 (API 26) to start | Covers nearly all active phones. Check Unity's own minimum for your version. |
| Version control | Git with a private GitHub repository | Free backups and undo for everything |

**Google Play rules to confirm in Play Console before release (they change):**

- New apps and updates must target a recent Android API level. As of the 2026 deadline this is API 36 (Android 16).
- Native code in the app must be compatible with 16 KB memory pages. Pick a Unity version whose release notes say it supports this.
- Use the Play Billing library version that Google currently accepts.

## A3. Test phones

Buy or borrow three physical phones. Emulators are not good enough for performance.

| Tier | What it stands for | Target speed | Resolution scale | Rubble cap | Shadows |
| --- | --- | --- | --- | --- | --- |
| Low | Budget phone, about 3–4 GB RAM, 3–4 years old | 30 fps | 0.7× | 100 cubes | Fake blob shadows only |
| Mid | Current mid-range, 6–8 GB RAM (this is your main target) | 60 fps, or 30 if it heats up | 0.85–1.0× | 150–250 cubes | One soft shadow |
| High | Recent flagship | 60 fps | 1.0× | 400 cubes | Full shadows |

The game picks a tier automatically on first launch and the player can change it in Settings. fps = frames per second, how smooth it looks.

## A4. The performance limits

| Item | Limit |
| --- | --- |
| Time to draw one frame | 33 ms at 30 fps, 16.6 ms at 60 fps |
| Draw calls (separate drawing jobs) | 150 or fewer on the low tier |
| Triangles on screen | 200,000 or fewer low tier, 400,000 mid |
| Memory used | 1.2 GB or less on the low tier (Android closes greedy apps) |
| Download size | Aim for 150 MB or less (check Google's current size limits) |
| Heat | After 20 minutes of play the phone must still hold 30 fps |
| Start-up | 8 seconds or less from tapping the icon to first punch |
| Save file | Under 1 MB |

## A5. How the pieces fit together (plain-language architecture)

The game is built from eleven small parts, each with one job. When you are lost, find the part that owns the problem.

| # | Part | Its one job | Built in |
| --- | --- | --- | --- |
| 1 | **Input** | Turns touches into walk, look, punch, vacuum, tool switch | Phase 1 |
| 2 | **World** | Stores the mine as a grid of cubes (material, HP, value) and knows which are broken | Phase 1 |
| 3 | **Mesh builder** | Draws only the surface faces of the cubes, and redraws a small area when you punch | Phase 1 |
| 4 | **Tools** | Fists, vacuum, dynamite, headlamp as separate pieces | Phase 1–3 |
| 5 | **Rubble** | Bouncing broken cubes: pool, cap, pickup, sweep-up | Phase 1 |
| 6 | **Economy** | Cube values, crusher, shop, upgrade levels, all read from data tables that match the spreadsheet | Phase 2 |
| 7 | **Story** | Rooms, logbooks, Dispatch captions, lift-gate puzzle | Phase 3 |
| 8 | **UI** | Cash, tank gauge, shop, map, pause, settings | Phase 2–4 |
| 9 | **Save** | Remembers upgrades and which cubes are broken | Phase 2 |
| 10 | **Audio** | Sound effects, ambience, music mix | Phase 4 |
| 11 | **Services** | Ads, purchases, analytics, crash reports, hidden behind simple wrappers so a company can be swapped | Phase 4 |

**Key decisions:**

- **The world is a block of about 100 × 50 × 100 cubes (50 × 25 × 50 m).** That is about 500,000 cubes and half a megabyte of data, which is tiny. It is cut into small 16-cube chunks so a punch only redraws a few chunks.
- **Cubes inside the rock are never drawn.** Only the faces you can see.
- **Bouncing rubble is capped and reused.** All rubble shares one shape and one material, so the phone draws them in a single batch. Extra rubble is swept up into a small cash bonus.
- **Numbers live in data files, not in code.** Changing a cost or a cube value never needs a rebuild.
- **Saving is small.** Save the seed-free mine as a list of broken cubes per chunk, plus upgrades and story progress.

## A6. Go / no-go gates

At each gate you stop and ask: is this good enough to keep building on?

| Gate | Question | If it fails, cut this first |
| --- | --- | --- |
| 1 | Punching a wall into cubes holds 30 fps with 100 rubble cubes on the low phone | Bigger cubes, lower cap, simpler shadows |
| 2 | Vacuuming feels good with a thumb | Add suction assist, simplify the pull |
| 3 | The 10-minute loop makes 5 strangers say "one more upgrade" | Re-tune the economy, shorten bands |
| 4 | The whole mine runs about 70 minutes with no dead ends | Trim a band, adjust costs |
| 5 | Release candidate: no crashes in a 20-minute heat test on all three phones | Lower quality tiers, fix bugs |

## A7. Testing plan

- Connect your phone to the Unity Profiler and watch frame time while digging.
- 20-minute heat test on each phone.
- Interruptions: phone call, switch app and back, low battery, rotate, lock screen.
- Odd screens: notches, tall and short phones, so buttons are never hidden.
- Use Google Play's pre-launch report after uploading a test build.
- Keep a "known problems" list and clear it before each gate.

# Part B – Touch screens (wireframes)

Visual wireframes of all five screens are delivered separately. The rules they follow:

| Screen | Purpose | Key elements | Thumb rule |
| --- | --- | --- | --- |
| Play | Digging | Joystick, look area, PUNCH and VACUUM buttons, hotbar, cash, tank gauge, map button | Nothing important under the thumbs; centre stays clear |
| Upgrade shop (Company Store) | Spend cash | Seven upgrade cards, price, level, "Buy", ad-boost button | Big tap targets, scroll with one thumb |
| Map | Where am I | Hand-drawn map, rooms found, question marks | One tap to close |
| Pause | Stop and settings | Resume, Settings, Quit | Large buttons |
| Ad popup | Offer | What the bonus is, "Watch" and "No thanks" with equal weight | Never hide "No thanks" |

General rules: tap targets at least 48 dp (about 9 mm), a safe margin around notches, and left-handed mode that mirrors everything.

# Part C – Production plan

## C1. Assumptions

About 10–15 hours per week, one person, first game. All times are rough guesses; the prototype will tell us the truth. A phase is finished only when its "done when" list is true.

## C2. The phases

| Phase | Weeks | Goal | Done when |
| --- | --- | --- | --- |
| 0 – Learn and set up | 1–6 | Learn Unity basics, install tools | You finished a small practice game; Git and backups work; test phone connects |
| 1 – Prototype | 7–14 | Punch, rubble, vacuum, crusher on a real phone | Gates 1 and 2 pass |
| 2 – Core loop and economy | 15–22 | Cash, shop, seven upgrades, HUD, save | Gate 3 passes |
| 3 – Content | 23–34 | Six bands, the mine map, rooms, logbooks, equipment, lift escape | You can play start to finish once (Gate 4) |
| 4 – Polish and services | 35–42 | Audio, effects, tutorial, ads, purchase, settings, quality tiers | Tutorial works with a stranger; ads and purchase work in test mode |
| 5 – Test and release | 43–50 | Closed test, fixes, store page, launch | Gate 5 passes; Google approves; staged rollout |

About 50 weeks, roughly 11–12 months, plus a buffer. If life gets busy, phases stretch but the order stays the same.

## C3. A weekly rhythm that works for a solo developer

- **Start of week:** pick the one thing this week must achieve (written in one sentence).
- **Work sessions:** 3–4 sessions of 2–3 hours.
- **Rule:** the game must always be playable. Never leave it broken overnight (Git makes this easy).
- **End of week:** build to your phone, play for 15 minutes, write three lines: what works, what does not, what is next.

## C4. Learning plan (Phase 0)

1. Unity Learn (learn.unity.com): the beginner pathways, such as *Unity Essentials* and then *Junior Programmer*. Pathway names change, so check the current titles.
2. Make a tiny toy: a first-person view where you click a block and it breaks into pieces. This is the seed of the real game.
3. Learn Git basics: save, undo, and put your project on GitHub.
4. Read Unity's own mobile performance tips once, so the budget in Part A makes sense.

## C5. Accounts and setup checklist

- Google account for the game (separate from your personal one is tidier).
- **Google Play Console** developer account: one-time registration fee (about $25, check current amount), plus identity verification. New personal accounts have had to run a **closed test with a minimum number of testers for about two weeks** before production. Check the current numbers in Play Console now and plan for the delay.
- AdMob account for the rewarded ads (linked to the Play app later).
- A privacy policy web page (free: GitHub Pages or Google Sites). Google requires a link.
- Unity account and the Personal plan (free; check its revenue limit).
- GitHub account with a private repository.
- Optional: Firebase for crash reports and basic analytics.

## C6. Store listing checklist

- App name and a short and a full description.
- App icon, a feature graphic, and at least a few phone screenshots (landscape is fine). Check the current size rules.
- Content rating questionnaire (the game should rate as mild cartoon violence at most).
- Data safety form (what ads and analytics collect) and a target audience that is not primarily children.
- Declare that the app contains ads.
- Privacy policy link, contact email.

## C7. Assets and licences

- Sources: Unity Asset Store, Kenney.nl (free, permissive packs), OpenGameArt, Freesound (check each file's licence), and your own creations.
- **Licence log:** a simple spreadsheet listing every asset: name, where from, licence, date, and a link.
- Never copy art, sounds, text or UI from the reference game or any other game.

## C8. Rough cost

| Item | Estimate |
| --- | --- |
| Play Console registration | about $25 |
| Unity Personal | $0 (if you qualify) |
| Cheap used test phone | $0–150 |
| Art and model packs | $0–300 |
| Music and sound | $0–100 |
| **Likely total** | **about $25–600** |

## C9. The cut list (if you fall behind)

Cut in this order, starting at the top:

1. Rock Drill tool
2. Conveyor
3. Extra treasure types
4. Dynamite
5. Third crew logbook (keep a single clue)

**Never cut:** punch, vacuum, crusher, the seven upgrades, the lift escape.

## C10. Main risks

| Risk | What we do |
| --- | --- |
| Too many cubes for phones | Rubble cap, quality tiers, tested at Gate 1 |
| First Unity project is large | Phase 0 learning, small steps, always-playable rule |
| Touch aiming feels poor | Aim assist, preview, early playtests |
| Burnout | Small weekly goals; stop on a working build |
| Play Console delays | Register early, run the closed test as soon as a stable build exists |
| Looking like another game | Original setting, names, art, text; check at each gate |

## C11. How I can help in each phase

Phase 0: explain every term and check your practice project. Phase 1 onward: write and explain code in small steps once you say go, review what you built, find bugs from your descriptions, tune the Economy spreadsheet with your playtest numbers, and draft the store text.

## C12. Your first seven days

1. Day 1: install Unity Hub and Unity 6.3 LTS with Android Build Support.
2. Day 2: create the GitHub account and an empty private repo.
3. Day 3–5: work through the first Unity Essentials lessons.
4. Day 6: enable Developer Options and USB debugging on your phone and run a test build.
5. Day 7: open the Economy spreadsheet, change a few numbers, and see what happens to the pacing.
