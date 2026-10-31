# Gahwit Sitti: level B systems

These systems sit on top of the merge economy in `GAME_SYSTEMS.md`:
- **Unchanged:** the board, chains, generators, energy, orders and levels.
- **Replaced:** races become **hosting** (section 7), and Ramadan is only an optional theme.

Their numbers live in `balance.json` under `levelB`. They are starting values to tune in testing.

## 1. A day in the game

| Session (2–10 minutes) | What the player does |
|---|---|
| Open the game | The café shows the real time of day. Regulars who are "in" are sitting at their tables. |
| Kitchen | Merge for 2–5 minutes, serve orders, earn coins |
| Café | Spend coins on a task or a decor piece, place it, watch customers react |
| Story | A task completes → a story scene; the last task of a chapter → a playable memory |
| Neighbours | Serve a regular their favourite → friendship rises → their own story moves forward |
| Self-expression | Dress Salma for the next chapter's occasion; share a postcard |

## 2. The 3D café

| Rule | Value |
|---|---|
| Spaces in season 1 | Café floor, kitchen corner, rooftop (from chapter 6), Teta's back room (memories only) |
| Camera | Orbit about 120° around the room, pinch to zoom, double-tap a table to focus |
| Placement | Snap to a grid of 0.25 m cells, rotate in 90° steps, swap a piece by tapping it |
| Decor in season 1 | 60 pieces: 20 earned from tasks, 25 bought with coins, 10 from events, 5 premium sets |
| Styles | Old Levantine, Modern Ammani, Gulf majlis. Matching 3+ pieces of a style gives a cosy bonus. |
| Cosiness score | Sum of placed pieces (1–5 each) plus style bonuses. Higher scores bring more walk-in customers (+1 table, up to 6) and more tips. |
| Time of day | Morning, afternoon, evening and night follow the phone's clock (start hours in `balance.json` `levelB.cafe.timeOfDayStartHours`). Each has its own light, music and regulars. |
| Weather | Sunny most days; rain and fog in winter, from the date. Purely for mood. |

## 3. Customers

**Walk-ins**
- 2–6 at a time, depending on the cosiness score.
- They sit, show what they'd like over their head, and leave a tip of 1–5 coins when served from the board (`balance.json` `levelB.walkIns.tipCoins`; 2–10 ran the season too fast, DEVLOG 2026-10-31).
- They leave after 10 minutes of real time if no one serves them, with no penalty.

**Regulars**
- 12 in season 1, each visiting at set times of day (see `balance.json`).
- **Friendship** goes from level 1 to 10. Serving a regular their favourite item gives +3 points, and any other item gives +1. Each level needs 10 more points than the last.
- **At levels 3, 6 and 10**, the regular's own short story scene plays: 3 small scenes each, 36 in total.
- **At level 10**, they give a unique decor piece.

**Season 1 regulars**

| Regular | Their story |
|---|---|
| Abu Fadi | His lost love |
| Tala | A school student facing her final exams (tawjihi) |
| Um Rami | A young widow starting a catering business |
| Karim | The carpenter |
| Khalto Samira | Her own marriage, which is not what she pretends |
| Dr. Hiba | A night-shift doctor |
| Yazan | A busker with an oud |
| Sara and Noor | Newlyweds |
| The postman | Mail he never delivered |
| Mrs. Odeh | A retired teacher who taught Salma's mother |
| Faris | He comes in disguised as a normal customer |
| A tourist from Jeddah | Changes every season |

## 4. Recipes

**How to cook a recipe**
- Teta's recipe book has 20 blank pages in season 1.
- **The pan:** the kitchen has a cooking pan where you place two items from different chains to try a recipe.
- **Discovering:** a correct pair of items fills a page, plays a short sound and animation, and sometimes reveals a story clue.
- **Wrong pairs** return both items, so nothing is lost. After 3 wrong tries in a row, the book shows a hint.

**What a discovered recipe gives**
- **Signature dishes:** customers ask for it by name, paying 1.5× the coins of a normal order of the same size.
- **Story clues:** 5 recipes carry clues about the family secret.
- **Example:** Saudi coffee level 4 + coffee level 5 = "قهوة الثنتين", the two-city coffee from chapter 5.

