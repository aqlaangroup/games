# Gahwit Sitti: dev log

Decisions and known gaps. Newest first. Read this at the start of each session (`/CLAUDE.md`).

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
- Text uses a JSON table (`Resources/Loc/ar.json`), not Unity Localization string tables (CLAUDE.md). Move it in backlog item 5.
- The save shape is not `save_schema.json` yet, and has no `schemaVersion` or migrations (backlog 3).
- Energy uses the phone clock. It needs the time service, server time plus uptime (backlog 4).
- There is one scene. Boot and Café scenes and a service locator are backlog 1.
- The camera is a small orbit script, not Cinemachine (backlog 11).
- Not built yet: decor placement and cosiness, walking customers and Abu Fadi as a regular, the pour memory, the letter, wardrobe, the 11-step tutorial, storage, bubbles, audio, analytics.
- Dialogue other than the prototype lines is `[draft]` and needs the native writers.
