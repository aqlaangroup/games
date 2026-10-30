# Aqlaan Games

One repo for all our games and the one shared platform behind them.

```
games/                     one folder per game
  gahwit-sitti/            قهوة ستّي (Gahwit Sitti)
    game.json              how the platform knows this game: ids, store products
    CONCEPT.md, PLAN.md    what the game is and how we test it
    design/                game systems, balance.json, pacing simulator
    art/                   prompt pack and generated art
    prototype/             web prototype of the core loop
    feel-demo/             3-minute feeling demo
    screens/               screen designs
    research/              market research notes and report
    BUILD_GUIDE.md         how to build it with Claude Code and Unity, with the task backlog
    DEVLOG.md              decisions and known gaps, newest first
    unity/                 the Unity project (Unity 6.3 LTS, URP); chapter 1 is playable
  wingwright/              plane upgrade game with real engineering (working title, mobile, free to play)
    CONCEPT.md
unity-shared/              shared Unity package (com.aqlaan.cozykit) for systems every game reuses
platform/                  ONE .NET application for every game
  src/Platform.Domain      shared data model
  src/Platform.Api         player accounts and sign-in, cloud saves, support, purchases, config, events, staff admin API
  tests/Platform.Tests     API tests on a throwaway PostgreSQL database
  (admin web app to come)  one admin panel for all games
```

## Rules

- **One platform for every game.** Payments, players, game settings, events and the admin panel live in `platform/`. Games never talk to payment providers on their own.
- **Every record belongs to a game.** Each table has a `GameId`, and each API route starts with `/games/{gameSlug}`.
- **Each game is self-contained.** Its own folder and its own Unity project. Shared code goes in `platform/` (server) or a shared Unity package later (client).
- **Each game registers itself** through its `game.json`, so adding a new game needs no platform code change.

Claude Code reads `CLAUDE.md` for the rules every session follows.
