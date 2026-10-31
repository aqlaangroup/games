// Chapter 5: قهوتين · Two coffees
// Written by Claude Code from STORY_BIBLE.md sections 5–8; every line waits for the Amman dialect writer and the
// Najdi writer (CLAUDE.md, writing style). The cousins serve Ammani and Saudi coffee and work together for the first
// time: Salma speaks Amman, Lana Najdi, Nawal Amman with Najdi words, the guest from Jeddah light Hijazi.
// The speaker is the first tag on a line, the expression the second. [[word]] marks a tap-to-explain word; its meaning
// is in the Glossary table. chapter5_en.ink (English) and chapter5_ar.ink (neutral-Arabic subtitles) follow this file
// line for line (the parity test in Story/Tests checks it).

// The saved choice for a knot ("" before it's made), bound by the game (STORY_BIBLE.md section 8).
EXTERNAL chose(knot)

// Task 1: the Saudi coffee station. Lana teaches Salma Saudi coffee; Salma laughs at the colour.
=== saudi_station ===
هذي القهوة السعودية: بن أشقر، وهيل، وزعفران. شوفي وتعلّمي. #lana
لانا… هاي قهوة ولا شاي بحليب؟ لونها أصفر! #salma #smile
[[وش]] فيه لونها؟ قهوتكم سودا مثل الفحم، وأنا ما قلت شي. #lana #angry
…طيّبة. بس إيّاكِ تقوليلي «قلتلك». #salma #thinking
ما قلت شي. وجهي هو اللي قال. #lana #smile
-> END

// Task 2 (styles): the majlis bench. The guest from Jeddah sits in it and feels at home. She is this season's
// tourist (STORY_BIBLE.md section 9); her look at the window is for her level-6 scene.
=== majlis_bench ===
يا سلام… كأنّي في بيت أمّي في جدّة. كدا كانت الجلسة بالضبط. #tourist #smile
حيّاك الله يا خالة. أصبّ لك قهوة سعودية؟ #lana
إيوه يا بنتي، وشويّة تمر. عمّان دايمًا تعرف كيف تخلّي الواحد في بيته. #tourist
والشبّاك هذا… لسّا زي ما هو. #tourist #thinking
-> END

