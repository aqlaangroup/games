// Chapter 3: لانا من الرياض · Lana from Riyadh
// [draft] Every knot here was drafted by Claude Code from STORY_BIBLE.md sections 5, 6 and 7, and waits for the
// native writers (CLAUDE.md, writing style): Lana speaks Najdi from her first line (وش، زين، أبغى، الحين، كذا، لين,
// and she keeps the -ين ending: تضحكين); Salma, Abu Fadi, Karim, Teta and Rana speak Amman dialect.
// The speaker is the first tag on a line; an optional second tag is the expression. [[word]] marks a tap-to-explain
// word; its meaning is in the Glossary table.
// chapter3_en.ink (English) and chapter3_ar.ink (neutral-Arabic subtitles) follow this file line for line
// (the parity test in Story/Tests checks it).

// The saved choice for a knot ("" before it's made), bound by the game (STORY_BIBLE.md section 8).
EXTERNAL chose(knot)

// [draft] Task 1, a corner for Lana's things: Lana arrives, cool and businesslike.
=== lana_corner ===
السلام عليكم. أنا لانا، بنت نوال. [[توّني]] جاية من الفندق، ومعي الأوراق. #lana #neutral
لانا؟! آخر مرة شفتك كان عمرك تمان سنين… جهّزتلك زاوية لأغراضك. #salma #surprised
زين، مشكورة. وأبغى نسخة من كل الفواتير، عشان نتفاهم على البيع. #lana
البيع؟ هاد مش ميتنغ يا لانا… لسّا ما شربتي ولا فنجان! #salma
الشغل قبل القهوة. يالله، وين الزاوية؟ #lana
-> END

// [draft] Task 2, a tea shelf (tea joins the kitchen): Lana makes tea "the right way"; Salma disagrees.
=== tea_shelf ===
لا لا، كذا غلط. الشاهي يبغى له نعناع، ولازم [[يتخدّر]] على مهله، والسكر كل واحد يحطه بنفسه. #lana
نعنع؟ إحنا بنحط [[ميرمية]]، والسكر بالإبريق من الأول. هيك كانت ستّي تعمله. #salma
ميرمية؟ في الشاهي؟ وش هالسالفة؟ #lana #surprised
شاهي؟ اسمه شاي يا لانا. شاي. #salma #smile
شاهي. وخلاص. #lana
-> END

// [draft] Task 3, one ledger for two owners: they split the accounts down the middle, literally.
=== shared_ledger ===
نقسم الدفتر بالنص: خط في نص كل صفحة. اليمين لك، واليسار لي. #lana
ليش أنا اليمين؟ #salma #thinking
العربي يبدا من اليمين، وإنتي الأكبر. شفتي؟ ما ظلمتك. #lana
يعني إحنا [[هسّا]] شريكات ولا جارات؟ #salma
جارات. [[لين]] نبيع. #lana #neutral
-> END

// [draft] Task 4, lanterns: Abu Fadi calls them by their childhood nicknames.
=== lanterns ===
يا الله… حبّة الهيل وحبّة السكّر، تحت فانوس واحد! #abu_fadi #smile
وش قلت يا عمّي؟ #lana #surprised
هيك كانت ستّكم تسمّيكم. سلمى حبّة الهيل، ريحتها بتسبقها. ولانا حبّة السكّر، بتذوب من أول دمعة. #abu_fadi
لانا؟ حبّة سكّر؟ #salma #smile
لا تضحكين. كنت سكّر يا عمّي، والحين صرت قهوة سادة. #lana
-> END

// [draft] Task 5, the back room's door unstuck: Karim opens the door Teta kept locked.
=== back_room_door ===
هالباب زعلان. الخشب نفخ، وصارله سنين ما حدا فتحه. #karim
ليش كانت جدّتي تقفله؟ #lana #thinking
الحجّة قالتلي مرة: هالغرفة إلها وقت. شكله إجا وقتها. #karim
على مهلك يا حبيبي… هيك. انفتح. #karim
ريحتها هيل وورق قديم… ريحة ستّي. #salma #sad
-> END

