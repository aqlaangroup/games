# Gahwit Sitti: build guide (you + Claude Code + Unity)

This is the working guide for building the game yourself with Claude Code. Read it together with these files:

| File | What it covers |
|---|---|
| `CONCEPT.md` | What the game is |
| `design/GAME_SYSTEMS.md` | Merge economy |
| `design/SYSTEMS_B.md` | Level B systems |
| `design/COMPLETE_SPEC.md` | Everything else |
| `design/balance.json` | Every number |
| `design/save_schema.json` | Save shape |
| `unity/README.md` | Project structure |
| `/CLAUDE.md` | Rules every Claude Code session follows |

## 1. What changes when it's you and Claude Code

| Work | Who |
|---|---|
| C# gameplay code, UI code, tools, tests | Claude Code writes it; you review, run in Unity and play |
| .NET platform (purchases, saves, events, admin) | Claude Code writes it; you run and deploy it |
| Story and dialogue drafts | Claude Code drafts; **native writers polish** (Amman and Najdi) |
| 2D art (portraits, icons, key art) | You, with Flow and Nano Banana, from `art/FLOW_PROMPTS.md` |
| 3D models | Asset store packs as a base + AI 3D tools for drafts + **a freelance 3D artist** for the heroes and key decor |
| Character animation | Mixamo base animations, retargeted, plus a **freelance animator** for key moments |
| Cutscenes | You, with Veo in Flow |
| Music, sound, voice | Freelancers; Claude Code can't make final audio |
| Playing on phones, store accounts, legal | You |

**The honest bottleneck is art, not code.** Budget for freelancers on the parts players look at most: Salma, Lana, Teta, Abu Fadi, the café and 20 hero decor pieces.

### Rough solo budget (estimate)

| Item | Estimate |
|---|---|
| 3D freelancer (characters, café, hero decor) | $15–40K |
| Animator (key animations) | $5–12K |
| Audio, music, voice actors | $5–12K |
| Native writers (polish) | $3–6K |
| Tools: Unity, Flow, assets, servers | $3–6K a year |
| Store accounts, legal | $2–5K |
| Soft-launch ads | $10–30K |
| **Total** | **about $45–110K** plus your time |

### Rough solo timeline (estimate)

| Milestone | Month |
|---|---|
| Vertical slice | 3–4 |
| Soft-launch MVP | 9–11 |
| Full season 1 | 13–15 |

## 2. Scope: build in three rings

Don't build everything at once. Each ring is playable and testable on its own.

| Ring | Contains | Purpose |
|---|---|---|
| **1. Vertical slice** | Café floor (15 decor), merge kitchen (coffee + sweets), Abu Fadi as a regular, chapter 1, pour memory, the letter, 5 wardrobe pieces, tutorial, local save | Test with 30–50 women. Gate: 70%+ want to keep playing |
| **2. Soft-launch MVP** | Chapters 1–5, tea and Saudi coffee chains, 6 regulars, 10 recipes, 5 memories (pour, rhythm, find, restore, and find again in chapter 5), 30 decor, 20 wardrobe pieces, pass, album, mini board, shop, rewarded ads, cloud save, platform purchases, analytics, notifications, settings, postcards | Soft launch in Jordan, Egypt, Saudi Arabia |
| **3. Full season 1** | Chapters 6–10, rooftop, breakfast chain, 12 regulars, 20 recipes, 10 memories, 60 decor, 40 wardrobe pieces, friends, hosting, visits, Sitti wall | Launch |

**Moved out of the MVP on purpose**
- Hosting, friends and the Sitti wall need social servers and moderation.
- They come in ring 3, after the core is proven.

## 3. Coverage check

Everything a complete game needs, and where it's specified.

