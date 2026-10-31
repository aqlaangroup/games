# Shared Unity package: `com.aqlaan.cozykit`

Systems that every cozy game in this repo can reuse. Each game adds this folder as a local package in its `Packages/manifest.json`:

```json
"com.aqlaan.cozykit": "file:../../../../unity-shared/com.aqlaan.cozykit"
```

The path is relative to each game's `unity/Packages` folder.

## What goes here

| Module | Contains |
|---|---|
| Core | Config loading, save and migrations, time service, service locator, events bus |
| Merge | Board, chains, generators, energy, orders, bubbles, storage (rules + view) |
| Builder | 3D room, decor catalog, grid placement, styles, cosiness |
| Customers | Walk-ins, regulars, friendship, schedules, order bubbles |
| Story | Ink runner, dialogue UI, choices, portraits, glossary |
| Memories | Memory framework and the 8 memory game types |
| Wardrobe | Outfit slots, occasions, style score |
| Live ops | Event templates: pass, album, mini board, hosting, frenzy |
| Platform client | Sign-in, cloud save, receipt verification, remote config, analytics forwarding |
| UI kit | Frosted panels, buttons, transitions, unlock cards, RTL helpers |

## What stays in each game

Art, story, item chains, recipes, characters, balance numbers, scenes and the game's own unique features.

## Rule

A system moves here only once it works in one game. Don't build it here first and guess.

## What has moved so far

- **Core:** the time service, device clock, save slots and the analytics interface (from Gahwit Sitti, 2026-10-31).
- **Platform client:** the platform client, cloud sync, device secrets and Sign in with Apple (from Gahwit Sitti, 2026-10-31).

The package's own `README.md` lists the files. The other modules in the table above still live in Gahwit Sitti; `games/gahwit-sitti/missing/done/24.md` says when each one is ready to move.

## Tests

The package's EditMode tests are in `Tests/Editor`. A game that lists the package under `testables` in its manifest runs them with its own tests. To run them on their own, use an empty Unity project whose manifest has only this package and `com.unity.test-framework`.
