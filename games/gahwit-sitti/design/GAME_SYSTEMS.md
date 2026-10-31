# Gahwit Sitti: game systems (season 1)

This is the full system behind the game, ready for a Unity team to build.

- **All numbers live in one file:** `balance.json`. Unity loads it at start, and a remote-config copy can override it without an app update.
- **The pacing is checked:** `sim/simulate.py` plays 35 days for four player types. The results are in `sim/results.md`.
- **Change a number, run the simulator again.** If a chapter drifts off target, the report says so.

---

## 1. The core loop

```
Tap a generator (1 energy) → item appears on the board
Merge 2 identical items → 1 item of the next level
Serve an order → coins + XP
Spend coins on café tasks → story scene + café changes
XP → player level → bigger orders, new features, energy reward
```

## 2. Board

| Rule | Value |
|---|---|
| Size | 7 × 9 = 63 squares |
| Merge | 2 identical items (same chain, same level) → 1 item one level higher |
| Moving | Drag or tap-tap to any empty square |
| Storage | 6 slots to park items off the board; more slots for gems (30, 60, 100, 150, 200, 300) |
| Selling | Any item sells for coins equal to its level |
| Free-space rule | After a chapter, or on login after 4+ hours, offer to sell or store low items until **25% of the board is free** |

## 3. Items and chains

Each chain has its own generator. The value of an item is measured in **units**: a level-L item takes 2^(L−1) level-1 items to make. That one rule drives every price in the game.

| Chain | Levels | Generator | Unlocks | Top item |
|---|---|---|---|---|
| Coffee (قهوة) | 10 | Teta's coffee tin | Chapter 1 | Full hospitality set |
| Sweets (حلو) | 10 | Semolina sack | Chapter 1 | Eid tray |
| Tea (شاي) | 8 | Tea box | Chapter 3 | Tea gathering |
| Saudi coffee (قهوة سعودية) | 8 | Lana's spice jar | Chapter 5 | Coffee majlis |
| Breakfast (فطور) | 8 | Milk fridge | Chapter 7 | Friday breakfast table |

Every item name, in Arabic and English, is in `balance.json → chains`.

## 4. Generators

- **Cost:** a tap costs 1 energy and puts one item on a free square.
- **Upgrades:** merging two generators of the same level makes a higher-level one. Copies come from chapter rewards, the starter pack and later events.
- **Higher levels** sometimes drop level-2 or level-3 items, so each tap is worth more:

| Generator level | Output | Units per tap |
|---|---|---|
| 1 | 100% level 1 | 1.00 |
| 2 | 90% L1, 10% L2 | 1.10 |
| 3 | 75% L1, 25% L2 | 1.25 |
| 4 | 60% L1, 40% L2 | 1.40 |
| 5 | 45% L1, 50% L2, 5% L3 | 1.65 |

## 5. Energy

| Rule | Value |
|---|---|
| Cap | 100 |
| Refill | 1 every 2 minutes (30 an hour; empty to full in 3h20) |
| Rewarded ad | +25, up to 5 a day |
| Buy with gems | +100 for 30 gems; that day's later buys cost 45, 60, 80, 100 |
| Level up | +50 |
| Chapter reward | +50 to +200 |

Energy can go over the cap from rewards. Only the time-based refill stops at 100.

## 6. Orders

- **Visible orders:** 3 at the start, 4 from level 4, 5 from level 12.
- **Size:** each order asks for items worth about **6 + 1.6 × player level** units. Example: about 14 units at level 5, about 38 at level 20.
- **Item level cap** grows with player level:

| Player level | 1 | 2 | 4 | 6 | 9 | 12 | 16 | 20 | 25 |
|---|---|---|---|---|---|---|---|---|---|
| Highest item level asked | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |

- **Two-item orders:** from level 5, 30% of orders ask for 2 items.
- **Characters:** 40% of orders come from named characters (Abu Fadi, Khalto Samira, Karim, Lana). They say a short line and grow closer to Salma.
- **Reward:** 1.6 coins and 1 XP per unit, rounded, with a minimum of 3 coins. A rewarded ad can double the coins of one order, 3 times a day.
- **Fairness:** at least one visible order must be finishable with chains the player already has.

