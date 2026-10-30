# Gahwit Sitti: dev log

Decisions and known gaps. Newest first. Read this at the start of each session (`/CLAUDE.md`).

## 2026-10-30: the pour memory and Teta's letter (backlog 17, 18)

- **Memory framework.**
  - A task can name a memory (`chapters.json`, `"memory": "m1_pour"` on the sign task, matching COMPLETE_SPEC.md section 1, step 9).
  - A memory plays in order:
    1. A lead-in scene today.
    2. A fade to 1974: the café as Teta ran it, in faded film colours with grain (`CafeRoom.SetPast`).
    3. An intro scene.
    4. The memory game.
    5. A scene chosen by the result.
    6. An outro.
    7. A scene back in today.
  - Best stars are saved under `memories`. A memory cut short by closing the app plays again on the next launch.
- **Pour game** (`_Game/Memories/PourGame.cs`, 5 tests): hold to fill the cup, then release inside the line for 3 stars, above it for 2, or after a spill for 1. Releasing too early asks for "a bit more"; it never fails. The numbers come from the feeling demo and live in `balance.json` `levelB.memories.pour`.
- **The easy assist** that widens the line is built and tested but turned off. It needs a settings field (COMPLETE_SPEC.md section 5) and a settings screen.
- **The letter:**
  - After the memory, the counter's middle drawer glows and an "open the drawer" button floats over it.
  - The letter fades in line by line, then Teta's voice note plays with word-by-word subtitles (there's no recording yet), then Lana's message arrives as a phone notification. The letter is saved under `story.lettersFound`.
- **Text:** all memory and letter lines are in `chapter1.ink`, taken from `feel-demo/index.html`. New characters: `teta_young` (Um Khalil) and `fadi_young`.

## 2026-10-30: Unity Localization (backlog 5)

- Player-facing text comes from the Unity Localization **"UI" string table** (`Assets/Content/Localization`), with Arabic and English locales. `Loc.T(key)` reads it; the old `Resources/Loc/ar.json` is gone.
- The table preloads, and `Loc.Load` waits for it once at startup, so lookups are instant.
- **Gahwit Sitti > Set up localization** recreates the settings, locales and table if they are missing.
- **Tests check** that every key has Arabic and English text, that every key written in code exists, and that every character, walk-in and task has its text.
- English UI text was written by Claude Code and needs a review. The language setting (`settings.language`) is saved, but there's no settings screen yet.
- Dialogue stays in Ink (CLAUDE.md). A glossary table and English dialogue come with the story localization work.

## 2026-10-30: time service, save schema, generators on the board (backlog 3, 4, 6)

