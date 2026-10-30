# Gahwit Sitti: the parts not covered elsewhere

`GAME_SYSTEMS.md` covers the merge economy and `SYSTEMS_B.md` covers the level B systems. This file covers everything else a finished game needs. Each section is short enough to build from.

## 1. First-time experience (the first 10 minutes)

Goal: the player feels the story within 60 seconds and has served Abu Fadi within 3 minutes. The game shows no shop and no ads before the player reaches level 3.

| Step | What happens | Teaches |
|---|---|---|
| 1 | Language and age screen (`screens/AgeGate`) | |
| 2 | 15-second intro cutscene: Salma opens the shutters of the dusty café | Mood |
| 3 | Salma: "يا رب حدا يجي". Door bell; Abu Fadi walks in and orders | Customers |
| 4 | The kitchen opens with 4 green beans already placed. A hand pointer shows the first merge. | Merging |
| 5 | Tap Teta's tin for more beans; the energy bar appears for the first time | Generators, energy |
| 6 | Drag the finished cup to Abu Fadi; he sips and reacts | Serving |
| 7 | Coins fly to the counter; "Fix the sign" task glows | Tasks |
| 8 | Fix the sign, choose its style, watch it swing straight | Decor choice |
| 9 | Abu Fadi: "ستّك كانت تقعد هون كل صبح" → first memory (pour) | Memories |
| 10 | The drawer glows → the letter → Lana's notification | The mystery hook |
| 11 | Free play. The next three orders are guaranteed to be easy | |

**Rules for the tutorial**
- Every step can be skipped after the first time.
- The tutorial survives an app restart, because it resumes from the last finished step.
- Each step logs `tutorial_step` to analytics.

## 2. Unlock schedule

| Player level | Unlocks |
|---|---|
| 1 | Kitchen, coffee and sweets chains, café floor, chapter 1 |
| 2 | Storage (6 slots), decor shop (5 pieces) |
| 3 | Daily gift, starter pack offer, rewarded ads |
| 4 | 4th order slot, Salma's wardrobe |
| 5 | Recipes (the cooking pan) |
| 6 | Season pass, regulars' friendship levels |
| 7 | Memory album event |
| 8 | Mini board events |
| 10 | Friends, gifts, café visits, postcards, Sitti wall |
| 12 | 5th order slot, hosting (big orders with friends) |
| 15 | Events show their full calendar |
| Chapter 3 | Tea chain |
| Chapter 5 | Saudi coffee chain |
| Chapter 6 | Rooftop |
| Chapter 7 | Breakfast chain |

**Numbers:** the schedule lives in `balance.json` `unlocks` (this table and GAME_SYSTEMS.md section 8 were merged on 2026-10-31).

**How unlocks are shown:** each one gets a short frosted card with one sentence and one button. It never shows more than one card per session.

## 3. Notifications

Notifications are opt-in. Their text is in Salma's café voice (Amman dialect), like dialogue; CLAUDE.md's neutral Arabic is for menus. The numbers below live in `balance.json` `notifications`. The game asks only after the first memory, never at first launch. There are at most 2 a day and none between 23:00 and 08:00 local time, except when the player has chosen the Ramadan night option.

| Trigger | Text |
|---|---|
| Energy full | «الطاقة امتلت. أبو فادي بستنّى فنجانه» ("Energy's full. Abu Fadi is waiting for his cup") |
| A regular is due | «كريم وصل القهوة» ("Karim has arrived at the café") |
| Chapter task affordable | «صار معك عملات تكفي لترميم السطح» ("You have enough coins to restore the rooftop") |
| Friend needs help (hosting) | «نور محتاجة مساعدتك بعرس جارتها» ("Noor needs your help with her neighbour's wedding") |
| Event ends in 6 hours | «باقي ٦ ساعات على سوق الجمعة» ("6 hours left in the Friday market") |
| Away 3 days | «القهوة اشتاقتلك» ("The café misses you") |

All notifications are local, scheduled on the phone. Hosting messages also use server push (Firebase Cloud Messaging).

## 4. Settings

- Sound, music and haptics, each with its own switch.
- Notifications, with each type controlled separately.
- Language: Arabic or English. Dialogue stays in dialect, with an option for neutral-Arabic subtitles.
- Tap-to-explain words: on or off.
- Text size: normal or large.
- Graphics quality: auto, low or high.
- Account: guest, linked to Apple or Google, cloud save status.
- Restore purchases, which is required by Apple.
- Help and contact, privacy, terms.
- Delete account, which is required by both stores. It deletes the account on the platform within 30 days.

