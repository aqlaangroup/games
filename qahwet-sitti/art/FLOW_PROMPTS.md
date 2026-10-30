# Qahwet Sitti: art and animation prompt pack (Google Flow)

Use this in Google Flow: Nano Banana for images, Veo for video. Make the images first, then use them as **ingredients** (references) for every video, so the characters look the same in every shot.

Save every result into `qahwet-sitti/art/` with the file name given, and I'll put them into the game demo.

---

## 1. The style (paste this at the start of every prompt)

> **STYLE:** stylized 3D animated film look, soft and premium, like a modern feature animation. Matte, slightly clay-like skin and fabric, gentle subsurface glow, big expressive eyes with real emotion, soft rounded shapes. Palette: blush pink, lilac, sage green, warm cream, soft gold accents. Warm natural window light with soft shadows, shallow depth of field. Elegant, feminine and calm. Modest clothing. No text, no logos, no watermark.

For the **1974 memory** scenes, add this to the style:

> **ERA:** Amman, 1974. Same 3D style, but warm faded film colours, gentle grain, amber and rose tones, slightly softer focus at the edges, like a cherished memory.

---

## 2. Characters (images, Nano Banana)

Make each one as a **character sheet**: front view, three-quarter view and a full-body pose, on a plain light background (#FBF5F3), 16:9, 2K. Then make a single front portrait of each on a plain background for the game.

| File | Prompt (after STYLE) |
|---|---|
| `salma_sheet.png` | Character sheet of **Salma**, a 29-year-old Jordanian woman from Amman. Warm light-olive skin, dark brown hair in a loose low bun with a few strands falling, soft brown eyes, small gold earrings. Wears a blush-pink linen shirt with sleeves rolled up, a cream apron, and wide sage trousers. Kind, capable, a little tired, with a hopeful smile. |
| `lana_sheet.png` | Character sheet of **Lana**, a 28-year-old Saudi woman from Riyadh. Wears a soft lilac hijab wrapped neatly, a long flowing open abaya in deep plum over a cream outfit, and minimal gold jewellery. Sharp confident eyes, elegant posture, a teasing half-smile. Modern and stylish. |
| `teta_1974_sheet.png` | Character sheet of **young Teta (Im Khalil) in 1974**, a 20-year-old Jordanian newlywed. Light floral headscarf in rose tied loosely at the back, a long lilac dress with small white flowers and a cream cardigan. Warm round face, bright laughing eyes. Holds a small brass dallah. ERA style. |
| `teta_old_portrait.png` | Framed portrait photo of **Teta in her 70s**, the same woman as young Teta, now older. White headscarf, gentle wrinkles, a loving smile. Soft and slightly faded, as if it hangs on the café wall. |
| `abufadi_sheet.png` | Character sheet of **Abu Fadi**, a 75-year-old Jordanian man. Silver hair combed back, neat grey moustache, warm wrinkled eyes. Sage-green knitted cardigan over a white shirt, brown trousers, prayer beads in his hand. Gentle and nostalgic. |
| `fadi_1974_sheet.png` | Character sheet of **young Fadi in 1974**, age 25: the same man 50 years younger. Dark wavy hair, black moustache, light blue shirt with a 70s collar, slim trousers. Shy and charming. ERA style. |
| `samira_sheet.png` | Character sheet of **Khalto Samira**, a 55-year-old Jordanian aunt. Plum headscarf, a patterned long dress, gold bracelets, a big handbag. Expressive eyebrows, always mid-sentence, funny and warm. |
| `karim_sheet.png` | Character sheet of **Karim**, a 31-year-old Jordanian carpenter. Short dark hair, light beard, kind eyes. A canvas work apron over a rolled-sleeve olive shirt, a pencil behind his ear. Quiet and gentle. |

**Consistency tip:** once a sheet looks right, always attach it as a reference and say "the same character as the reference" in every later prompt.

---

## 3. Places (images, Nano Banana)

| File | Size | Prompt (after STYLE) |
|---|---|---|
| `cafe_now_closed.png` | 9:16, 2K | Interior of a small old café in Jabal Al-Weibdeh, Amman, closed for months. Arched window with morning light through dust, chairs stacked in a corner, a crooked hand-painted sign reading in Arabic «قهوة ستّي», a white counter with a wooden drawer, brass dallahs on a shelf, patterned cement tile floor in blush and sage. Empty, quiet, hopeful. |
| `cafe_now_open.png` | 9:16, 2K | The same café, restored and open: the sign straightened, plants, soft string lights, chairs at round tables, coffee steam, warm morning light. Same layout as `cafe_now_closed.png` (attach it as a reference). |
| `cafe_1974.png` | 9:16, 2K | The same café on its opening day in 1974. Fresh paint, new brass dallahs, a radio on the counter, a calendar on the wall, flowers in a jar. Same layout (attach `cafe_now_closed.png` as a reference). ERA style. |
| `street_weibdeh.png` | 16:9, 2K | Sloping stone street in Jabal Al-Weibdeh, Amman, at golden hour. White limestone houses, jasmine climbing a wall, the café's arched window glowing. |

The game screen draws characters over these backgrounds, so keep the floor and counter clear in the middle third of the image.

---

## 4. Game items (images)

One image per item, centred, plain background (#FBF5F3), 1:1, 1K. Use the STYLE line, then: *"a single game icon of [item], soft 3D, centred, gentle shadow beneath, nothing else in the frame."*

| File | Item |
|---|---|
| `item_bean_green.png` | one green raw coffee bean |
| `item_bean_roasted.png` | one glossy roasted coffee bean |
| `item_coffee_cardamom.png` | a small blush ceramic bowl of ground coffee with green cardamom pods |
| `item_cup.png` | a small handleless Arabic coffee cup (finjan) with a rose band, wisps of steam |
| `item_dallah.png` | a polished brass Arabic coffee pot (dallah) |
| `item_tin.png` | a vintage blush-pink coffee tin with a gold lid |

---

## 5. Cutscene videos (Veo)

Format: **9:16**, 8 seconds, with sound. Attach the character sheet and place image as ingredients for every shot.

**Voices:** Veo can speak Arabic, but its Jordanian accent may not be accurate. If the accent sounds wrong, make the clip without dialogue ("ambient sound only") and we'll add a real voice actor later.

| File | Ingredients | Prompt (after STYLE) |
|---|---|---|
| `scene1_abufadi_enters.mp4` | abufadi, salma, cafe_now_closed | Morning. The café door opens and a small brass bell rings. Abu Fadi steps in slowly, looks around with wet, nostalgic eyes, touches the back of a chair, and sits at the round table by the window. Salma looks up from the counter, surprised and hopeful. Slow push-in camera. Sound: door bell, soft street sounds, a quiet oud note. |
| `scene2_first_sip.mp4` | abufadi, salma, cafe_now_closed | Close-up. Salma places a small finjan of Arabic coffee in front of Abu Fadi. He lifts it with both hands, smells it, sips, and closes his eyes. A tear forms. He smiles and whispers in Jordanian Arabic: «يا الله… هاي قهوتها». Camera holds on his face, shallow focus. Sound: cup on saucer, a soft breath, gentle music-box notes. |
| `scene3_memory_1974.mp4` | teta_1974, fadi_1974, cafe_1974 | ERA style. The same café in 1974. Young Teta laughs as she pours coffee from a brass dallah into a finjan for young Fadi, who is shy and charmed. Sunlight, dust in the air, a radio playing softly. The camera slowly orbits the table. Sound: pouring coffee, laughter, an old radio song with no lyrics. |
| `scene4_hiding_letter.mp4` | teta_1974 (aged to about 50: add "the same woman at age 50"), cafe_1974 | ERA style, evening. Teta, now older, alone in the closed café, folds a handwritten letter, kisses it, and slides it under the counter drawer's lining. She closes the drawer and rests her hand on it, eyes full of regret. Low warm lamp light. Sound: paper, a drawer sliding shut, a slow single oud phrase. |
| `scene5_letter_found.mp4` | salma, cafe_now_closed | Present day. Salma pulls open the stuck counter drawer, lifts the lining and finds an old envelope with Arabic handwriting: «لبناتي». She opens it with trembling hands; her eyes fill with tears. Macro shots of the paper, then her face. Sound: drawer creak, paper, a quiet heartbeat-like drum. |
| `scene6_lana_arrives.mp4` | lana, salma, cafe_now_open | Late afternoon. The door bell rings. Lana walks in, takes off her sunglasses and looks around. She and Salma face each other across the café: a long, tense, funny pause, then Lana raises one eyebrow and smiles. Sound: door bell, a playful plucked qanun note. |

---

## 6. The viral trailer (Veo, multi-shot)

**Format:** 9:16 and 30 seconds, built from 5 shots. Use all character sheets as ingredients.

**Hook in the first 2 seconds:** start on the letter, not on gameplay. Gossip Harbor's best ads also open on drama before showing gameplay.

1. **0–3s:** an extreme close-up of an old letter in Arabic handwriting, «سامحوني», with a tear dropping on the ink.
2. **3–9s:** Abu Fadi sips coffee and whispers «هاي قهوتها».
3. **9–16s:** a match cut through the coffee cup into 1974, where young Teta laughs while pouring.
4. **16–23s:** present day. Two cousins, Salma and Lana, stare at each other across the café; their mothers' photo hangs between them, torn in half.
5. **23–30s:** the café lights come on one by one. The title «قهوة ستّي» appears in soft gold handwriting, then the line «كل فنجان… بيكشف سرّ» ("Every cup reveals a secret").

**Sound:** a music box in Maqam Hijaz that grows into strings, plus coffee pouring.

**Make 3 versions** to test which hook works best:
- Opening on the letter.
- Opening on Abu Fadi's tear.
- Opening on the two cousins.

---

## 7. What happens after you generate

- **In the demo:** I'll replace the drawn characters with your character portraits, give them gentle idle motion (breathing, blinking, sway), use the place images as backgrounds, and play the cutscene videos at the key story moments.
- **In the real Unity game:** the characters need proper 2D animation rigs (Spine or Live2D) made by a 2D animator from these same images, for idle, talking, walking, sipping and reacting. AI video is ideal for cutscenes and ads, but gameplay characters must react live, so they need rigs.
