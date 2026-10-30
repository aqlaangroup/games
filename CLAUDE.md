# Working in this repo

This repo holds several mobile games and one shared .NET platform. Games are built in Unity (C#); the platform is ASP.NET Core + PostgreSQL. Read `README.md` for the layout.

## Before writing code

- Find the game's spec first. For Gahwit Sitti:
  - Start with `games/gahwit-sitti/BUILD_GUIDE.md`. Section 3 says which file covers which system.
  - Then read the specific section the task refers to.
- **Never invent game numbers.** Every number comes from the game's `design/balance.json`. If a number is missing, add it to `balance.json` and say so, rather than hard-coding it.
- If a `DEVLOG.md` exists in the game folder, read it for past decisions.

## Unity rules

- **Unity version:** Unity 6 LTS, URP. Code goes under `Assets/_Game/<System>/` with one assembly definition per system.
- **Keep game rules out of MonoBehaviours.** Rules live in plain C# classes (no `UnityEngine` dependency where possible) with EditMode tests. MonoBehaviours only display state and forward input.
- **Every new system ships with EditMode tests** for its rules.
- **Text:**
  - All player-facing text comes from Unity Localization string tables and renders with RTLTMPro. Never type Arabic into a scene or a string literal.
  - Dialogue lives in Ink files under `Content/Resources/Story/` (Resources, so the game can load them at runtime).
- **Money and time:**
  - Gems, purchases and anything bought with money are changed only from platform API replies. Never grant them on the client.
  - Energy and event timers use the time service: server time plus the phone's uptime counter, never the phone clock alone.
- **Save format:** saves follow `design/save_schema.json`. Any change bumps `schemaVersion` and adds a migration with a test.
- **Performance targets:** 60 fps on mid-range phones, 30+ fps on a 3 GB Android. Avoid per-frame allocations in Update. Pool spawned objects.
- **Accessibility:** touch targets of 44 pt or more, and nothing that relies on colour alone.
- **Shared package:** systems that other games could reuse (café builder, customers, merge board, story runner, platform client) belong in the shared Unity package under `unity-shared/`, not inside one game.

## Platform rules (.NET)

- Every table has a `GameId`. Every game route starts with `/games/{slug}`.
- Store receipts are verified on the server. A store transaction id can grant only once.
- No secrets in the repo. Store credentials come from configuration or a secret store.

## Writing style for docs and in-game text

- Plain, short sentences in English docs.
- **Dialogue dialects:** Amman dialect for Salma, Teta, Abu Fadi, Samira and Karim; Najdi for Lana; neutral Arabic for menus.
- Dialogue drafts are marked `[draft]` until a native writer approves them.

## Git

- Unity binary files go through Git LFS (see the `.gitattributes` in each Unity project).
- Commit after each working feature, with tests passing.