## 7. Bubbles

When a merge makes an item of level 4 or higher, there's a 6% chance a **copy floats up in a bubble** for 60 seconds.

- **Buy it:** for 5 × 2^(level−4) gems.
- **Or watch an ad:** up to 2 a day.

This is the main small gem spend. It's never a random paid item, because the player sees the exact item before paying.

## 8. Player levels

- **XP to the next level:** 60 + 30L + 2L², where L is the current level. For example, 92 XP from level 1 to 2, 560 XP from level 10 to 11, and 1,460 XP from level 20 to 21.
- **Each level-up gives:** +50 energy and 5 + ⌊L/2⌋ gems.
- **Features unlock by level:** see `balance.json` `unlocks`, the one schedule (merged with COMPLETE_SPEC.md section 2 on 2026-10-31).

## 9. Café tasks and story

- **Structure:** season 1 has **10 chapters × 6 tasks**. Each task costs coins, gives 10 + 2 × chapter XP, changes the café picture and plays one story scene. That makes 60 scenes, each 3–6 lines.
- **Style choices:** some tasks let the player choose a style: Old Levantine, Modern Ammani or Gulf majlis. The player can change it later for free.
- **Chapter rewards:** gems, energy and a generator copy.

| Chapter | Title | Total coins | Unlocks | Target finish (typical player) |
|---|---|---|---|---|
| 1 | رجعة سلمى · Homecoming | 100 | Coffee, sweets | Day 1 |
| 2 | خطط خالتو · Khalto's plans | 500 | | Day 1 |
| 3 | لانا من الرياض · Lana from Riyadh | 800 | Tea | Day 2 |
| 4 | دلال ستّي · Teta's dallahs | 1,000 | | Day 3 |
| 5 | قهوتين · Two coffees | 1,500 | Saudi coffee | Day 5 |
| 6 | الافتتاح · Grand reopening | 1,800 | | Day 7 |
| 7 | فطور الجمعة · Friday breakfast | 2,300 | Breakfast | Day 10 |
| 8 | عرض فارس · Faris's offer | 3,200 | | Day 14 |
| 9 | الصفحة الناقصة · The missing page | 5,900 | | Day 21 |
| 10 | عرس بالحارة · A wedding in the neighborhood | 8,300 | Season 2 teaser | Day 30 |

**Story outline**

1. Salma reopens the café; Abu Fadi wants a proper coffee.
2. Khalto Samira brings a groom.
3. Cousin Lana arrives from Riyadh with half the inheritance.
4. Karim the carpenter remembers Teta.
5. The cousins serve both Ammani and Saudi coffee.
6. Grand reopening.
7. Friday breakfast brings the whole street.
8. Faris, who owns a coffee chain, offers to buy the building, and the cousins disagree.
9. Abu Fadi has the missing knafeh page.
10. They cater a neighborhood wedding, decide to stay partners and plan a café in Riyadh.

**Writing rules**
- Salma speaks urban Amman dialect and Lana speaks Najdi; both are written by native writers.
- Menus and system text are in neutral Arabic.
- Local words get tap-to-explain.
- Romance stays family-centred and marriage-minded, for a 12+ rating.

## 10. Simulated pacing

From `sim/results.md`. A typical player plays 4 sessions a day, watches 3 ads and turns 85% of energy into served orders.

| Day | Player level | Chapter | Orders served |
|---|---|---|---|
| 1 | 6 | 3 | 69 |
| 3 | 9 | 5 | 27 |
| 7 | 13 | 7 | 22 |
| 14 | 17 | 9 | 18 |
| 21 | 20 | 10 | 16 |
| 30 | 22 | 10 (finishing) | 11 |