## 5. Accessibility

- **Text:** every text uses the type scale, with large mode about 1.2×. Nothing is smaller than 12 pt.
- **Touch targets:** 44 × 44 pt or larger.
- **Colour:** important states never rely on colour alone. Ready orders show a tick and a colour.
- **Motion:** a reduced-motion option honours the phone setting, so camera moves become cuts and screen shake is off.
- **Memory games:** every timing game, such as pour and rhythm, has an "easy" assist that widens the target.
- **Reading:** dialogue never advances on its own.

## 6. Languages and text

- All text lives in Unity Localization string tables, never written directly into a scene.
- Tables:
  - UI: neutral Arabic and English.
  - Dialogue: Amman or Najdi dialect, plus English.
  - Glossary: local word → meaning.
- Arabic text renders with RTLTMPro. Numbers use Western digits (0–9) in both languages.
- Fonts: one Arabic display face and one Arabic text face, both with full harakat support, with SF fallbacks on iOS. Check the licence before shipping.
- Names of people and places are never translated literally. Salma stays Salma.

## 7. Audio list

| Type | Items |
|---|---|
| Music | Main theme in Maqam Hijaz (oud + strings), café mornings (light), evenings (warm), night (soft piano), memory theme (music box), event theme, hosting theme |
| Ambience | Street by day, street by night, rain, café murmur |
| Interface | Tap, open panel, close panel, coin, gem, level up, error (soft) |
| Kitchen | Generator tap per chain, merge (pitch rises with item level), top item, sell, bubble pop |
| Café | Door bell, footsteps, chair, cup on saucer, pour, sip, laugh, placing decor |
| Story | Letter unfold, drawer, voice-note start, page turn |
| Voice | Key lines for Teta, Salma, Lana, Abu Fadi, recorded by native actors; other dialogue is text only |

Target loudness is about −16 LUFS for music. Sound effects play on separate mixer groups so the settings switches work.

## 8. Feel: motion and effects list

Every item needs a designed animation and a small effect.

- **Merge:** the two items pull together, a soft flash, the new item pops with an overshoot, and a level-coloured sparkle.
- **Generator tap:** the item squashes, then the new item arcs to its square.
- **Order served:** the item flies to the customer, then coins burst and fly to the counter.
- **Level up:** a full-screen petal burst and a soft chime. The rewards count up.
- **Decor placed:** a dust puff, a small bounce, and the cosiness number ticks up.
- **Memory transition:** a sepia wipe from the touched object, film grain fading in, and music crossfading to the music box.
- **Chapter complete:** the café lights come on one by one.
- **Transitions:** every panel slides with spring easing, about 0.35 s. No hard cuts in the interface.

## 9. Save data

The full shape is in `design/save_schema.json`.

- **Where saves go:** saved on the phone after every change, debounced to once every 2 seconds, and sent to the platform every 60 seconds and when the app goes to the background.
- **Conflict rule:** each save has a version. If the server has a newer version from another phone, the game asks: "keep this phone or the other one?", showing level and chapter for each.
- **Migration:** a `schemaVersion` field. Every change to the save format comes with a migration step and a test.

## 10. Offline play and errors

- **Offline:** the core game works fully offline (merge, café, story already downloaded).
- **Needs a connection:** purchases, hosting, friends, events and new chapter downloads. Each shows a gentle message: «بدنا إنترنت لهاد».
- **Purchases are never lost:** a purchase that started but wasn't confirmed is retried on the next launch. Unity IAP keeps it pending until the platform confirms it.
- **Server down:** the game keeps running with its last config and shows a small banner only when the player opens an online feature.

## 11. Fair play and anti-cheat

- **Server-owned:** gems and anything bought with money are only ever changed by the platform's replies.
- **Clock:** energy and event timers use server time when online. When offline, the game uses the phone's uptime counter (not the clock) since the last server time, so changing the date doesn't add energy.
- **Coins and items:** these are on the phone and not protected, which is acceptable because they can't be bought. The platform runs a sanity check on save upload: impossible jumps are flagged, not punished automatically.
- **Purchases:** receipts are verified on the server. A transaction id can only be used once.

## 12. Screen list