| Area | Spec |
|---|---|
| Merge board, chains, generators, energy, orders, bubbles, storage | `GAME_SYSTEMS.md` 2–7 |
| Levels and XP | `GAME_SYSTEMS.md` 8 |
| Unlock schedule | `COMPLETE_SPEC.md` 2 |
| Café tasks and chapter structure | `GAME_SYSTEMS.md` 9 |
| 3D café, decor, cosiness, time of day, weather | `SYSTEMS_B.md` 2 |
| Walk-ins and regulars | `SYSTEMS_B.md` 3 |
| Recipes | `SYSTEMS_B.md` 4 |
| Memories (8 types) | `SYSTEMS_B.md` 5 |
| Wardrobe | `SYSTEMS_B.md` 6 |
| Hosting, visits, postcards, Sitti wall | `SYSTEMS_B.md` 7–8 |
| Currencies, shop, rewarded ads | `GAME_SYSTEMS.md` 11–12 |
| Events and calendar | `GAME_SYSTEMS.md` 13, `SYSTEMS_B.md` 9, `balance.json` |
| Daily gift and daily tasks | `GAME_SYSTEMS.md` 14 |
| Tutorial | `COMPLETE_SPEC.md` 1 |
| Notifications | `COMPLETE_SPEC.md` 3 |
| Settings | `COMPLETE_SPEC.md` 4 |
| Accessibility | `COMPLETE_SPEC.md` 5 |
| Languages, Arabic text, fonts | `COMPLETE_SPEC.md` 6 |
| Audio list | `COMPLETE_SPEC.md` 7 |
| Motion and effects | `COMPLETE_SPEC.md` 8 |
| Save data, cloud sync, conflicts | `COMPLETE_SPEC.md` 9, `save_schema.json` |
| Offline and errors | `COMPLETE_SPEC.md` 10 |
| Anti-cheat | `COMPLETE_SPEC.md` 11 |
| Screen list | `COMPLETE_SPEC.md` 12 |
| Art asset list | `COMPLETE_SPEC.md` 13 |
| Legal, privacy, age rules | `COMPLETE_SPEC.md` 14 |
| Store listing | `COMPLETE_SPEC.md` 15 |
| Player support | `COMPLETE_SPEC.md` 16 |
| Analytics and metrics | `GAME_SYSTEMS.md` 15, `COMPLETE_SPEC.md` 17 |
| Purchases, saves, events on the server | `/platform/README.md` |
| Story outline | `GAME_SYSTEMS.md` 9, `CONCEPT.md` |

**Still to write before ring 1 ends:**
- **Story bible:** the full season 1 outline, the 2006 secret, all 10 memories and the 36 regular scenes.
- **Art bible:** the 3D style test result.
- **Final story text for chapter 1**, in Ink.

## 4. Set up once

1. Install Unity Hub and **Unity 6 LTS** with the iOS and Android modules. You need a Mac with Xcode to build for iPhone.
2. In `games/gahwit-sitti/unity/`, create a project from the **Universal 3D** template.
3. Install **Git LFS**. The `.gitattributes` and `.gitignore` in that folder are already set up for Unity.
4. Add the packages listed in `unity/README.md`.
5. Make a free **Mixamo** account (character animations) and set up Flow for 2D art.
6. Open Claude Code in the repo root. It reads `/CLAUDE.md` automatically.
7. For the platform, install the .NET 10 SDK and PostgreSQL, then run `dotnet build platform/Platform.slnx`.

## 5. How to work with Claude Code

- **One feature per session.** For example: "Implement the merge board from GAME_SYSTEMS.md section 2, with EditMode tests."
- **Always point to the spec section and to `balance.json`.** Claude Code should never invent numbers.
- **Ask for tests with every system.** EditMode tests for logic; play-test the feel yourself.
- **Logic and visuals stay separate.** Pure C# classes hold the rules and Unity components only display them. That way Claude Code can test the logic without the editor.
- **After each session:** open Unity, press Play, try it on a phone weekly, then commit.
- **Keep a `DEVLOG.md`** of decisions. Tell Claude Code to read it at the start of each session.
- **Reading files:** Claude Code can't see the Unity editor, so describe what you see or paste the console errors. Screenshots help too.

## 6. Backlog: ring 1 (vertical slice)

Each line is roughly one Claude Code session (S) or a few sessions (M). Do them in order.

