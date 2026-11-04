# Break to Escape — Unity Setup (owner steps)

Written for someone new to Unity and working alone. Follow the steps in order. Each step says what success looks like. **Nothing here has been run in Unity yet**: the code has only been type-checked against Unity API stand-ins and its pure logic tested outside Unity, so expect a few compile errors on the first import. The steps tell you exactly what to copy back when that happens.

## The first 10 minutes (checklist)

- [ ] Unity Hub and **Unity 6.3 LTS** installed with **Android Build Support** (including OpenJDK and Android SDK & NDK).
- [ ] New project from the **Universal 3D** template.
- [ ] Active Input Handling set to **Both**, editor restarted.
- [ ] TextMeshPro essentials imported.
- [ ] `UnityProject/Assets` copied into the project, in the three groups below.
- [ ] Console shows **0 errors** (red).
- [ ] **BreakToEscape > Build Complete Playable Scene** run; `Assets/Scenes/Main.unity` exists.
- [ ] Pressed **Play**: you stand in a lit chamber, Crusher 07 is on the floor in front of you, a wall is beyond it.

## 1. Install Unity

1. Install **Unity Hub** from unity.com.
2. Hub > Installs > Install Editor > choose the newest **Unity 6.3 LTS**. Tick **Android Build Support** and both sub-items (**OpenJDK**, **Android SDK & NDK Tools**).
3. Success: the version appears under Installs with an Android icon.

## 2. Create the project

1. Hub > Projects > New project > template **Universal 3D** (it may be called "Universal 3D Core" or "3D (URP)"). Name it `BreakToEscape`.
2. Success: the editor opens with a sample scene and no red errors in the Console (Window > General > Console).
3. Put the project folder under Git (GitHub Desktop is the easiest way). Commit once before adding anything.

## 3. Project settings

1. **Edit > Project Settings > Player > Other Settings > Active Input Handling → Both**. Unity asks to restart; click Yes.
2. **Window > Package Manager**: check that **Input System** is installed (Unity 6 templates include it). If not, install it from the Unity Registry.
3. **Window > TextMeshPro > Import TMP Essential Resources** → Import.
4. **File > Build Profiles** → select **Android** → **Switch Platform** (takes a few minutes the first time).
5. In the URP renderer asset (Project window: `Assets/Settings/`, the file ending in `_Renderer`), set **Rendering Path** to **Forward**.
6. Success: no red errors; the title bar shows `<Android>`.

## 4. Copy the game files, in three groups

Copy from this repository's `UnityProject/Assets/` into your project's `Assets/` folder (using your file manager, with Unity open). After each group, wait for Unity to finish compiling and look at the Console.

| Group | Copy these folders | Expect |
|---|---|---|
| 1 | `Scripts/Core`, `Resources`, `Tests` | 0 errors. Open **Window > General > Test Runner > EditMode > Run All**: about 75 tests should pass. |
| 2 | `Shaders`, then all other folders in `Scripts` **except** `Scripts/Editor` | 0 errors. Shader warnings are possible; note them. |
| 3 | `Scripts/Editor` | 0 errors and a **BreakToEscape** menu appears at the top. |

If the Console shows red errors after a group:

1. Click the **first** red line (later ones are often caused by the first).
2. Copy the whole message (it looks like `Assets/Scripts/World/VoxelChunk.cs(57,13): error CS1061: ...`).
3. Send back: the first error line, the file name and line number, and which group you were copying. Do not try to fix many errors at once.

Shaders: if `Assets/Shaders/VoxelLit.shader` shows an error in the Inspector, copy its first error line too. The builder falls back to a simple unlit shader so you can keep going.

## 5. Build the scene

1. **BreakToEscape > Build Complete Playable Scene**. If asked, save or discard the current scene.
2. It creates `Assets/Generated` (materials and prefabs), builds the whole scene, saves it as `Assets/Scenes/Main.unity` and adds it to the build list.
3. Success: the Console says "scene built and saved to Assets/Scenes/Main.unity"; the Hierarchy shows `VoxelWorld`, `Crusher07`, `Player`, `_Systems`, `UI`, `EventSystem`.
4. Layers: the builder names layer 6 **VoxelWall** and layer 7 **Rubble**. If it warns that those layers are already used, tell me which names they had.