// Task 3: the menu board with both coffees. Which goes first is the choice c5_menu (ammani / saudi). The game
// saves a choice under its knot, so chapter 6's opening night reads it with chose("menu_board").
=== menu_board ===
طيب، ميتنغ سريع… يعني، قعدة. شو بنكتب أول عاللوح؟ #salma #thinking
السعودية. حتى بالأبجدية، السين قبل العين. #lana #smile
* [«العمّانية أول. إحنا بعمّان.» #ammani]
    زين، العمّانية فوق… بس السعودية بخط أكبر. #lana
* [«ماشي، السعودية أول. الضيف أول.» #saudi]
    ضيف؟ أنا نص المالكة يا بنت خالتي. بس زين، أقبل. #lana #smile
-
يا بنات، اللوح ما بيشرب. المهم شو بالفنجان. #abu_fadi
-> END

// Task 4: a bowl of dates on every table. Nawal messages Lana and asks about the café.
=== dates_bowl ===
أمّي أرسلت لي. #lana #surprised
طمّنيني، القهوة زينة؟ لسّا الدلال على الرف؟ ولا تقولي لحدا إنّي سألت. #nawal
عشرين سنة ما سألت عن شي هنا… وتقول لا تقولين لأحد. وأنا قلت لك. #lana #sad
قوليلها إنهم كلهم على الرف. والصغيرة كمان رجعت. #salma #smile
وأقول لها إنّك حطّيتي تمر على كل طاولة، على طريقتنا. #lana #smile
-> END

// Task 5: cardamom and saffron jars. Salma minds her mother's silence less than she misses her voice.
=== cardamom_jars ===
مرطبان للهيل، ومرطبان للزعفران… ستّي كانت تكتب على كل شي؟ #lana
على كل شي، إلا على اللي بقلبها. #salma #sad
بتعرفي؟ ماما كمان صارت هيك. سكوتها تعوّدت عليه… صوتها هو اللي ناقصني. #salma #sad
أمّي نفس الشي. تكلّمني كل يوم، وما تقول شي. #lana #sad
يلّا نعبّي المرطبانات. على الأقل هدول بيتعبّوا. #salma
-> END

// Task 6: two matching cups on the counter. They stop splitting the ledger and find the two-city coffee in
// Teta's book (recipe two_cities; Salma reads its clue). Memory 5 (find) plays after this scene, then letter 3.
=== two_cups ===
فنجانين متل بعض عالكاونتر. واحد إلي، وواحد إلك. #salma #smile
وبعدين؟ نقسم الفناجين بالنص مثل الدفتر؟ #lana
لا. بدّي نشطب الخط اللي بنص الدفتر. دفتر واحد، لقهوة وحدة. #salma
زين. بس أنا أمسك الحساب، خطّك ما ينقرا. #lana #smile
اتفقنا. وشوفي شو لقيت بدفتر الوصفات: «لعرس نوال، 1997. قهوتين بفنجان واحد.» #salma
عرس أمّي؟ ستّي سوّت لها قهوة من مدينتين… #lana #surprised
-> END

// Memory 5 (find): the 1974 kitchen and the first coffee roast. Teta keeps two jars: one for the house, one "for
// later". Letter 3 waits in the second one.

=== m5_lead ===
ستّكم كانت تحمّص البن بإيدها، بالمطبخ اللي ورا. ريحته توصل لآخر الحارة. #abu_fadi
وكان عندها مرطبانين. واحد تفتحه كل يوم… والتاني عمري ما شفتها فتحته. #abu_fadi #thinking
-> END

// 1974: older Amman speech, no modern loanwords.
=== m5_intro ===
يا أم سميرة، أول تحميصة بقهوتي! بس المطبخ لسّا مقلوب فوق تحت. #teta_young #smile
ولا يهمّك، بدوّر معك. شو بدّك [[هسّع]]؟ #samira_mother_1974
كل إشي! بسرعة يا روحي، قبل ما يحترق البن. #teta_young
-> END

=== m5_perfect ===
يسلموا إيديكِ! ولا حبّة احترقت. #teta_young #smile
-> END

=== m5_ok ===
احترق شوي؟ ولا يهمّك. التحميصة متل التربية: نار هادية وصبر. #teta_young
-> END

=== m5_outro ===
ليش مرطبانين يا أم خليل؟ البن واحد. #samira_mother_1974
هاد للبيت، كل يوم. وهاد بكتب عليه: «لبعدين». #teta_young
ولإيمتى هالبعدين؟ #samira_mother_1974 #thinking
لليوم اللي ما بينحكى فيه الحكي. هداك اليوم، بيحكي الفنجان. #teta_young
-> END

// The game opens every letter from the counter drawer, so Lana puts it there.
=== m5_return ===
لقيته! بآخر الرف: «لبعدين»، بخطّ ستّي. #salma #surprised
ما فيه قهوة… فيه ورقة مطويّة، ومكتوب عليها اسمي. #lana #surprised
ضلّت لانا ماسكة الورقة شوي، وبعدين حطّتها بدرج الكاونتر، جنب رسالة ستّي الأولى. #sys
-> END

// Letter 3 (STORY_BIBLE.md section 7), from the "for later" jar: to Lana, about Nawal as a girl. One tag per
// line style, like letter_1.
=== letter_3 ===
لانا يا روحي، يا بنت نوال، #letter
أمّك وهي صغيرة كانت أهدى وحدة بالبيت. تقعد ورا الكاونتر، تسمع كل شي، وما تحكي. #letter
كانت تحفظ السرّ متل ما بتحفظ اسمها. ولا مرّة خانت حدا. #letter
بس أمّك بتحفظ الوعد أكتر من اللازم يا روحي. #letter
والوعد اللي عليها، أنا اللي… #letter_faded
إذا سكتت، اقعدي جنبها. السكوت عند نوال محبّة، مش زعل. #letter
— ستّك #letter_signature
-> END

// Teta's voice note after letter 3. Needs the voice actor (missing/14).
=== voice_3 ===
لانا يا روحي… إذا لقيتي رسالتي، لا تسألي أمّك شو صار. اسأليها كيف حالها، وهي بتحكيلك لحالها. #teta
-> END

// The message after letter 3: Lana writes to Salma that night.
=== notify_3 ===
سألت أمّي كيف حالها، مثل ما قالت ستّي. سكتت شوي… وبعدين سألت عن خالتي رنا. #lana
-> END
