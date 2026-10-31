// Chapter 2: خطط خالتو · Khalto's plans
// Every knot here was drafted by Claude Code from STORY_BIBLE.md sections 5, 6 and 8, and waits for the
// Amman dialect writer (CLAUDE.md, writing style). No letter in this chapter.
// The speaker is the first tag on a line; an optional second tag is the expression. [[word]] marks a tap-to-explain
// word; its meaning is in the Glossary table.
// chapter2_en.ink (English) and chapter2_ar.ink (neutral-Arabic subtitles) follow this file line for line
// (the parity test in Story/Tests checks it).

// The saved choice for a knot ("" before it's made), bound by the game (STORY_BIBLE.md section 8).
EXTERNAL chose(knot)

// Task 1, new curtains: Samira inspects the café like a mother-in-law.
=== curtains ===
يا أهلا يا أهلا! وحياة ولادي، ستايرك أحلى من ستايري. #samira #smile
بس الكراسي مش على خط واحد، والفناجين ناشفة، وإنتِ… وين الكحل يا بنت؟ #samira
خالتو سميرة، إنتِ جاية تشربي قهوة ولا تعملي تفتيش؟ #salma
الإتنين يا روحي. أنا عمري ما بعمل إشي واحد بس. #samira #smile
-> END

// Task 2, a glass case for sweets: the groom "just happens" to be coming.
=== sweets_case ===
حلوة [[الفترينة]]! بتلبق للكنافة… وللضيوف المهمّين كمان. #samira
ضيوف مهمّين؟ #salma #thinking
هاني، ابن جارتنا، الصيدلاني. صدفة يعني، جاي بكرا يشرب قهوة. #samira #smile
خالتو، أنا اشتغلت سبع سنين بالماركتنغ… قصدي بالتسويق. بعرف الصدفة المخطّطة لما بشوفها. #salma
وحياة ولادي صدفة! وإذا مش صدفة، بتصير صدفة بكرا الساعة خمسة. #samira
بكرا الساعة خمسة… بجيب كرسيّي وبقعد أتفرّج. #abu_fadi #smile
-> END

// Task 3, basil on the windowsill: the groom visits; awkward, kind, no spark.
// The choice c2_groom (STORY_BIBLE.md section 8). The game saves it under this knot's name, so the callback in
// chapter 10 reads chose("basil_pots"): "polite" or "honest".
=== basil_pots ===
مساء الخير… أنا هاني. خالتك سميرة قالت إنه… يعني… [[الحبق]] حلو عالشبّاك. #hani
ستّي كانت تزرعه بنفس المحل. بتحب الحبق؟ #salma #smile
بحبّه. وبحب إنه القهوة قريبة من الصيدلية… يعني، القهوة حلوة كمان. #hani
* [«تشرّفنا يا هاني. القهوة مفتوحة إلك أي وقت.» #polite]
    تسلمي. أكيد بجي… قهوتكم أطيب من قهوة المناوبات. #hani #smile
* [«بصراحة يا هاني؟ أنا مش جاهزة لعريس هسّا. القهوة هي كل إشي عندي.» #honest]
    الحمد لله… قصدي، بفهم عليكِ. وأنا كمان جيت كرمال خالتك. #hani #smile
- -> END

// Task 4, old photos on the wall: Samira finds herself in a 1974 photo as a child.
=== wall_photos ===
لحظة لحظة… هاي أنا! البنت أم الجدايل، أنا! #samira #surprised
وهاي إمّي الله يرحمها، وهاي ستّك ماسكة إيدي. سنة 74. #samira
يا الله يا خالتو، كنتِ زغيرة… وساكتة! #salma #smile
ساكتة لأنه ستّك حطّتلي معمول [[بتمّي]]. هيك كانت تسكّتني. #samira #smile
-> END

// Task 5, embroidered tablecloths: the groom asks about the night hours, and Samira misreads it.
=== tablecloths ===
سلمى، سؤال… القهوة بتضل فاتحة لآخر الليل؟ أنا مناوب بالصيدلية لحد الفجر. #hani
سمعتي؟ بدّه ييجي بالليل يشوفك! هاد عريس جدّي. #samira #smile
لا، لا… قصدي القهوة. بعد الساعة تنتين ما في ولا محل بيعمل فنجان. #hani #surprised
بتعرف؟ إنت تاني واحد بيسألني هالأسبوع. في دكتورة من المستشفى سألت نفس السؤال. #salma
دكتورة؟ انسي الدكتورة! ركّزي يا سلمى، ركّزي. #samira
-> END

// Task 6, an Eid corner with ma'amoul moulds: Samira remembers making ma'amoul here. Then memory 2.
=== eid_corner ===
قوالب المعمول! هاد المدوّر للتمر، وهاد الطويل للجوز، وهاد أبو قبّة للفستق. #samira #surprised
آخر عيد عملنا فيه معمول هون، كنتِ إنتِ ولانا قدّ هيك، تتخانقوا عالقوالب. #samira
بتذكّر الطحين… وبتذكّر إنه لانا كانت تسرق الجوز من الصينية. #salma #smile
وإنتِ كنتِ تسرقي التمر. طالعين لخالتكم سميرة. #samira #smile
-> END

// Memory 2 (rhythm, STORY_BIBLE.md section 6): Eid ma'amoul with the neighbours in 1974, little Samira and her
// mother in the café. The player shapes ma'amoul in time with the song.

=== m2_lead ===
هاد قالب إمّي. كانت تدقّه عالطاولة على الغنّية، دقّة ورا دقّة، ولا مرة غلطت. #samira
وأنا كنت أقعد تحت الطاولة، أعدّ الدقّات… وآكل. #samira #smile
-> END

=== m2_intro ===
يلّا يا جارات، بكرا العيد! على الغنّية: دقّة بالقالب، وقلبة عالصينية. #samira_mother_1974
واللي بتضيّع الدقّة، بتعمل القهوة للحارة كلها! #teta_young #smile
سميرة يا ماما، الفستق للمعمول، مش لتمّك! #samira_mother_1974
-> END

=== m2_perfect ===
شوفي! مصفوفين متل حبّات المسبحة. هيك المعمول، ولا بلاش. #teta_young #smile
-> END

=== m2_ok ===
مش مهم الشكل، الطعمة وحدة… اسألي سميرة، أكلت تلاتة. #samira_mother_1974 #smile
-> END

=== m2_outro ===
إمتى بدّك ولاد يعبّوا هالقهوة يا أم خليل؟ #samira_mother_1974
لما الله يريد. ولحدّ هداك اليوم، سميرة بنتي. #teta_young
سميرة تربّت نصّ عمرها بالقهوة. وكل عيد، كان قالبها يستنّاها على الطاولة. #sys
-> END

=== m2_return ===
شوفيني، عم ببكي على معمول! هاد الطحين فات بعيوني، مش إشي تاني. #samira #sad
هالعيد بنعمل المعمول هون يا خالتو. سوا. #salma #smile
اتفقنا. وأنا بجيب القوالب… والعريس. #samira #smile
-> END