The Figma-style designs are in `screens/`. They need updating to the level B look.

| Area | Screens |
|---|---|
| Start | Splash, language and age, intro cutscene, tutorial overlays |
| Main | Café (3D, main screen), kitchen (merge), orders, storage, bubble offer |
| Café | Decor shop, decor placement, style picker, cosiness panel, task list, rooftop |
| Story | Dialogue, choices, memory loader, memory games ×8, letter, voice note, chapter complete, journal, glossary |
| People | Regular profile (friendship, stories), cast list |
| Kitchen extras | Recipe book, cooking pan |
| Wardrobe | Wardrobe, occasion check |
| Social | Friends, visit a café, hosting order, postcard maker, Sitti wall |
| Events | Events hub, pass, album, mini board, event results |
| Shop | Shop, starter pack, bundle offer, purchase result |
| System | Out of energy, level up, unlock card, daily gift, daily tasks, settings, help, offline, update required, save conflict |

## 13. Art asset list (season 1)

| Type | Count | Notes |
|---|---|---|
| 3D café spaces | 3 (floor, kitchen corner, rooftop) + 1974 version of the floor | Modular walls, floor and windows |
| 3D decor | 60 | About 20 base models × 3 style variants |
| 3D characters | Salma, Lana, Abu Fadi, young Teta, young Fadi, 12 regulars, 4 walk-in bodies with swappable heads | One shared rig |
| Character animations | Walk, sit, sip, idle, talk, laugh, wave, react happy and sad, pour, cook | Can start from Mixamo, then polish |
| 2D portraits | 10 main characters × 5 expressions | From the Flow art |
| Item icons | About 50 (5 chains × 8–10 levels) + generators | 2D renders of 3D or painted |
| Wardrobe | 40 pieces | As 3D on Salma, plus 2D icons |
| Memory scenes | 10 (several reuse the 1974 floor) | |
| Cutscenes | About 6 (Veo), plus in-engine scenes for the rest | |
| Interface | Frosted panels, icons (about 60), currency icons, buttons | |
| Marketing | App icon, 6–8 store screenshots per language, a 30 s preview video | |

## 14. Legal, privacy and store requirements

- **Company and store accounts:** Apple Developer (individual or organisation) and Google Play Console accounts, the company bank account and tax forms.
- **Privacy policy and terms:**
  - The privacy policy is hosted on a public web page.
  - It lists the data collected: account id, purchases, analytics, crash data.
  - It covers the Saudi PDPL, the UAE PDPL and Egypt's data law.
- **Store privacy forms:** Apple's privacy "nutrition label" and Google's Data safety form must match what the game and SDKs actually collect.
- **Age:**
  - The age gate at first launch.
  - Under-13s: no personalised ads and no social features until a parent agrees. This follows the UAE Child Digital Safety Law (comply by 1 January 2027) and Egypt's rules for under-15s.
- **Ratings:** IARC (through Google Play) and the App Store age questionnaire. Content stays within 12+.
- **Purchases:**
  - Prices use store tiers per country.
  - "Restore purchases" button.
  - Clear contents on every product.
  - No paid random items.
- **Account deletion** from inside the app (required by both stores).
- **Music and fonts:** licences kept in `art/licences/`.

## 15. Store listing

- **Name:** قهوة ستّي – Gahwit Sitti (check that it's available on both stores).
- **Subtitle (Arabic):** «رمّمي قهوة ستّك واكتشفي سرّ العيلة» ("Restore your grandmother's café and uncover the family secret").
- **Text:** keywords in Arabic and English (merge, café, story, decorating, cozy). Arabic listing first.
- **Screenshots:**
  1. The emotional letter moment
  2. The 3D café
  3. Decorating
  4. A 1974 memory
  5. Wardrobe
  6. Friends visiting
- **Preview video:** opens on the letter, as in the trailer plan.

## 16. Player support

- In-game "Help" opens a short FAQ and a contact form that sends the player id, game version and device automatically.
- The admin panel (shared platform) can look up a player, see their purchases and grant compensation.
- Target: answer within 48 hours, in Arabic.

## 17. Metrics to watch from day one

These are the soft-launch gates in `PLAN.md`, plus:
- tutorial completion by step
- first-session length
- share of players who reach the first memory
- day-1 return by country
- energy-empty moments per session
- first purchase timing
- rewarded ads per daily player
- crash-free users (target 99.5%+)
