# Gahwit Sitti (قهوة ستّي): production plan, level B

- **What the game is:** `CONCEPT.md`.
- **Systems:** `design/GAME_SYSTEMS.md` and `design/SYSTEMS_B.md`.
- **Unity setup:** `unity/README.md`.
- **Research behind it:** `research/reports/Qahwet Sitti concept deep research.md`.
- **Earlier prototypes:** `prototype/index.html` (core loop) and `feel-demo/index.html` (feeling demo).

**Budget figures are estimates.** They assume a team based mainly in Jordan, with art, audio and voice partly outsourced. Check them against real quotes before committing.

## 1. Targets

| | Target |
|---|---|
| Platforms | iOS 15+ and Android 9+, portrait |
| Weakest phone we support | Android with 3 GB RAM (a Galaxy A14-class phone), iPhone XR |
| Frame rate | 60 fps on mid-range phones, never below 30 fps on the weakest |
| First download | Under 150 MB; later chapters and events download in the background |
| Launch markets | Soft launch in Jordan and Egypt, then Saudi Arabia and the UAE, then the rest of the Arab region |
| Languages | Arabic first (Amman and Najdi dialogue, neutral Arabic menus), English second |
| Rating | 12+ |

## 2. Stages and stop-or-go gates

| Stage | Months | What is built | Gate to continue |
|---|---|---|---|
| **0. Pre-production** | 1–2 | Art bible and 3D style test (one character, one table, one lamp in-engine). Tech test: 3D café plus merge board running on the weakest phone. Story bible for season 1. | Style approved; the café runs at 30+ fps on a 3 GB Android |
| **1. Vertical slice** | 3–5 | One café room you can decorate (15 pieces). Merge kitchen. Abu Fadi as a regular. Chapter 1 with story scenes. One playable memory (pour). The letter. 5 wardrobe pieces. Cloud save. | 30–50 women play it in Amman and Riyadh: 70%+ want to keep playing; the memory and letter moments are named as the best part |
| **2. Alpha** | 6–9 | Chapters 1–5, 6 regulars, 10 recipes, 5 memories, 25 decor and 20 wardrobe pieces. Platform: sign-in, purchases, config, events. Shop, rewarded ads, analytics. | Feature-complete for chapters 1–5; crashes under 1% |
| **3. Beta** | 10–11 | Chapters 6–10, 12 regulars, the remaining memories, hosting, postcards, Sitti wall, 2 event templates. Arabic voice for key moments. | All of season 1 playable; internal testers finish the season |
| **4. Soft launch** | 12–14 | Android in Jordan and Egypt, then iOS and Android in Saudi Arabia | Day 1 return 40%+, day 7 15%+, day 30 7%+; 3%+ of players pay in Saudi Arabia; ad spend pays back within 12 months |
| **5. Launch** | 15–16 | Saudi Arabia, UAE and the rest of the region; season 2 in production | Spending more on ads keeps install costs from rising more than 50% |

**Stop rules:**
- **Vertical slice:** if fewer than half the testers want to keep playing, rework the slice before spending more.
- **Soft launch:** if fewer than 25% of players return on day 1, fix it before scaling.

## 3. Team

Up to about 14 people at the peak, from stage 2.

| Role | People | From stage |
|---|---|---|
| Game director / producer | 1 | 0 |
| Game and economy designer | 1 | 0 |
| Narrative lead | 1 | 0 |
| Dialect writers (Amman, Najdi) | 2, part-time | 1 |
| Unity developers (gameplay, UI, tools) | 3 | 0 (1), then 1 (3) |
| .NET backend developer (shared platform) | 1 | 1 |
| Technical artist (shaders, performance, pipeline) | 1 | 0 |
| 3D artists (environment and props; characters) | 2 | 0 |
| 2D / UI artist (portraits, icons, interface) | 1 | 1 |
| Animator (3D characters) | 1 | 1 |
| QA tester | 1 | 2 |
| Community and user acquisition | 1 | 3 |
| Audio, music, voice actors | Outsourced | 1 |

## 4. Budget (estimate)

| Item | Estimate |
|---|---|
| Team salaries, about 16 months, average about $2.5–3K per person per month | $520–680K |
| Outsourced art: extra decor, outfits, Veo cutscene clean-up | $80–150K |
| Audio, music (an original Maqam Hijaz theme), voice actors | $30–60K |
| Tools and licences: Unity, Spine or animation tools, Flow and AI tools, servers | $25–40K |
| Soft-launch ads | $30–60K |
| Legal: company, rating, privacy, store accounts | $15–25K |
| Contingency (15%) | $105–150K |
| **Total** | **about $0.8–1.2M** |

**How to fund it, in order:**
1. **Pre-production and slice:** own money plus Jordan's KAFD grant (JD 5–10K).
2. **After the vertical slice:** a publisher such as Tamatem (Amman), or angel investors.
3. **After soft-launch numbers:** a seed round from Merak or Impact46 ($0.5–1.5M rounds), or Saudi programmes, which often prefer a Saudi presence.

## 5. What makes it premium on ordinary phones

- **Graphics:** stylized 3D with Unity's mobile rendering pipeline (URP) and baked lighting. Soft shading painted into textures instead of heavy real-time effects.
- **The café:** about 150K triangles on screen, under 150 draw calls, and textures no larger than 2K, shared across props.
- **Characters:**
  - **In the café:** small 3D figures with a shared animation set: walk, sit, sip, chat, react.
  - **In dialogue:** close-ups use high-quality 2D portraits made from the Flow art.
  - **Big story moments:** Veo cutscenes.
- **Weaker phones:** a quality setting lowers shadows and effects automatically.

## 6. Risks

| Risk | What we do |
|---|---|
| 3D slows down cheap phones | Test on a 3 GB Android from month 1; automatic quality levels |
| Content costs too much to keep up | Reusable templates for memories and events; AI for decor variations; 2D portraits instead of 3D close-ups |
| Art style is inconsistent | One art bible, the tech artist as owner of the style, AI used only on top of approved references |
| Dialect sounds wrong | Native writers and voice actors; Saudi readers check comprehension in stage 1 |
| Scope keeps growing | Each stage has a fixed feature list; new ideas go to season 2 |
| Player backlash against AI | Characters, key art and main voices are made by people; AI only for variations and drafts |
| Big studios copy the idea | Speed, authentic writing and the community are the moat |

## 7. Platform

Payments, players, settings, events and the admin panel come from the shared .NET platform in `platform/`. This game registers through its `game.json`.