How the four player types compare:
- **Typical player:** finishes chapter 10 on day 32. Every chapter is within 25% of its target.
- **Casual player** (3 sessions, no ads): is on chapter 9 at day 35. That's fine, because events keep them busy.
- **Engaged player:** finishes on day 24.
- **Payer** (buys 200 energy a day): finishes on day 20.

**Known tuning point:** generator output only reaches about 1.13 units a tap by day 30, because season 1 gives only 10 generator copies, spread over five chains. If day-30 retention is weak, add generator copies to events first.

## 11. Currencies

| Currency | Earned from | Spent on |
|---|---|---|
| Coins (soft) | Orders, selling, events | Café tasks, decor |
| Gems (hard) | Purchases, level-ups, chapters, daily tasks, login, hosting | Energy, bubbles, storage, premium decor |
| Event tokens | Orders during an event | That event's board only; never sold for money |

## 12. Shop

**Rules**
- No paid random items.
- Every bundle lists its exact contents.
- Prices use App Store and Google Play local price tiers (SAR, AED, EGP, JOD).

| Product | Price (USD tier) | Contents |
|---|---|---|
| Starter pack (once, from level 3) | 0.99 | 80 gems, 200 energy, coffee generator copy |
| Gems | 1.99 / 4.99 / 9.99 / 19.99 / 49.99 / 99.99 | 100 / 275 / 600 / 1,300 / 3,500 / 7,500 |
| Energy pack | 2.99 | 500 energy + 20 gems |
| Eid decor bundle | 4.99 | Lantern set, Salma's Eid outfit, 60 gems |
| Season pass | 7.99 per 28 days | Premium track |

**Rewarded ads** (no interstitials, because they hurt retention with this audience):
- +25 energy, 5 times a day.
- Double an order's coins, 3 times a day.
- Pop a bubble, 2 times a day.

