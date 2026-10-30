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