// [draft] Task 6, Teta's back room: Lana finds a drawing the two of them made in 2005. Then memory 3 and letter 2.
=== back_room ===
سلمى، تعالي. شوفي وش لقيت… رسمتنا! #lana #surprised
عيد 2005! ستّي ورا الكاونتر، وإحنا التنتين جنبها، وكل وحدة كاتبة اسمها. #salma #smile
وإنتي لوّنتي برّا الخطوط، وأنا زعلت ثلاث ساعات. #lana
ساعتين. وبعدين ستّي قسمتلنا قطعة كنافة بالنص. #salma
بالنص… مثل الدفتر. #lana #sad
-> END

// Memory 3 (find, STORY_BIBLE.md section 6): Teta's back room on a busy evening in 1974. The player finds what the
// café needs (chapters.json objects); the tin box and its key lead to letter 2.

// [draft]
=== m3_lead ===
هالغرفة… بعزّ الزحمة كانت تفوت عليها، وتطلع شايلة كل إشي بإيديها التنتين. #abu_fadi
وكل ليلة، قبل ما تسكّر، كانت تقفل علبة التنك. #abu_fadi
-> END

// [draft]
=== m3_intro ===
يا أم خليل! القهوة فايضة ناس، والشباب بدّهم عشر فناجين. #fadi_young
جاية يا أستاذ فادي! الفناجين، والصينية، والدلّة الكبيرة… وين حطّيتهم؟ #teta_young
يلّا يا إيدي، دوّري. الضيف ما بيستنّى، والهيل ما بيطحن حاله. #teta_young #smile
-> END

// [draft]
=== m3_perfect ===
ولا دقيقة راحت! هيك الشغل، بيبيّض الوجه. #teta_young #smile
-> END

// [draft]
=== m3_ok ===
تأخّرنا شوي… بس الضيف اللي بيستنّى، بتطلعله القهوة أطيب. #teta_young
-> END

// [draft]
=== m3_outro ===
شو بتخبّي بعلبة التنك يا أم خليل؟ ذهب؟ #fadi_young #smile
أول قرش دخل القهوة. هاد ما بينصرف، ولا بأصعب يوم. #teta_young
قفلت العلبة، وخبّت المفتاح ورا الساعة. #sys
-> END

// [draft]
=== m3_return ===
الساعة… كل ليلة، قبل ما تسكّر، كانت تمدّ إيدها وراها. #abu_fadi
فيه مفتاح! وفي العلبة قرش قديم… وظرف مكتوب عليه: سلمى. #lana #surprised
إلي أنا؟ #salma #surprised
-> END

// Letter 2 (STORY_BIBLE.md section 7), in the tin box from the back room, to Salma: Rana as a girl.

// [draft]
=== letter_2 ===
لسلمى، حبّة الهيل تبعتي، #letter
إمّك وهي بنت سبع سنين طلعت عالسطح تجيب طابة الولاد، ونزلت بركبة مجروحة، وما بكت. #letter
وكانت عنيدة متلي. إذا قالت لأ، الدنيا كلها ما بتقلبها. #letter
لما الكل راح، هي ضلّت. ضلّت بعمّان، وضلّت جنبي، حتى لما بطّلت تفوت من باب القهوة. #letter
وهاد مش ذنبها يا روحي، هاد ذنبي أنا. #letter
هي بتعرف نصّ الحكاية بس… #letter_faded
ما تزعلي من عنادها. العناد عندها محبّة ما لقيت طريقها. #letter
— ستّك #letter_signature
-> END

// [draft] Teta's voice note after the letter (needs the voice actor, missing/14).
=== voice_2 ===
سلمى يا روحي… إمّك راسها يابس متل راسي. ما تستنّيها تيجي، روحي إنتِ لعندها، وخدي معك فنجان. #teta
-> END

// [draft] The message after the letter: Rana writes to Salma. She says "your Teta", never "my mother" (section 10).
=== notify_2 ===
سلمى، سميرة قالتلي إنه بنت خالتك وصلت. انتبهي على حالك… وما تتأخري بقهوة ستّك. #rana
-> END