| # | Task | Size | Done when |
|---|---|---|---|
| 1 | Project skeleton: folders, assembly definitions, Boot scene, service locator | S | Boot scene loads the Café scene |
| 2 | Config loader: read `balance.json` into typed C# classes | S | EditMode tests read every value |
| 3 | Save system: local save and load matching `save_schema.json`, with migrations | M | Tests: save, load, migrate v1 |
| 4 | Time service: server time plus the phone's uptime counter | S | Tests: changing the phone clock doesn't add energy |
| 5 | Arabic text: RTLTMPro, localisation tables, fonts, tap-to-explain words | M | Arabic dialogue shows correctly with glossary taps |
| 6 | Merge logic (pure C#): board, chains, merge, sell, storage | M | Tests cover every rule in `GAME_SYSTEMS.md` 2–3 |
| 7 | Generators and energy logic | S | Tests for output odds, regen, cap |
| 8 | Orders logic: size formula, level cap, fairness rule | M | Tests; matches the simulator's pacing |
| 9 | Kitchen view: grid, drag and drop, merge animation, generator taps | M | Playable on a phone |
| 10 | Progression: XP, levels, unlock cards | S | Level-ups fire the right unlocks |
| 11 | Café scene: modular room, orbit and pinch camera (a small script; Cinemachine wasn't needed), time of day | M | Runs at 30+ fps on a 3 GB Android |
| 12 | Decor: shop, placement grid, rotate, styles, cosiness | M | 15 pieces placeable; cosiness updates |
| 13 | Customers: walk-in path, sit, order bubble, serve from the kitchen, sip, tip | M | Abu Fadi visits, orders and reacts |
| 14 | Regular friendship (Abu Fadi, levels 1–3) | S | Level 3 plays his first story scene |
| 15 | Story runner: Ink integration, dialogue UI, choices, portraits | M | Chapter 1 plays end to end |
| 16 | Café tasks: chapter 1 tasks, style choice, story triggers | S | Each task changes the café and plays its scene |
| 17 | Memory framework + "pour" memory, sepia transition | M | The 1974 memory plays from the right task |
| 18 | The letter and voice note scene | S | Matches the feeling demo |
| 19 | Wardrobe: 5 pieces, one occasion, style hearts | S | Salma changes outfit in the café |
| 20 | Tutorial: the 11 steps from `COMPLETE_SPEC.md` 1 | M | A new player reaches the letter without help |
| 21 | Audio: mixer, music by time of day, kitchen and café sounds, haptics | S | Settings switches work |
| 22 | Analytics: events from `GAME_SYSTEMS.md` 15 | S | Events visible in the dashboard |
| 23 | Quality settings and performance pass | S | 60 fps mid-range, 30+ fps on the weakest |
| 24 | Playtest build: TestFlight and Play internal testing | S | 30–50 testers can install it |

## 7. Backlog: ring 2 (soft-launch MVP)

- **Content:** chapters 2–5 (Ink plus tasks), tea and Saudi coffee chains, 5 more regulars, the recipe pan and book (10 recipes), the rhythm, find, restore and choose memories, 15 more decor, 15 more wardrobe pieces.
- **Events:**
  - Event framework driven by remote config.
  - Season pass, album (no paid packs) and mini board.
  - Daily gift and daily tasks.
- **Money:**
  - Unity IAP with the products from `game.json`.
  - Platform receipt verification for Apple and Google.
  - Starter pack, bubbles and rewarded ads.
- **Services:**
  - Guest and Apple/Google sign-in.
  - Cloud save with conflict handling.
  - Remote config from the platform.
  - Notifications, settings, account deletion, restore purchases.
- **Store and launch:** postcards, store listing assets, privacy forms, rating questionnaires, crash reporting.

## 8. Backlog: ring 3 (full season)

- **Content:** chapters 6–10, the rooftop, the breakfast chain, 6 more regulars, 10 more recipes, the trace, tune and reassemble memories, and the rest of the decor and wardrobe.
- **Social and live ops:**
  - Friends, visits and gifts.
  - Hosting with bots for solo players.
  - Sitti wall with moderation.
  - Admin panel pages for events and support.

## 9. Testing

- **Logic:** EditMode tests for every rule. Port `design/sim/simulate.py` to a C# test that checks chapter pacing stays on target.
- **Play:** a weekly build on two real phones, one cheap Android (3 GB) and one iPhone.
- **People:** the ring 1 gate test with 30–50 women in Amman and Riyadh, plus a short survey: would you keep playing, which moment you liked most, which word you didn't understand.
- **Before every store build:**
  - no crashes in a 30-minute play
  - purchases tested in sandbox
  - offline mode works
  - Arabic text checked on every new screen

## 10. Release checklist

- [ ] App Store Connect and Google Play Console apps created with ids from `game.json`
- [ ] In-app products created in both stores with the same SKUs
- [ ] Platform deployed; receipt checks pass with sandbox purchases
- [ ] Privacy policy page live; Apple privacy label and Google Data safety filled in to match the SDKs
- [ ] Age rating done (12+); age gate and under-13 rules working
- [ ] Account deletion and restore purchases working
- [ ] Store listing in Arabic and English; screenshots; preview video
- [ ] Analytics and crash reporting on; soft-launch dashboard ready
- [ ] Soft launch to Jordan and Egypt first; check the gates in `PLAN.md` before Saudi Arabia