- **Time service** (`Core/TimeService.cs`): trusted time is server time (when the platform gives it) moved forward by the phone's uptime counter, which keeps counting while the phone sleeps. On Android that counter is `elapsedRealtime`, on iOS `CLOCK_MONOTONIC` (`Plugins/iOS/GahwitUptime.mm`). Changing the phone clock adds no energy (tested). After a restart the uptime counter starts again, so time falls back to the phone clock but never goes backwards. The platform's sanity check covers the rest (COMPLETE_SPEC.md section 11).
- **Saves follow `design/save_schema.json`** (`_Game/Save`). Sections the game doesn't use yet are kept as they are. Migration step 0 → 1 upgrades the first playable's save, and loading this Editor's real save worked.
- **Schema v1 changed before release.** No real saves existed yet, so the version stays at 1:
  - `player.energyStampUptimeMs` became `energyStampMs` (trusted time), because uptime restarts with the phone.
  - New fields: `clock` (the time service's anchor), `player.ordersServed` and `rng` (the seeded random state, for replaying bugs).
- **Generators are pieces on the board** (GAME_SYSTEMS.md section 4, save schema). Tap one for an item; merge two of the same level to upgrade it; they can't be sold. A chapter's generator copy now lands on the board to merge. Pieces that don't fit wait in `board.storage` and come back when there's room. The storage screen itself isn't built.
- **Ids follow the schema:** `abu_fadi`. Walk-in orders save no name; the shown name comes from the order id.
- **Story choices** are saved by the choice's first Ink tag (`* [text] #ask`), under `story.choices[knot]`. Scenes seen are saved too.
- **Tests:** 22 EditMode tests (Core 5, Merge 13, Save 4).

## 2026-10-30: Unity project and first playable

**Set up**
- Unity 6.3 LTS (6000.3.27f1), Universal 3D (URP) template, iOS as the active platform, Android and iOS build support installed.
- Packages: Pipeline (lets the Unity CLI and Claude Code drive the open Editor), Cinemachine, Addressables, Ink 2.0, RTLTMPro 4.0 (`hk1ll3r/RTLTMPro?path=/UPMPackage`, the packaged copy the RTLTMPro README points to).
- Arabic font: Noto Naskh Arabic (OFL) in `Assets/Content/Resources/Fonts`, built into a dynamic TextMeshPro font at runtime. It covers Arabic presentation forms, which RTLTMPro needs.
- `unity projects create` from the Unity CLI (1.0.0-beta.13 and beta.15) hangs forever on this Mac, so the project was made with the Editor's `-createProject` from the bundled URP template.

**What runs (`Assets/Scenes/SampleScene`, press Play)**
- The 3D café opens first and shows chapter 1's current task. Drag to orbit, pinch or scroll to zoom. Light follows the phone's time of day.
- The intro scene plays in Ink with tap-to-explain words.
- Kitchen: 7×9 board, coffee and sweets generators, tap or drag to merge, sell, orders from `balance.json` rules, XP and levels.
- Chapter 1: six café tasks. Each one changes the room and plays its Ink scene: sign, floor, chairs, dallahs, lights, counter.
- Local save every 2 seconds and when the app is paused. Use **Gahwit Sitti > Reset save** to start again.
- 11 EditMode tests for the rules (`Assets/_Game/Merge/Tests`).

**Decisions**
- Added a `formulas` block to `design/balance.json`, with machine-readable copies of the prose formulas (order size, XP, task XP, units, sell price, minimum coins). It also holds two new numbers the docs didn't give: `orderMaxItemsPerLine` (3) and `orderLevelPick` (half the order lines ask for the level cap). Keep `sim/simulate.py` in step if they change.
- Orders reach their unit size with up to 3 copies of one item, because early level caps (level 2) can't reach 7.6 units with a single item.
- A chapter's generator copy is merged straight into the matching generator. There are no generator copies on the board yet.
- Gems are never changed on the client. `Wallet` only shows what the platform reports, so it stays at 0 until the platform client exists. Level-up and chapter gem rewards will come from the platform.
- Tap-to-explain markup is `[[word:meaning]]`, because Ink reserves `|`.
- Ink 2.0 compiles each `.ink` into an `InkFile` asset (`storyJson`); the game loads it from `Resources`.
- Items, icons and the café are code-made shapes and colours until art arrives.

**Known gaps against the spec (next sessions, BUILD_GUIDE ring 1 order)**
- ~~Text uses a JSON table, not Unity Localization (backlog 5).~~ Done, see above.
- ~~The save shape is not `save_schema.json` yet (backlog 3).~~ Done, see the entry above.
- ~~Energy uses the phone clock (backlog 4).~~ Done, see the entry above.
- There is one scene. Boot and Café scenes and a service locator are backlog 1.
- The camera is a small orbit script, not Cinemachine (backlog 11).
- Not built yet: decor placement and cosiness, walking customers and Abu Fadi as a regular, wardrobe, the 11-step tutorial with its hand pointer, the storage screen, bubbles, audio, analytics. ~~Pour memory and letter~~ are done (see above).
- Dialogue other than the prototype lines is `[draft]` and needs the native writers.
