# Gahwit Sitti: dev log

Decisions and known gaps. Newest first. Read this at the start of each session (`/CLAUDE.md`).

## 2026-10-31: purchases, shop, restore, refunds and rewarded ads (missing/17)

- **Platform** (`/platform`, 8 new tests, 56 in all):
  - **Apple:** `AppleStoreVerifier` asks the App Store Server API for the transaction id the game sends, production first and then the sandbox, with a short ES256 token from the in-app purchase key. It accepts only the right bundle id and product, and nothing Apple has revoked.
  - **Google:** `GooglePlayVerifier` asks the Play Developer API for the purchase token with a service-account token, accepts only completed purchases, and acknowledges them.
  - The keys come from configuration (`Stores:Apple`, `Stores:Google`), never the repo. Without them a store refuses every receipt.
  - **Refunds:** `/games/{slug}/stores/apple/notifications` asks Apple about the transaction named, and `/games/{slug}/stores/google/notifications` (with its secret token) takes voided purchases. A refund marks the purchase and takes its gems back, never below zero; the ledger notes any shortfall.
  - **Only gems go in the wallet.** Before this, a product's `energy` would have become a wallet currency. Now the reply returns everything else as items with a purchase id, and `GET /players/me/purchases` lists them.
  - **Restore:** a non-consumable sent again from another account brings its items, not its gems.
  - **On a desk,** `Stores:AllowDevReceipts` (Development only) lets the Editor's test store buy with `dev:` receipts.
- **Game:**
  - `PurchaseService` runs the flow: the store says paid, the platform checks, the game applies items once per purchase id and shows the new gems, and only then is the store transaction finished. An unconfirmed purchase is sent again on the next launch.
  - **Unity IAP couldn't be installed:** Unity's package server asks for a Unity Hub sign-in. `UnityIapStore` is written for Unity IAP 5 behind `GAHWIT_IAP`, which the Boot assembly defines once the package is present. Until then the Editor uses `EditorTestStore` and phones `NoStore`.
  - **The shop** opens from the HUD's gems from level 3. It lists every product's exact contents from `game.json` (now copied into the game by `BalanceSync`) with the store's own price, the starter pack first until bought, and Restore purchases. Restore is in Settings too.
  - **Rewarded ads** (`Game.Ads.cs`, tested): +25 energy from the out-of-energy card, a bubble for an ad, and "double +N with an ad" for 6 s after serving (`ui.adOfferSeconds`, proposed). Each placement counts its own daily cap (5, 3, 2) per local day, none before level 3. Personalised ads are off for under-13s and unknown ages. The ad network is LevelPlay once its account exists; the Editor plays a test ad.
  - **Save schema v10:** `purchases.applied`, `purchases.entitlements` and `daily.adsDate`, with a migration test.
- **Checked in the Editor:** buying the starter pack against the local platform gave 80 gems from the platform, and the game added +200 energy and a coffee-tin copy once.
- **Decisions** are in GAME_SYSTEMS.md section 12: the pass is a one-off purchase per 28-day season; refunds; restore; the starter pack; ads.
- **Still missing:** see "Still open" in missing/17. That covers the Hub sign-in for Unity IAP, the store keys and products, sandbox tests, the LevelPlay account, and the Eid set's content.

## 2026-10-31: recipes (missing/10)

