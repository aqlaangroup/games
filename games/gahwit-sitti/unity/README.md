# Gahwit Sitti: Unity project

This folder is the Unity project, made with **Unity 6.3 LTS (6000.3.27f1)** from the **Universal 3D (URP)** template. Follow this structure.

## Getting started

- Install the Editor with phone build support: `unity install 6000.3.27f1 -m android -m ios`.
- Install **git-lfs** before you clone. Art, audio, video and fonts in this folder are stored in Git LFS (see `.gitattributes`).
- The project includes `com.unity.pipeline`, so the Unity CLI and Unity's Claude Code plugin can drive an open Editor (`unity status`).

## Packages

Also add the shared package from `/unity-shared/` (see its README). Systems that other games could reuse move there once they work here.


| Need | Package |
|---|---|
| Rendering | Universal Render Pipeline (mobile renderer settings) |
| Arabic text | TextMeshPro + **RTLTMPro** (joins Arabic letters and lays text right to left) |
| Story and dialogue | **Ink** (inkle's Unity integration) |
| Input | Input System (touch: tap, drag, pinch, orbit) |
| Camera | A small orbit, zoom and focus script (`_Game/Cafe/CafeCamera.cs`); add Cinemachine only if cutscenes need it |
| Downloadable content | Addressables (chapters, events and decor downloaded later) |
| Purchases | Unity IAP; receipts verified by the platform API |
| Ads | LevelPlay or AppLovin MAX (rewarded ads only) |
| Analytics | Firebase or GameAnalytics |
| Video | Unity Video Player (Veo cutscenes) |
| Animation | Animator with a shared humanoid set for café characters |

## Folders

```
Assets/
  _Game/
    Boot/             first scene: load config, sign in, download content
    Core/             services: Config, Save, Time, Platform API, Analytics, Audio
    Merge/            board, items, generators, energy, orders, bubbles, storage
    Cafe/             3D café, decor placement, cosiness, time of day, weather
    Customers/        walk-ins, regulars, friendship, schedules
    Recipes/          cooking pan, recipe book
    Story/            Ink runner, dialogue UI, choices, tap-to-explain words
    Memories/         the 8 memory game types and the scene loader
    Wardrobe/         outfits, occasions, style score
    Social/           friends, hosting, visits, postcards, Sitti wall
    Events/           pass, album, mini board, hosting, frenzy, calendar
    Shop/             products, IAP flow, rewarded ads
    UI/               screens, frosted panels, buttons, transitions
  Art/                models, textures, portraits, icons (by area)
  Audio/              music, ambience, sound effects, voice
  Content/            Ink stories, config copies, localisation tables
  Scenes/             Boot, Cafe, Visit (memories play over the café: no scene of their own)
```

## Code rules

- **Data comes from config.** Every number comes from `design/balance.json` (remote copy from the platform). No magic numbers in code.
- **The server owns money.** Gems and purchases are only changed by the platform's replies. The game shows them but never decides them.
- **Use server time** for energy and events.
- **Arabic first.** Every text field uses RTLTMPro and comes from the localisation table, never typed into a scene.
- **One assembly per folder** under `_Game/`, so systems stay separate and compile fast.

## Vertical slice checklist (stage 1)

1. Boot scene: config, guest sign-in, cloud save.
2. Café floor in 3D with orbit and zoom; 15 decor pieces with grid placement.
3. Merge kitchen: coffee and sweets chains, generators, energy, orders.
4. Abu Fadi as a walking, sitting, sipping regular with friendship levels 1–3.
5. Chapter 1 in Ink with tap-to-explain words.
6. Memory type "Pour" and the 1974 scene.
7. The letter and Teta's voice note.
8. Wardrobe: 5 pieces, one occasion.
9. Analytics events from `design/GAME_SYSTEMS.md` section 15.