## 6. Press Play (in the editor)

1. Press **Play**. Use the Game view's aspect menu to pick a landscape phone size (for example 2400 × 1080).
2. Editor controls: **WASD** walk, **hold the right mouse button and drag** to look, **Space** punch (hold to charge), **Left Shift** hold to vacuum, **F** suck/blow, **1–4** hotbar, **E** use, **M** map, **Esc** pause. On-screen buttons also work with the mouse.
3. What success looks like:
   - You stand on the floor of the Collapse Chamber facing north; Crusher 07 (a dark plate with two rollers) is in front of you; a caption slides in: *"Good morning, Worker 17..."* then *"Punch the wall. It has it coming."*
   - Walk to the wall, punch: a plus-shaped patch of 5 cubes breaks and rubble falls; a white wireframe shows the next patch.
   - Hold Shift near rubble: cubes fly into the vacuum, the TANK counter rises.
   - Stand a step away from the crusher, look down at it, press F (blow mode) and hold Shift: cubes shoot into it, "+$2" pops up and cash rises. (Press F again to go back to suction.)
   - Open the **STORE** (top right), buy Tank Capacity for $125.
4. Run the tests again (Test Runner) if you change anything in `Scripts/Core`.

## 7. Quality tiers (optional now, needed before release)

1. **Edit > Project Settings > Quality**: add levels named **Low**, **Mid**, **High** (click Add Quality Level, rename).
2. **BreakToEscape > Create URP Tier Assets**. Success: the Console says "3 of 3 quality levels assigned".
3. Details in `docs/ANDROID_BUILD_CHECKLIST.md` section 2.

## 8. Build to a phone

1. On the phone: enable Developer Options (tap Build Number 7 times) and USB debugging; connect by USB and accept the prompt.
2. Set the Player Settings in `docs/ANDROID_BUILD_CHECKLIST.md` section 1 (IL2CPP, ARM64, target API 36).
3. **File > Build Profiles > Android > Run Device** = your phone → **Build And Run**. Save the build as `Builds/BreakToEscape.apk` (turn off "Build App Bundle" for local testing).
4. Success: the game opens in landscape and shows the chamber.

## 9. First measurements to record (send these back)

On each test phone, open **Settings > Show Rate Overlay** and play for 5 minutes, then note:

- Phone model, RAM, Android version, and the TIER shown on the overlay.
- FPS while punching continuously with lots of rubble (and the RUBBLE count shown).
- PUNCH / VACUUM / CRUSH measured rates against the table rates shown next to them.
- Any hitch when breaking into a new area (a short freeze).
- How long until the first cash and first upgrade (the Console logs "Tutorial: first cash at … s, first upgrade at … s").

## What to send back after the first Play

1. The first red Console error (if any), exactly as shown, with file and line.
2. A screenshot of the Game view right after pressing Play.
3. Whether you spawned on the floor facing the crusher, or somewhere else.
4. Whether walls render in colour, magenta/pink (shader problem) or invisible.
5. Whether a punch breaks a plus-shaped patch of 5 cubes.
6. The last ten lines of the Console after one minute of play.

## Where things are

| Path | What |
|---|---|
| `Assets/Scripts/Core` | Game rules without Unity (economy, patterns, grid, save, story, tiers). Tested. |
| `Assets/Scripts/Bootstrap/GameBootstrap.cs` | Start-up order of the whole game. Start reading here. |
| `Assets/Resources/Data/economy.json` | All numbers. Generated from the spreadsheet: edit the spreadsheet or `tools/verify/economy_extras.json`, then run `python3 tools/verify/generate_economy_json.py`. |
| `Assets/Resources/Data/story.json` | All story text, captions and the gate code. Safe to edit directly. |
| `Assets/Resources/Data/audio.json` | Which sound file plays for which event. |
| `Assets/Scripts/Editor/SceneAutoBuilderEditor.cs` | The one-click scene builder. |