- **Content:** 20 recipes in `Content/Resources/Content/recipes.json`, each two items from different chains, in any order. They run from coffee + sweets in chapter 1 to dairy in chapter 7; `two_cities` is the spec's example (Saudi cup + coffee with cardamom). 5 are marked as story-clue recipes. The pairs and names are proposed; the clue texts wait for the story bible (11).
- **`RecipeBook`** (`_Game/Recipes`, plain C#, 5 tests):
  - Two items from different chains go in the pan; no generators.
  - A right pair fills its page and is used up.
  - A wrong pair, or a recipe already found, goes back to the board (or storage when the board is full), so nothing is lost.
  - After 3 wrong pairs in a row, the book hints one item of a recipe whose chains are open.
- **Signature orders:** once a recipe is found, 20% of new orders (`levelB.recipes.signatureOrderChance`, proposed) ask for it by name: its two items, paying 1.5× a normal order of that size. The card shows a star, the dish's name and a gold edge. Only recipes whose chains the player has generators for are asked for.
- **Screens:** the pan button sits over the board's top corner from level 5. With an item picked, it goes in the pan; with nothing picked, the pan opens: two slots, Cook, the hint and the book. A discovery shows a card with petals. The book lists all 20 pages, with found ones named.
- **Save schema v9:** `recipes.found`, `recipes.wrongStreak` and `recipes.pan` are used, and `orders[].recipe` names a signature dish (migration tested).
- **Unlock card** for recipes at level 5.
- The touch-target play test now ignores errors other Editor packages log (the AI Assistant's network errors failed it once).
- **Still missing:** the clue texts and showing them in the journal (11), and recipe art (13).

## 2026-10-31: every memory game type (missing/12)

- **Rules** (`_Game/Memories`, plain C#, 12 new tests), numbers in `balance.json` `levelB.memories.<type>`:
  - **Rhythm:** tap as a ring closes on the dough; perfect counts 1, good half; stars by accuracy.
  - **Find:** find the listed objects among decoys; a hint after 20 s without a find; stars by time left.
  - **Restore:** drag the torn photo's 12 pieces; each snaps within `snapDistance` of its place.
  - **Trace:** follow a dotted seam; points are stitched in order, and a finger that wanders off can rejoin further on. Distances are in centimetres, so the seam feels the same on every phone; a seam too big for the screen is drawn smaller.
  - **Tune:** drag the radio dial; signal bars rise near the station; holding on it for `holdSeconds` tunes it in.
  - **Choose:** the conversation is Ink, like every scene; a choice tagged `#good` is the good answer, each choice has a timer, and when time runs out the talk moves on with a weak answer.
  - **Reassemble:** the letter's strips (one Ink line each) start out of place; tap two to swap.
  - **Stars** never fall below 1, so nothing blocks the story.
- **Decisions (proposed numbers, `balance.json`):**
  - The easy assist widens rhythm's windows ×1.5, trace's tolerance ×1.5 and tune's station ×2, and gives choose ×1.5 time. Find, restore and reassemble are puzzles with a clock, so they have no assist.
  - Tune needs the dial held on the station for 1 s.
  - **Retry:** right after a memory under 3 stars, the game offers another try for `skipRetryGems` (10) through the platform (`memoryRetry:<id>`), or carrying on. The best stars count.
  - **No `Memory_Template` scene.** Memories play over the café, so a new memory is only content: a `memories` entry (`gameKnot`, `objects`, `decoys`, `line`) and its Ink knots.
- **Practice memories:** chapters 2–9 aren't written yet (missing/11), so `Content/memory_practice.json` and `Story/memory_practice.ink` [draft] hold one of each type. The Editor menu **Gahwit Sitti > Play memory game** plays them in Play mode, without saving.
- **Still missing:**
  - Each chapter's own memory content, cutscenes and 1974 sets (11, 13).
  - A free replay later, which needs a place to replay from, such as a memory album.
  - A gem retry interrupted by a lost connection is applied by the platform but not replayed by the game.

## 2026-10-31: phone builds, CI, download size and performance data (missing/23)

- **Android:** `BuildTools.BuildAndroid` (menu **Gahwit Sitti > Build Android**) makes an app bundle: IL2CPP, arm64, target API 36 (Google Play's minimum from 31 August 2026).
  - **Size:** a 44 MB bundle; bundletool puts the download at 45 MB.
  - **Signing:** the upload key comes from the environment (`GAHWIT_ANDROID_KEYSTORE` and its passwords) and never stays in Player Settings. Without it the bundle is debug-signed, which Google Play refuses. The key doesn't exist yet.
- **iOS:** `tools/ios_testflight.sh` archives the Xcode project and prints its size: 108 MB installed and a 36 MB compressed `.ipa`, far under the 150 MB limit. With an App Store Connect API key, `upload` sends it to TestFlight.
  - Xcode's app-thinning step crashes on this Mac (macOS 27), so the script exports without thinning by default (`THIN=1` asks for it).
- **Build numbers:** each build takes the number of commits, or `BUILD_NUMBER` in CI. Player Settings keep their own numbers, so builds don't change tracked files.
- **CI** (`.github/workflows/gahwit-sitti.yml`):
  - The platform's tests run on every push, against a PostgreSQL service.
  - The Unity EditMode tests run on every push, and PlayMode too at night, through GameCI.
  - Android and iOS builds run every night or on demand.
  - The Unity jobs skip themselves until the licence secrets are set.
- **Performance:**
  - **The `perf` event:** after two minutes of play (`ui.graphics.perfSampleSeconds`), the game sends average fps, the share of frames slower than 30 fps, the worst frame and memory, once a session. Testers' phones, such as the 3 GB Android, report without anyone watching (`Core/FrameSampler.cs`, tested).
  - **Fewer allocations per tap:** order cards rebuild only when orders or their readiness change, style chips are built once per task, and `Game.IsWanted` loops instead of LINQ.
- **Git LFS** rules now cover images, models, audio and video anywhere in the game's folder (`games/gahwit-sitti/.gitattributes`).
- **Still missing:** the store uploads and the upload key (accounts only the owner can create), the CI secrets, the test on a 3 GB Android, a memory budget from its numbers, and remote content through Addressables.

## 2026-10-31: touch targets, settings, accessibility and feel (missing/15)

- **Touch targets:** every button is now 44 pt or more on the narrowest phone we support (360 pt wide), which is 132 units on the 1080-unit canvas.
  - `UiKit.MinTouch` comes from `balance.json` `ui` (`minTouchTargetPt`, `smallestPhoneWidthPt`) through `Core/UiRules.cs`.
  - A ready order card is now one big serve button with a tick, so a ready order never relies on colour alone.
  - **First play-mode test:** `_Game/UI/PlayTests/TouchTargetTests.cs` boots the game and checks every visible button on the café, the kitchen, settings, a prompt card and storage. Run it with `test run --mode PlayMode`. It uses the Editor's own save, like pressing Play.
  - The Editor menu **Gahwit Sitti > Check touch targets** runs the same check on whatever screen is open.
- **Settings:** a new "Accessibility" section.
  - **Large text:** every size × 1.2 and nothing under 12 pt. The game reloads to apply it, like a language change, because every screen is built for its text size. The kitchen's hint bar grows with it.
  - **Reduced motion:** the player's switch, and the phone's own setting too (iOS Reduce Motion through `Plugins/iOS/GahwitAccessibility.mm`; Android's animator scale of 0). It turns off every motion below and makes camera moves cuts.
  - **Easy memories:** widens the pour target by `easyWiden`. **Save schema v8** adds `settings.memoryAssist` (off), with a migration test.
  - **Graphics:** auto, low or high, at once. Low drops the sun's shadows and edge smoothing and runs at 30 fps; high runs at 60 fps. "Auto" picks low on phones with 3 GB or less (CLAUDE.md). The numbers are in `balance.json` `ui.graphics`.
  - **English lays out left to right.** The `UiKit` helpers mirror every side, anchor, row and fill when the language is English, so the same screen code works both ways. Item and generator names now use their English names from `balance.json`.
- **Feel** (`_Game/UI/Tweens.cs`, fixed pools, nothing allocated per frame):
  - **Merge:** the moving piece slides into its partner, a soft flash, the new item pops past its size and settles, and a sparkle in its colour.
  - **Generator tap:** the tin squashes and a dot arcs to the new item's square.
  - **Order served:** coins burst from the order and fly to the coin counter.
  - **Level up:** the rewards count up under the petal burst.
  - **Decor placed:** the piece squashes and bounces, a dust puff rises, and cosiness ticks up.
  - **Memory:** a sepia wash spreads from the last touch, and film grain fades in over the café.
  - **Panels** spring up from below in 0.35 s (`ui.panelSlideSeconds`).
- **Still missing:**
  - Neutral-Arabic subtitles for dialogue, which need the neutral lines first (11).
  - Restore purchases (17).
  - The served item flying to its customer, who isn't on the kitchen screen.
  - Checking the sizes and the motion on a real phone.

## 2026-10-31: local notifications (missing/19)

- **Package:** Unity's Mobile Notifications (`com.unity.mobile.notifications` 2.5.0), through its unified API.
- **`NotificationPlan`** (`_Game/Notifications`, plain C#, 4 tests) plans the notes when the app goes to the background. It makes none before the first memory.
  - **Energy full:** when the bar fills.
  - **A regular is due:** at the start of their next visit.
  - **A task is affordable:** 2 hours later (`taskReminderHours`, proposed).
  - **Away:** after 3 days.
  - Quiet hours (23:00 to 08:00) move a note to 08:00, and at most 2 come a day, the earliest first. All numbers are in `balance.json` `notifications`.
- **`NotificationService`** (Boot):
  - It asks permission once, right after the first memory, never at first launch. The package also handles Android 13's runtime permission.
  - It schedules on pause and clears everything on return.
- **One switch per type** in settings: energy full, a regular arrives, a task you can restore, after a break.
  - Save schema v7 turns `settings.notifications` into one switch per type; each starts as the old single switch was (tested).
  - Event and hosting switches come with those systems.
- **Text** is in the café's voice (Amman dialect, decision in missing/02), from COMPLETE_SPEC.md section 3. Characters now carry `female` so the Arabic agrees: «سميرة وصلت».
- **Still missing:**
  - The Ramadan-night option, which isn't in the settings design.
  - The event-ending note (missing/18).
  - Server push for hosting (missing/20).

## 2026-10-31: storage, bubbles, energy refill, level and chapter screens, unlock cards, pacing test (missing/05)

- **Storage** (`Merge/Game.Kitchen.cs`, tested):
  - Opens at level 2 with `board.inventorySlots` (6) slots. `board.storageSlots` now counts slots bought with gems through the platform (`storageSlot:N`).
  - **Store** sits beside Sell for the picked item; with nothing picked, the same place shows **Storage (n)**. The storage sheet takes pieces back and buys a slot.
  - **Decision:** rewards that don't fit the board still wait in storage, past the limit. The old automatic return to the board is gone, because it would empty storage the player filled on purpose.
- **Free space:** after a chapter, or after 4 hours away, a card offers to sell or store the lowest pieces no order wants, until a quarter of the board is free.
- **Bubbles:** a merge to level 4 or higher floats a copy 6% of the time, for 60 seconds.
  - It shows in the kitchen's top corner with a countdown, and is bought with gems through the platform (`bubble:L:chain`; the chain comes back in the grant).
  - The free ad pop waits for ads (missing/17).
- **Out of energy:** a card instead of a toast, with the time until full and a gem refill through the platform (+100). The price shown rises with refills that day; the platform has the final say. The ad option waits for ads.
- **`GemShop`** (Boot): each gem purchase is kept on the phone until its grant is applied. A purchase cut off by the connection or a closed app is sent again with the same request id, so the platform spends once.
- **Level up and chapter complete:** cards with the rewards and a petal burst (`PromptView`) instead of toasts. When a chapter ends, the café's bulbs go dark and come on one by one.
- **Unlock cards:** `Merge/Unlocks.cs` (tested) reads the one schedule in `balance.json`. Features in `unlock_cards.json` get a card: only the ones this build has, at most one a session, each once, never during a scene, a panel or the hands-on tutorial. Save schema v6 keeps `player.unlocksShown`.
- **Pacing test** (`_Game/Pacing/Tests`): the simulator's typical player (4 sessions, 3 ads, 85% of what she makes served) plays the real rules for 35 days. It fails when a chapter ends more than 25% (at least a day) from `chapters[].targetDay`, which is now in `balance.json` for both the simulator and the test. Chapters without content yet are paid from their task costs.
  - **It found a real problem:** with tips of 2–10 coins (SYSTEMS_B.md), chapter 6 finished on day 5 instead of 7. **Decision: tips are now 1–5 coins** (proposed, written into SYSTEMS_B.md). With that, every chapter is within range: 1, 1, 1, 2, 4, 6, 9, 12, 20 and 30 against targets 1, 1, 2, 3, 5, 7, 10, 14, 21 and 30.
- **`Game.cs`** started its split: the kitchen extras live in `Game.Kitchen.cs` (missing/01).

## 2026-10-31: Salma's wardrobe (missing/09)

- **Decision, how an outfit scores:** each piece has style tags, and each chapter's occasion lists the tags it likes. Worn pieces that match make the hearts: always 1, 2 for 2 matches, 3 for 4 (`levelB.wardrobe.heartsAtMatches`, proposed). The bonus is 100 × hearts × chapter coins, paid once per chapter. It never blocks the story. Written into SYSTEMS_B.md section 6.
- **Content:** `Content/Resources/Content/wardrobe.json` has 5 modest pieces: a blush linen top, sage trousers, a cream cardigan, a lilac hijab (optional, never required), and an embroidered dress earned by restoring the dallahs. It also has chapter 1's occasion, "a family lunch" (it suits family, traditional and cosy).
- **`WardrobeRules`** (`_Game/Wardrobe`, plain C#, 4 tests): opens at level 4, buys with coins (`priceCoinsByTier`), wears or takes off one piece per slot, earns pieces from tasks, and works out and pays hearts.
- **Café:**
  - Salma stands behind the counter wearing her outfit (`CafeSalma`, simple shapes until her 3D model).
  - The **Wardrobe** button opens her sheet: the occasion with a hearts preview and "I'm ready", her pieces, and pieces for sale.
- **Save schema v5:** `wardrobe.occasions` keeps the hearts won per chapter; `owned` and `worn` are now read and written.
- **Still missing:** the 3D Salma with swappable pieces and 2D piece icons (missing/13), occasions for chapters 2–10 (missing/11), and premium bundles (missing/17).

## 2026-10-31: decor, cosiness, live time of day, winter weather (missing/07)

- **Catalogue:** 15 pieces in `Content/Resources/Content/decor.json`, across Old Levantine, Modern Ammani, Gulf majlis and neutral.
  - Each has cosiness (1–5), a price tier (`levelB.cafe.decorPriceCoinsByTier`), the level it reaches the shop at, a footprint in 0.25 m cells, and placeholder shapes.
  - 5 pieces arrive when the shop opens at level 2, 5 more at 4, and 4 at 6.
  - **Teta's 1974 radio** comes from 3 stars in her pour memory (SYSTEMS_B.md section 5).
- **Rules** (`_Game/Decor`, plain C#, 7 tests):
  - `DecorLayout`: snaps to the grid, turns in quarter turns, allows no overlaps, and keeps clear of the counter, the tables with their chairs, and the way in from the door.
  - `DecorRules`: buys a piece with coins (it's placed in the first free spot), moves, turns, stores and places pieces, and changes the sign's and chairs' styles for free.
  - Cosiness is the sum of placed pieces plus `styleSetBonus` for each style with 3 or more pieces placed. It sets the seats and the tip bonus (missing/08).
- **Café:**
  - `CafeDecor` draws the pieces.
  - The **Decor** button (from level 2) opens decor mode: a bar shows cosiness and the next table, with Shop, Turn, Store and Done.
  - Press a piece to pick it, drag it to move it, and tap the floor to let go. Dragging the floor still turns the camera.
  - The shop sheet lists pieces for sale, pieces in storage, and the sign's and chairs' styles.
- **Camera:** double-tap to look closer, double-tap again for the whole room. It cuts instead of gliding when reduced motion is on.
  - The small orbit script stays; Cinemachine is removed (BUILD_GUIDE.md item 11 and `unity/README.md` updated).
- **Time of day:** the light and music now follow the clock while the app is open; a memory keeps its own music.
- **Weather:** rain on the window or a light fog on some winter days, picked from the date (`Core/Weather.cs`, tested; `balance.json` `weather`, proposed).
- **Save:** `cafe.owned`, and `cafe.placed` as grid cells with turns in degrees. The schema example was updated; it had no saves yet, so the version stays.
- **Proposed numbers:** `styleSetBonus` 10, and the weather chances.
- **Still missing:**
  - Wall pieces and a wall rule (all 15 stand on the floor).
  - The kitchen corner, the rooftop and Teta's back room.
  - Real models (missing/13).
  - Measuring speed on a 3 GB Android (missing/23).

## 2026-10-31: customers in the café, Abu Fadi as a regular (missing/08)

**Decisions**
- **Every order card is a customer sitting in the café.** Order slots stay as they were (3 to 5 by level), so pacing keeps its shape.
- Cosiness sets how many can sit: 2 to 6 seats (`levelB.cafe.walkInTables`, `cosinessForExtraTable`). Before the chairs task nobody sits, and the rest wait standing by the counter.
- Walk-ins who wait 10 minutes unserved leave without penalty, and a new customer takes the slot. Regulars and the tutorial's first order wait.
- Abu Fadi's friendship counts from the start, as ring 1's exception. Other regulars' friendship opens at level 6 (`unlocks`).
- Regulars only order on their schedule (`levelB.regulars.schedule`). Abu Fadi comes mornings and afternoons, so evening and night testers won't see him after his first order. Watch this in the playtest.

**Built**
- **`CustomerRules`** (`_Game/Customers`, plain C#, 5 tests):
  - Tips of 2–10 coins from walk-ins, raised by cosiness up to the cap.
  - Walk-ins leave after 10 minutes.
  - Friendship: +3 for a regular's favourite item, +1 for any other, and level L needs 10 × L points.
  - The scene due at levels 3, 6 and 10.
  - Seats.
- **`CafeCustomers`** (Cafe): pooled placeholder figures walk in through the door, sit facing a table or wait by the counter, sip when served and walk out.
  - The door bell rings for each new customer.
  - Each waiting customer shows what they ordered in a bubble over their head (`CafeScreen`).
- **Drag to serve:** dropping a finished item on its customer's order card serves it. The serve button still works.
- **Abu Fadi:** his favourite is a level-3 coffee (`chapters.json`). His level-3 scene, `abu_fadi_3` (his lost love), is a **[draft]** in Amman dialect for the native writer.
- **Save schema v4:** each order keeps when its customer walked in. The `regulars` section and `cafe.cosiness` are now read and written.
- **Pacing:** `sim/simulate.py` now adds walk-in tips. With the spec's 2–10 coins, a typical player finishes chapters 4–9 one or two days early, and chapter 10 on target (day 30). With 1–5 coins, about a day early.
  - The tip range stays as the spec says. Tune it, or task costs, after the playtest.

**Still to do**
- Real character art and animations (missing/13).
- The cast list and a regular's profile screen.
- The other 11 regulars and their scenes (missing/11).

## 2026-10-31: the first-time tutorial (missing/06)

- **Steps as data:** the 11 steps from COMPLETE_SPEC.md section 1 are in `Content/Resources/Content/tutorial.json`. Each has an `until` (what finishes it), and the hands-on ones have a target, a screen and a text key.
- **`TutorialFlow`** (`_Game/Tutorial`, plain C#, 4 tests):
  - Each step finishes when the game's state shows it, so closing the app resumes at the same step.
  - Steps 4–8 are the hands-on ones; the banner counts them 1 to 5, as in `screens/Tutorial`.
  - Every step logs `tutorial_step`.
- **`TutorialView`:** a banner with "step N of 5" and the instruction, a ring on what to touch, and a finger that shows the drag or the tap.
  - It never blocks input, so a player can't get stuck behind it.
  - On the wrong screen it points at the tab that leads to the right one.
  - It waits under scenes and panels.
- **What each step does:**
  1. The age gate.
  2. The intro.
  3. The door bell, then Abu Fadi's arrival, now its own Ink knot (`abu_fadi_arrives`). Then the kitchen opens.
  4. Merge two beans.
  5. Tap Teta's tin. The energy counter appears only from this step.
  6. Serve Abu Fadi.
  7. Earn coins for the sign, with a coin count.
  8. Restore the sign and choose its style.
  9. The pour memory.
  10. The letter.
  11. Free play, with 3 easy orders (one item, level 2 at most; `balance.json` `tutorial`).
- **The `#sys` instruction line** left `chapter1.ink`. Instructions are neutral-Arabic UI text now (`tutorial.*`).
- **Decision, "skippable after the first time":** once this phone has finished the tutorial, later accounts on it (after deleting an account, say) get a Skip button. Skipping stops the guidance; the story still plays.
- **Save schema v3:** `tutorial.easyOrdersLeft`, with a migration and a test.
- **Checked in the Editor:** as a brand-new player, the pointer and banner showed on the merge, and after a restart on the tin, pointing at the kitchen tab from the café.
- **Gaps, each waiting on another item:**
  - Abu Fadi doesn't walk in yet (missing/08).
  - The 15-second intro clip doesn't exist (missing/13).
  - Serving is still a button on the order card, not a drag to the customer (missing/08).
  - Coins don't fly to the counter (missing/15).

## 2026-10-31: game events and tester numbers on our own platform (missing/16)

- **Choice:** events go to our platform first, so the ring 1 tester gate needs no third-party account. `IAnalytics` (Core) lets Firebase or GameAnalytics sit behind the same calls later, for crash dumps and push.
- **Game:** `PlatformAnalytics` queues events.
  - They're kept on the phone while it's offline (up to 2,000) and sent in batches of 200 with each cloud sync and when the app goes to the background.
  - Errors and exceptions become `client_error`, once per message per session.
  - Tests cover offline queuing and deduplication.
- **Events sent now:** `session_start`, `session_end`, `generator_tap`, `merge`, `order_served`, `task_complete`, `level_up`, `energy_empty`, `board_full`, `scene_viewed`, `memory_played` and `letter_found`.
  - `tutorial_step` comes with the tutorial (missing/06). Ads, shop and live-event events come with those systems.
  - Scenes can't be skipped yet, so there's no `scene_skipped`.
- **Platform:**
  - `POST …/players/me/events` stores events. They're deleted with the account.
  - `GET …/admin/metrics` (viewer) gives the gate numbers: players, sessions, players per tutorial step, reached the first memory, found the letter, day 1 return, average session length, and error-free sessions. It works in memory, which is fine for playtests; move it to SQL before soft launch.
  - 48 tests.
- **Checked end to end:** Editor sessions reached the database, including one queued at quit and sent on the next launch.
- **Not done:**
  - Native crash dumps need an SDK (Crashlytics, or Unity's).
  - Attribution for paid installs, and the soft-launch dashboards.
  - Apple's tracking prompt, only if ads or attribution use the advertising id (missing/17, 21).

## 2026-10-31: music, effects and haptics play (missing/14)

- **Cues by id:** `AudioCatalog` (`Resources/Audio/AudioCatalog.asset`) lists every file under `Assets/Audio` by name. Rebuild it with **Gahwit Sitti > Rebuild audio catalog**. A test checks that every cue named in code exists.
- **Import settings** (`AudioCatalogBuilder`): music streams. Effects load decompressed and in mono, so playing one costs nothing and the download stays small.
- **`AudioService`** (Boot) plays cues on 8 pooled sources with no allocation per play. It lives for the whole run, so music doesn't restart when the game reloads.
  - Groups are volumes in code: the sound switch mutes every effect, and the music switch mutes the music.
  - A mixer asset can come later, when a group needs its own effects.
  - The interface asks for sounds through `UI/UiSound`, which does nothing until the service exists, so tests run silent.
- **What plays:**
  - Every button: `ui_tap`.
  - Panels opening and closing.
  - Kitchen: generator taps (chains without their own sound use coffee's), merges that rise in pitch with the item's level, `merge_top` for a chain's top item, sell, serve with coins, level up.
  - Café: a task done plays `decor_place`.
  - Gems arriving, errors (no energy, board full), the drawer, the letter, and the pour loop while held.
- **Music:**
  - The café track follows the time of day; the afternoon shares the morning track.
  - The memory theme plays in 1974.
  - The main theme plays for the intro and for 30 seconds after a finished chapter.
  - Music ducks under Teta's voice note.
- **Haptics:** light for a merge, medium for a serve, success for a level-up. They use `Plugins/iOS/GahwitHaptics.mm`, with a new haptics switch in settings. Android vibration comes with the Android build (missing/23).
- **Still missing:** ambience tracks, more generator sounds, loudness checks, and native listening for the drafts (missing/14, "Audio not made yet").
- **iPhone build** (2026-10-31): Sentis and App UI are gone from `Builds/iOS/Libraries`.
  - The app is 176 MB, mostly debug symbols in `UnityFramework`; an App Store archive strips them. Measure the real download in missing/23.

## 2026-10-31: gems earned and spent through the platform, update required (missing/03, 04)

**Platform** (46 tests)
- **`POST …/players/me/rewards`** with `levelUp:N` or `chapter:N`:
  - The platform pays the gems from the published `balance.json`, once per player and id.
  - The latest cloud save must show the reward, or the reply is `409 not_earned_yet`.
- **`GET …/players/me/wallet`** returns the balance.
- **`POST …/players/me/spend`** with an item and a request id:
  - Items: an energy refill (each one costs more within a day, UTC), a storage slot (once each), a bubble, or a memory retry skip.
  - A repeated request id spends only once.
  - The game applies what it bought only after the reply.
  - Premium decor prices wait for the decor catalogue (missing/07).
- **`GET /games/{slug}/version`** returns `game.json` `minGameVersion` (now `0.1.0`).
- **Published config:** a game's `balance.json` becomes config version 1 when nothing is published. In Development a changed file is published again.
- **Bubble prices** were prose; they're now `formulas.bubbleGems` in `balance.json`.
- The purge job also deletes reward claims and gem spends. Migration `RewardsAndSpending`.

**Game**
- **Gems** in the HUD come from the wallet at launch and after every reward.
- **Rewards:** after each cloud upload, the game asks for every level-up and finished chapter it has earned (`EarnedRewards`, tested). Paid ids are kept in `gahwit-sitti-rewards.json`, so rewards earned offline are claimed later. A level-up or finished chapter syncs 3 seconds later.
- **Checked end to end** in the Editor against the local platform: reaching level 3 and finishing chapter 1 paid 6 + 6 + 10 gems once.
- **"Update required":** a screen the player can't close when the platform's minimum version is newer (`VersionRules`, tested). Its store address in `platform.json` is a placeholder.
- **Offline:** Help shows "sending needs the internet" while the platform can't be reached.
- **Age switches:** `AccountService.PersonalisedAdsAllowed` and `SocialAllowed` are false for under-13s and unknown ages until a parent agrees. Ads and social features must ask them when they're built.

**Still needs you**
- Hosting for the platform (HTTPS, managed PostgreSQL, a secret store), then `baseUrl` in `platform.json`.
- Store keys for receipts and notifications.
- Google sign-in client ids.
- The App Store address.
- How a parent agrees, which needs legal advice (missing/21).

## 2026-10-31: spec conflicts settled, missing numbers added (missing/02)

Each decision fixed the file that lost. Overturn any of them by changing `balance.json` and the spec together.

**Rule used:** `balance.json` wins on numbers and `game.json` on products and the name. The level B docs (SYSTEMS_B.md) win over older text in GAME_SYSTEMS.md.

**Decisions**
- **Storage:** 6 slots (`board.inventorySlots`), opening at level 2. Fixed COMPLETE_SPEC.md and the save schema example.
- **One unlock schedule:** `balance.json` `unlocks`. It merges COMPLETE_SPEC.md section 2 and GAME_SYSTEMS.md section 8:
  - The Sitti wall opens at level 10, not 14.
  - Level 8 opens mini board events. The old "storage tutorial at 8" goes, because storage opens at 2.
  - Level 15 opens the full event calendar.
  - Hosting opens at level 12; friends at 10.
  - The duplicate `social.unlockPlayerLevel` and `sittiWall.unlockPlayerLevel` keys are gone.
- **Times of day:** four (SYSTEMS_B.md), starting at 6, 12, 17 and 20 (`levelB.cafe.timeOfDayStartHours`). The afternoon hour and the afternoon look are new. `CafeRoom.TimeFromClock` reads the hours, with tests.
- **Memories:** chapter 5 reuses Find in a new room. Chapter 10 ends with Pour: Salma makes the two cups Teta's letter asks for. Ring 2's memory list was fixed to match (BUILD_GUIDE.md).
- **Decor count:** 25 at alpha (PLAN.md) and 30 at soft launch (BUILD_GUIDE.md) are two stages, not a conflict.
- **Products:** `game.json` is the only product list, because the platform grants from it. `balance.json` keeps only display data keyed by SKU (`store.productDisplay`). Every price and content matched before the old list was removed.
- **Season pass:** a **non-renewing** product, because Apple has no 28-day auto-renewing period. The platform runs the 28 days. Added `ProductType.NonRenewing` on the platform.
- **Style ids:** `old`, `modern` and `gulf`, as saves and code already use.
- **Races:** replaced by hosting everywhere (SYSTEMS_B.md section 7). `screens/Race` is removed, and the events hub shows hosting.
- **Notifications:** written in Salma's café voice (Amman dialect). CLAUDE.md's neutral Arabic is for menus.
- **Name:** "Gahwit Sitti" in docs, art and screens. Research notes keep their original titles.
- **Ink folder:** `Content/Resources/Story/` (fixed in CLAUDE.md), so the game can load the stories at runtime.

**Numbers moved into `balance.json`**
- `unlocks` (with `noShopOrAdsBeforeLevel` 3).
- `tutorial`: 3 easy orders after the tutorial.
- `saves`: 2 s local, 60 s cloud.
- `notifications`: 2 a day, quiet 23:00–08:00, away 3 days, 6 hours before an event ends.
- `ui`: large text 1.2×, smallest text 12 pt, touch targets 44 pt, panel slide 0.35 s, kitchen hint thresholds.
- `board.freeSpaceOfferAfterHoursAway`: 4.
- `orders.twoItemOrderFromLevel`: 5, which used to live inside a key's name.
- The code now reads the save timings, time-of-day hours, two-line order level and kitchen hints from the file.
- Account deletion (30 days) stays a platform setting (`Accounts:DeletionGraceDays`).

**Proposed numbers** (the specs gave none; each block says `proposed`, to tune in playtests)
- Tip bonus from cosiness: 0.5% per point, up to +50%.
- Decor price tiers: 50 to 2,000 coins. Wardrobe price tiers: 100 to 1,500 coins.
- 10 gems to skip a memory retry, plus starting numbers for the 7 other memory types.
- Hosting rewards: host 15 gems, helpers 5, and solo play with bots pays half.
- Season pass: a 5-tier reward cycle per track, with premium items at tiers 10, 20 and 30.
- Album: set, completion and star-chest rewards.
- Mini board: an olive chain of 8 items (draft names) and 10 milestones.
- Daily tasks: 12 tasks.

**Pacing:** `sim/simulate.py` results are unchanged. None of the new numbers feeds chapter pacing until customers' tips exist (missing/08).

## 2026-10-31: Boot and Café scenes, package cleanup, audio drafts (missing/01)

- **Two scenes** replace `SampleScene` (`unity/README.md`):
  - `Boot` (`_Game/Boot/BootScene.cs`) loads config, content, the save, the text tables and the time service. It starts signing in, then opens `Cafe`. Signing in carries on in the background, so an offline start is just as fast.
  - `Cafe` (`CafeScene.cs`, which was `GameBootstrap.cs`) builds the café and the interface.
- **One composition root:** `Services.cs` holds what the scenes share.
  - A new language, a cloud save or a deleted account restarts from `Boot` with everything loaded again (`BootScene.Restart`).
  - Playing `Cafe` straight from the Editor still works: it makes the services itself.
- **Removed:**
  - Unused packages: Sentis (`com.unity.ai.inference`, which also pulled in App UI), Visual Scripting, Collab proxy and Multiplayer Center. Their scripting defines went with them.
  - The URP template's `TutorialInfo` and `Readme.asset`.
  - Kept: the AI Assistant (an Editor tool that ships nothing in the build), and Navigation for walking customers.
- **`.claude/`:** `worktrees/` stays git-ignored, as does any `settings.local.json`. Shared Claude Code settings may be committed.
- **Platform:** player accounts, cloud save, support and staff roles are documented in `platform/README.md` (commits `a4e9ac9`, `e487de7`, merged in `89ccf91`).
- **Draft music and effects** made with ElevenLabs: 5 music tracks and 20 effects in `Assets/Audio/{Music,Sfx}/Drafts`. Remake them with `tools/audio_drafts.py`, listed in `tools/audio_drafts.json`. They're placeholders until the audio freelancers deliver (COMPLETE_SPEC.md section 7). Nothing plays them yet (missing/14).
- `missing/` is the 2026-10-31 review of what the game still needs. Finished items are struck there and logged here.

## 2026-10-31: iPhone build, player accounts, settings, draft voice

**iPhone build**
- **Gahwit Sitti > Build iOS (Xcode project)** writes `unity/Builds/iOS` (`BuildTools.cs`; also runs with `-executeMethod`).
- Signing uses team `QJ9MPD82U5` with automatic signing.
- `IosPostBuild.cs` links AuthenticationServices and adds the Sign in with Apple capability.
- Addressables (the text tables) now build with the player.
- Two problems found on a real iPhone 14 Pro:
  - **Text tables:** the blocking wait for them (`WaitForCompletion`) never finished on the phone. They now load in a coroutine before anything else is built (`Loc.Load`, `Services.Start`).
  - **Café materials:** they were copied from the render pipeline's default material, which exists only in the Editor. They now come from `Resources/Materials/CafeLit.mat`; `CafeLitEmissive.mat` keeps the glow variant in builds.

**Accounts (COMPLETE_SPEC.md sections 4, 9, 14, 16)**
- **Platform client** (`_Game/Platform`, plain C#, 15 tests):
  - Guest account on first launch, kept by a sign-in token in the iPhone Keychain.
  - Access tokens renew themselves.
  - Everything returns "offline" instead of failing.
  - Lives in the game for now; it moves to `unity-shared` once it has worked here (`unity-shared/README.md`).
- **Cloud save** (`CloudSync.cs`):
  - Uploads every 60 seconds and when the app goes to the background.
  - At launch it compares with the platform. When both phones changed, the player chooses "this phone or the other one", with level and chapter shown for each.
  - A new install takes the cloud save without asking.
  - What the phone last agreed with the platform is kept in `gahwit-sitti-sync.json`, beside the save.
- **Sign in with Apple:** `Plugins/iOS/GahwitAppleSignIn.mm` asks for no name and no email. If the Apple id already has a café, the player picks which café to keep. Google sign-in on Android isn't built.
- **Age gate** (`screens/AgeGate`):
  - Language and birth year, with no year picked in advance.
  - The age band counts the younger possible age (`AgeRules.cs`, the same rule as the platform).
  - Under-13s and unknown ages need a parent for ads and social features. Neither feature exists yet.
  - The parent-consent flow itself isn't designed.
- **Settings** (`screens/Settings`; the gear at the top left of the café):
  - Switches for sound, music, notifications, explained words and full voice. Only the explained-words switch has an effect today; the rest are saved for when audio and notifications land.
  - Language, link account, help and contact (FAQ plus a form; the platform adds player id, version and phone), privacy and terms, and delete account.
  - Restore purchases waits for in-app purchases.
- **Save schema v2:** `account` (birth year, parent consent), plus `settings.notifications` and `settings.voice`. A v1 save migrates and shows the age gate once (tested).
- **Placeholders:** `Resources/Config/platform.json` holds the platform address and the legal page addresses, all still in [brackets]. Until they're set, the phone plays offline as a guest. The Editor uses `http://localhost:5080`.
- **Text:** new menu text is neutral Arabic, as CLAUDE.md asks, so the dialect lines in the AgeGate and Settings designs were rewritten. The English needs a review.
- **Touch targets:** new buttons and rows are at least 120 canvas px, about 44 pt on an iPhone 14 Pro. The existing tab bar buttons are 96 px (about 35 pt) and should grow.

**Draft voice**
- Teta's voice note now plays a recording: `Resources/Voice/Drafts/voice_1_teta.mp3`, 10.6 s, made with ElevenLabs. The subtitles follow the clip's length.
- It is a **draft**: PLAN.md keeps main voices for native actors. A native listener should check the Ammani pronunciation.
- `tools/voice_drafts.py` remakes draft lines from `tools/voice_drafts.json`. The API key comes from the git-ignored `/.env`.

## 2026-10-30: task style choice (backlog 16)

- The sign and chairs tasks offer three styles: Old Levantine, Modern Ammani and Gulf majlis (`chapters.json` `styles`, colours from the prototype and `cafe-3d`).
- Each chip on the task card is painted like the result. The chosen one has a thick outline and bold text, so the choice doesn't rely on colour alone.
- The choice is saved in `cafe.taskStyles`. It recolours the sign's board and lettering and the chairs' seats and backs. The 1974 memory always shows Teta's Old Levantine look.
- Changing a style later for free (GAME_SYSTEMS.md section 9) needs the decor screen.

## 2026-10-30: the pour memory and Teta's letter (backlog 17, 18)

- **Memory framework.**
  - A task can name a memory (`chapters.json`, `"memory": "m1_pour"` on the sign task, matching COMPLETE_SPEC.md section 1, step 9).
  - A memory plays in order:
    1. A lead-in scene today.
    2. A fade to 1974: the café as Teta ran it, in faded film colours with grain (`CafeRoom.SetPast`).
    3. An intro scene.
    4. The memory game.
    5. A scene chosen by the result.
    6. An outro.
    7. A scene back in today.
  - Best stars are saved under `memories`. A memory cut short by closing the app plays again on the next launch.
- **Pour game** (`_Game/Memories/PourGame.cs`, 5 tests): hold to fill the cup, then release inside the line for 3 stars, above it for 2, or after a spill for 1. Releasing too early asks for "a bit more"; it never fails. The numbers come from the feeling demo and live in `balance.json` `levelB.memories.pour`.
- **The easy assist** that widens the line is built and tested but turned off. It needs a settings field (COMPLETE_SPEC.md section 5) and a settings screen.
- **The letter:**
  - After the memory, the counter's middle drawer glows and an "open the drawer" button floats over it.
  - The letter fades in line by line, then Teta's voice note plays with word-by-word subtitles (there's no recording yet), then Lana's message arrives as a phone notification. The letter is saved under `story.lettersFound`.
- **Text:** all memory and letter lines are in `chapter1.ink`, taken from `feel-demo/index.html`. New characters: `teta_young` (Um Khalil) and `fadi_young`.

## 2026-10-30: Unity Localization (backlog 5)

- Player-facing text comes from the Unity Localization **"UI" string table** (`Assets/Content/Localization`), with Arabic and English locales. `Loc.T(key)` reads it; the old `Resources/Loc/ar.json` is gone.
- The table preloads, and `Loc.Load` waits for it once at startup, so lookups are instant.
- **Gahwit Sitti > Set up localization** recreates the settings, locales and table if they are missing.
- **Tests check** that every key has Arabic and English text, that every key written in code exists, and that every character, walk-in and task has its text.
- English UI text was written by Claude Code and needs a review. The language setting (`settings.language`) is saved, but there's no settings screen yet.
- Dialogue stays in Ink (CLAUDE.md). A glossary table and English dialogue come with the story localization work.

## 2026-10-30: time service, save schema, generators on the board (backlog 3, 4, 6)

- **Time service** (`Core/TimeService.cs`): trusted time is server time (when the platform gives it) moved forward by the phone's uptime counter, which keeps counting while the phone sleeps. On Android that counter is `elapsedRealtime`, on iOS `CLOCK_MONOTONIC` (`Plugins/iOS/GahwitUptime.mm`). Changing the phone clock adds no energy (tested). After a restart the uptime counter starts again, so time falls back to the phone clock but never goes backwards. The platform's sanity check covers the rest (COMPLETE_SPEC.md section 11).
- **Saves follow `design/save_schema.json`** (`_Game/Save`). Sections the game doesn't use yet are kept as they are. Migration step 0 → 1 upgrades the first playable's save, and loading this Editor's real save worked.
- **Schema v1 changed before release.** No real saves existed yet, so the version stays at 1:
  - `player.energyStampUptimeMs` became `energyStampMs` (trusted time), because uptime restarts with the phone.
  - New fields: `clock` (the time service's anchor), `player.ordersServed` and `rng` (the seeded random state, for replaying bugs).
- **Generators are pieces on the board** (GAME_SYSTEMS.md section 4, save schema). Tap one for an item; merge two of the same level to upgrade it; they can't be sold. A chapter's generator copy now lands on the board to merge. Pieces that don't fit wait in `board.storage` and come back when there's room. The storage screen itself isn't built.
- **Ids follow the schema:** `abu_fadi`. Walk-in orders save no name; the shown name comes from the order id.
- **Story choices** are saved by the choice's first Ink tag (`* [text] #ask`), under `story.choices[knot]`. Scenes seen are saved too.
- **Tests:** 22 EditMode tests (Core 5, Merge 13, Save 4).

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
- ~~Text uses a JSON table, not Unity Localization (backlog 5).~~ Done, see above.
- ~~The save shape is not `save_schema.json` yet (backlog 3).~~ Done, see the entry above.
- ~~Energy uses the phone clock (backlog 4).~~ Done, see the entry above.
- There is one scene. Boot and Café scenes and a service locator are backlog 1.
- The camera is a small orbit script, not Cinemachine (backlog 11).
- Not built yet: decor placement and cosiness, walking customers and Abu Fadi as a regular, wardrobe, the 11-step tutorial with its hand pointer, the storage screen, bubbles, audio, analytics. ~~Pour memory and letter~~ are done (see above).
- Dialogue other than the prototype lines is `[draft]` and needs the native writers.