**Decided 2026-10-31 (missing/17):**
- **Gems** are the only thing the platform's wallet holds. Every other thing a product gives (energy, a generator copy, a decor set, an outfit, the pass) comes back in the platform's reply as items, and the game applies them to its save once per purchase id (`purchases.applied` in the save).
- **The season pass** is a one-off purchase for each 28-day season (a non-renewing subscription on Apple, a one-time product on Google), because Apple has no 28-day renewal period.
- **Refunds** take back the purchase's gems, never below a zero balance; the ledger records any shortfall. Items already used (energy) stay; owned items stop counting once the purchase is marked refunded.
- **Restore purchases** (Settings and the shop) asks the store to re-send what the store account owns and fills in items from the platform's purchase list. A non-consumable restored to a new account brings its items, not its gems.
- **The starter pack** is hidden once bought; the stores can't block a second purchase, so the platform grants it if one happens.
- **Ads:** Unity LevelPlay, once the publisher account exists. Ads give their rewards in the game (they aren't bought with money); the caps count per local day in the save. Personalised ads are off for players under 13 or whose age is unknown.

## 13. Live events

Five templates, reused with new art and story each time:

| Template | How it works | Numbers |
|---|---|---|
| Hosting (عزومة) | Friends fill one big order of 30 items together (SYSTEMS_B.md section 7; it replaced the coffee race) | 48 hours, twice a week |
| Memory album (ألبوم الذكريات) | Collect cards of Teta's old photos | 40 days, 9 sets × 9 cards, a pack every 8 orders, **packs never sold**; duplicates become stars for a chest with listed contents |
| Season pass (تذكرة الموسم) | Points from energy spent | 28 days, 30 tiers × 400 points; free and premium tracks |
| Mini board (سوق الجمعة) | A small 5 × 6 board with its own 8-level chain | 4 days; 2 tokens per order; 10 milestone rewards |
| Double weekend (ويكند الضعف) | Order coins × 2 | 48 hours, Friday and Saturday |

**Weekly calendar**
- **Always on:** pass, album, daily tasks, daily login.
- **Monday–Wednesday:** hosting.
- **Thursday–Sunday:** mini board and hosting.
- **Friday–Saturday:** double weekend.

That's about 20 events a month, which matches Gossip Harbor's early calendar.

**Seasonal reskins**

| Event | Based on | Length | Special feature |
|---|---|---|---|
| Ramadan Nights | Mini board | 30 days | A story scene unlocks each night after iftar |
| Eid Ma'amoul | Hosting | 5 days | Gifting bundles |
| Wedding season | Mini board | 14 days | |
| Winter sahlab | Mini board | 10 days | |

The simulator checks the pass: a typical player reaches tier 30 in 28 days, and a casual player reaches tier 24.

## 14. Daily systems

- **Login, days 1–7:** 25 energy, 50 coins, 5 gems, 50 energy, 100 coins, 10 gems, then 20 gems plus a cushion decor.
- **Daily tasks:** 3 a day, such as "serve 10 orders". Each gives 2 gems and 50 pass points.
- **Gifting** (from level 10): send 5 energy gifts a day and visit friends' cafés. Players add friends by code, and there is no open chat.

## 15. Analytics: events Unity must log

These feed the soft-launch gates in `PLAN.md`:
- day 1, day 7 and day 30 retention
- payer conversion
- ARPDAU
- ad payback

| Event | Fields |
|---|---|
| `session_start` / `session_end` | length, energy at start |
| `tutorial_step` | step id |
| `generator_tap` | generator, level, energy left |
| `merge` | chain, new level, bubble shown |
| `order_served` | chain, item level, coins, character |
| `task_complete` | chapter, task, style chosen |
| `scene_viewed` / `scene_skipped` | scene id, seconds |
| `level_up` | new level |
| `energy_empty` | time since session start |
| `ad_offer` / `ad_watched` | placement |
| `store_view` / `purchase` | product, price tier, country |
| `event_join` / `event_progress` | event id, milestone |
| `board_full` | free squares |
| `perf` | average fps, share of frames under the weakest phone's target, worst frame, phone memory, device, graphics level (once a session, after `ui.graphics.perfSampleSeconds`) |

## 16. How Unity is organised

| System | Job | Notes |
|---|---|---|
| ConfigService | Loads `balance.json`, applies remote overrides | Unity Remote Config or Balancy |
| BoardSystem | Grid, drag and tap, merge, storage, selling, free-space rule | Board state saved as a list of {chain, level} |
| GeneratorSystem | Taps, output odds, generator upgrades | Odds from config |
| EnergySystem | Cap, refill by server time, ads, gem buys | Use server time to stop clock cheating |
| OrderSystem | Generates orders by size and level cap; fairness check | Seeded random for reproducible bugs |
| ProgressionSystem | XP, levels, unlocks | Formula from config |
| TaskSystem + CafeView | Tasks, style choices, café art states | Each café object has art per state and style |
| StorySystem | Scenes, choices, tap-to-explain words | Ink or Yarn Spinner; **RTLTMPro** for Arabic text |
| EventSystem | The 5 templates, calendar, rewards | Events defined fully in remote config |
| StoreService | Unity IAP, local price tiers, receipt check | Fixed-content products only |
| AdsService | Rewarded ads only, daily caps | LevelPlay or AppLovin MAX |
| SaveService | Local save plus cloud save | Unity Cloud Save or PlayFab |
| Analytics | Section 15 events | Firebase or GameAnalytics |
| Content | Art downloaded on demand | Addressables, so new events don't need app updates |

**Build order for the vertical slice**
1. Board, merge, generators and energy.
2. Orders and progression.
3. Tasks, café view and story for chapters 1–2.
4. Save and analytics.
5. Store and rewarded ads.
6. One hosting and one mini-board event.

## 17. What to tune after the soft launch

- **If day 1 is low:** check the tutorial drop-off. Make chapter 1 cheaper or let the first orders give more coins.
- **If day 7 is low:** check `energy_empty` timing and board-full rates. Increase generator copies or the free-space help.
- **If payer conversion is low:** check when the starter pack appears and whether bubble prices are reachable.
- **If chapters feel slow:** lower the late chapter costs in `balance.json`, then run `sim/simulate.py` again.