## 5. Playable memories

One memory per chapter, so 10 in season 1. Each lasts 3–5 minutes and uses one of 8 reusable game types. Chapter 5 reuses Find in a new 1974 room, and chapter 10 closes with Pour: Salma makes the two cups Teta's letter asked for (`balance.json` `levelB.memories.byChapter`).

| Type | How you play | First used |
|---|---|---|
| Pour | Hold to pour, release at the line | Chapter 1 |
| Rhythm | Shape ma'amoul in time with a song | Chapter 2 |
| Find | Find objects in a 1974 room | Chapter 3 |
| Restore | Rebuild a torn photo from its pieces | Chapter 4 |
| Trace | Sew a wedding dress by following a line | Chapter 6 |
| Tune | Turn a radio dial to find a song or a voice | Chapter 7 |
| Choose | A dialogue with timed choices, such as haggling at the souk | Chapter 8 |
| Reassemble | Put a torn letter back in order to read it | Chapter 9 |

- **Scoring:** each memory gives 1–3 stars. Stars never block the story. 3 stars unlock a memory decor piece, such as "Teta's 1974 radio".
- **Cutscenes:** each memory ends with a short pre-made video (Veo) or a 3D scene shot in the game itself.

## 6. Salma's wardrobe

- **Season 1:** 40 pieces in 6 slots: top, bottom or dress, outer layer, shoes, accessories, and an optional hijab.
- **Occasions:** each chapter has one, for example "a family lunch" or "the grand reopening". Dressing for it earns a style score of 1–3 hearts, worth bonus coins (100 / 200 / 300 × chapter). It never blocks the story.
- **How an outfit scores** (decided 2026-10-31): each piece has style tags, and each occasion lists the tags it likes. Worn pieces with a liked tag count: 1 heart always, 2 hearts for 2, 3 hearts for 4 (`balance.json` `levelB.wardrobe.heartsAtMatches`).
- **How players get pieces:** 15 from tasks and memories, 15 bought with coins, 6 from events and the pass, 4 premium sets.
- **Every piece is modest by default,** with the hijab as an option, never a requirement.

## 7. Hosting friends (replaces races)

- **Big orders:** a big order, such as wedding catering or a funeral gathering (azza), appears twice a week and lasts 48 hours. It needs 30 items.
- **Who can help:** you invite up to 3 friends, by friend code or from the friends list. They help from their own boards, and every item they give counts.
- **Rewards:**
  - The host gets gems and a decor piece.
  - Each helper gets gems and +10 friendship with the host.
  - If the order is finished, everyone gets a shared postcard.
- **Playing alone works too:** neighbours (bots) help, though the rewards are smaller.
- **Visiting:** you can walk around a friend's café in 3D and leave one gift a day.
- **No open chat.** Players send one of 12 preset messages, such as «قهوتك بتجنّن» ("your café is amazing").

## 8. Postcards and the Sitti wall

- **Postcards:** one tap renders a still of your café with Salma in her outfit, with a soft frame and the game's name. Share it to Snapchat, Instagram or WhatsApp with a download link.
- **The Sitti wall** (from level 10): a wall in the café where you pin your own grandmother's saying or recipe, up to 140 characters.
  - Every post is moderated, first automatically and then by a person, before anyone else sees it.
  - Posts are shown without names. Players can like posts but can't reply.

## 9. Changes to the economy (`GAME_SYSTEMS.md`)

- **New coin sinks:** decor pieces (50–2,000 coins) and wardrobe pieces (100–1,500 coins). They help with the late-season coin surplus the simulator showed.
- **New gem sinks:** premium decor sets and outfits at fixed prices, plus skipping a memory retry.
  - **Memory retry (decided 2026-10-31, missing/12):** right after a memory under 3 stars, another try costs `levelB.memories.skipRetryGems`; the best stars count. Carrying on is always free.
- **New shop items:**
  - Premium decor sets: 4 per season, about $4.99 each.
  - Outfit bundles: 4 per season, about $2.99–$4.99.
- **Events:**
  - The race template becomes hosting.
  - The album now collects Teta's 1974 photos.
  - Seasonal themes are optional and reuse the same 5 templates.
