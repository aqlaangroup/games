# Gahwit Sitti: dev log

Decisions and known gaps. Newest first. Read this at the start of each session (`/CLAUDE.md`).

## 2026-10-31: Boot and Café scenes, package cleanup, audio drafts (missing/01)

- **Two scenes** replace `SampleScene` (`unity/README.md`):
  - `Boot` (`_Game/Boot/BootScene.cs`) loads config, content, the save, the text tables and the time service. It starts signing in, then opens `Cafe`. Signing in carries on in the background, so an offline start is just as fast.
  - `Cafe` (`CafeScene.cs`, which was `GameBootstrap.cs`) builds the café and the interface.
- **One composition root:** `Services.cs` holds what the scenes share.
  - A new language, a cloud save or a deleted account restarts from `Boot` with everything loaded again (`BootScene.Restart`).
  - Playing `Cafe` straight from the Editor still works: it makes the services itself.
- **Removed:**
  - Unused packages: Sentis (`com.unity.ai.inference`, which also pulled in App UI), Visual Scripting, Collab proxy and Multiplayer Center. Their scripting defines went with them.
  - The URP template's `TutorialInfo` and `Readme.asset`.
  - Kept: the AI Assistant (an Editor tool that ships nothing in the build), and Navigation for walking customers.
- **`.claude/`:** `worktrees/` stays git-ignored, as does any `settings.local.json`. Shared Claude Code settings may be committed.
- **Platform:** player accounts, cloud save, support and staff roles are documented in `platform/README.md` (commits `a4e9ac9`, `e487de7`, merged in `89ccf91`).
- **Draft music and effects** made with ElevenLabs: 5 music tracks and 20 effects in `Assets/Audio/{Music,Sfx}/Drafts`. Remake them with `tools/audio_drafts.py`, listed in `tools/audio_drafts.json`. They're placeholders until the audio freelancers deliver (COMPLETE_SPEC.md section 7). Nothing plays them yet (missing/14).
- `missing/` is the 2026-10-31 review of what the game still needs. Finished items are struck there and logged here.

## 2026-10-31: iPhone build, player accounts, settings, draft voice

**iPhone build**
- **Gahwit Sitti > Build iOS (Xcode project)** writes `unity/Builds/iOS` (`BuildTools.cs`; also runs with `-executeMethod`).
- Signing uses team `QJ9MPD82U5` with automatic signing.
- `IosPostBuild.cs` links AuthenticationServices and adds the Sign in with Apple capability.
- Addressables (the text tables) now build with the player.
- Two problems found on a real iPhone 14 Pro:
  - **Text tables:** the blocking wait for them (`WaitForCompletion`) never finished on the phone. They now load in a coroutine before anything else is built (`Loc.Load`, `Services.Start`).
  - **Café materials:** they were copied from the render pipeline's default material, which exists only in the Editor. They now come from `Resources/Materials/CafeLit.mat`; `CafeLitEmissive.mat` keeps the glow variant in builds.

**Accounts (COMPLETE_SPEC.md sections 4, 9, 14, 16)**
- **Platform client** (`_Game/Platform`, plain C#, 15 tests):
  - Guest account on first launch, kept by a sign-in token in the iPhone Keychain.
  - Access tokens renew themselves.
  - Everything returns "offline" instead of failing.
  - Lives in the game for now; it moves to `unity-shared` once it has worked here (`unity-shared/README.md`).
- **Cloud save** (`CloudSync.cs`):
  - Uploads every 60 seconds and when the app goes to the background.
  - At launch it compares with the platform. When both phones changed, the player chooses "this phone or the other one", with level and chapter shown for each.
  - A new install takes the cloud save without asking.
  - What the phone last agreed with the platform is kept in `gahwit-sitti-sync.json`, beside the save.
- **Sign in with Apple:** `Plugins/iOS/GahwitAppleSignIn.mm` asks for no name and no email. If the Apple id already has a café, the player picks which café to keep. Google sign-in on Android isn't built.
- **Age gate** (`screens/AgeGate`):
  - Language and birth year, with no year picked in advance.
  - The age band counts the younger possible age (`AgeRules.cs`, the same rule as the platform).
  - Under-13s and unknown ages need a parent for ads and social features. Neither feature exists yet.
  - The parent-consent flow itself isn't designed.
- **Settings** (`screens/Settings`; the gear at the top left of the café):
  - Switches for sound, music, notifications, explained words and full voice. Only the explained-words switch has an effect today; the rest are saved for when audio and notifications land.
  - Language, link account, help and contact (FAQ plus a form; the platform adds player id, version and phone), privacy and terms, and delete account.
  - Restore purchases waits for in-app purchases.
- **Save schema v2:** `account` (birth year, parent consent), plus `settings.notifications` and `settings.voice`. A v1 save migrates and shows the age gate once (tested).
- **Placeholders:** `Resources/Config/platform.json` holds the platform address and the legal page addresses, all still in [brackets]. Until they're set, the phone plays offline as a guest. The Editor uses `http://localhost:5080`.
- **Text:** new menu text is neutral Arabic, as CLAUDE.md asks, so the dialect lines in the AgeGate and Settings designs were rewritten. The English needs a review.
- **Touch targets:** new buttons and rows are at least 120 canvas px, about 44 pt on an iPhone 14 Pro. The existing tab bar buttons are 96 px (about 35 pt) and should grow.

**Draft voice**
- Teta's voice note now plays a recording: `Resources/Voice/Drafts/voice_1_teta.mp3`, 10.6 s, made with ElevenLabs. The subtitles follow the clip's length.
- It is a **draft**: PLAN.md keeps main voices for native actors. A native listener should check the Ammani pronunciation.
- `tools/voice_drafts.py` remakes draft lines from `tools/voice_drafts.json`. The API key comes from the git-ignored `/.env`.

## 2026-10-30: task style choice (backlog 16)

- The sign and chairs tasks offer three styles: Old Levantine, Modern Ammani and Gulf majlis (`chapters.json` `styles`, colours from the prototype and `cafe-3d`).
- Each chip on the task card is painted like the result. The chosen one has a thick outline and bold text, so the choice doesn't rely on colour alone.
- The choice is saved in `cafe.taskStyles`. It recolours the sign's board and lettering and the chairs' seats and backs. The 1974 memory always shows Teta's Old Levantine look.
- Changing a style later for free (GAME_SYSTEMS.md section 9) needs the decor screen.

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
