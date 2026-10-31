// Chapter 9: الصفحة الناقصة · The missing page
// Every knot here was drafted by Claude Code from STORY_BIBLE.md (sections 3–8) and waits for the Amman and
// Najdi dialect writers (CLAUDE.md, writing style). The speaker is the first tag on a line; the second is the expression.
// [[word]] marks a tap-to-explain word; its meaning is in the Glossary table.
// chapter9_en.ink (English) and chapter9_ar.ink (neutral-Arabic subtitles) follow this file line for line
// (the parity test in Story/Tests checks it). Change this file first, then those two.
// The page: Teta tore it out of the book in 2006, wrote the truth on its back and gave it to Abu Fadi whole. In 2019
// she paid the last of the money back, added one line and tore the page into seven strips (window_seat, letter_5).

// The saved choice for a knot ("" before it's made), bound by the game (STORY_BIBLE.md section 8).
EXTERNAL chose(knot)

=== recipe_shelf ===
حطّيت الدفتر عالرف، ورجعت شفت مكان الصفحة المقطوعة. «كنافة أم خليل»… وبعدها ولا إشي. #salma #thinking
شوفي القطع: مستقيم، [[مرّة]] مرتّب. هذي ما انقطعت بالغلط. #lana
يعني ستّي قطعتها بإيدها… ليش؟ #salma
اللي يقطع صفحة كذا ما يرميها. يخبّيها عند أحد يثق فيه. #lana
-> END

=== knafeh_station ===
أول صينية من محطّة الكنافة: الجبنة مالحة، وتحتها محروق. #sys
لا، لا، كمّلوا. أنا بس بتفرّج. #abu_fadi #smile
عمّي، وش الغلط؟ قل بس. #lana
أنا وعدت أشرب قهوتي وأسكت. بس الجبنة… بدها تنام بالمي من الليل. #abu_fadi
يعني بنعيدها بكرا. #salma #smile
-> END

// "When both girls' girls are under one roof" (STORY_BIBLE.md section 3): the roof mended in chapter 8, and
// the table where they decided together.
=== abu_fadi_chair ===
كرسيك يا أبو فادي. شدّيت رجليه، وما غيّرت ولا خشبة. الكرسي بيعرف صاحبه. #karim
ستّكم قالتلي: لمّا بنات البنات يصيروا تحت سقف واحد… #abu_fadi #thinking
وش اللي يصير؟ #lana #surprised
السقف انصلح، وإنتو قعدتوا على طاولة وحدة وقرّرتوا سوا. صار وقتها. #abu_fadi
وقت شو يا عمّو؟ #salma
بكرا الصبح. جيبوا فنجانين، وتعالوا عالشبّاك. #abu_fadi
-> END

=== window_seat ===
أبو فادي طلّع من جيبته ظرف قديم، وحطّه بيناتهم. #sys
سنة 2006 أعطتني إياها كاملة. وسنة 2019 أخدتها ليلة وحدة، ورجّعتها هيك… قطع. #abu_fadi
ليش قطّعتها يا عمّو؟ #salma
قالت: اللي بدّه يقرأها، لازم يقعد ويلمّها بإيده. متل القهوة. #abu_fadi
وإنت؟ عشرين سنة وما قريتها؟ #lana #sad
وعدتها يا بنتي. #abu_fadi
-> END

=== book_case ===
الدفاتر بالخزانة، والصفحة على الطاولة قطع قطع. لانا اتّصلت بأمّها. #sys
يمّه، ردّي… أبغى أسألك عن شي واحد بس. #lana
رنّ التلفون لآخره. وما ردّت. #sys
أمّي كمان… بتعرف إيمتى بدّي أسأل، وما بتردّ. #salma #sad
طيب. نلمّها إحنا، ونوديها لهم. #lana
-> END

// Faris's apology, with the callback for c8_offer (chosen in chapter 8's faris_table; STORY_BIBLE.md section 8).
// He talks plainer now, without his English work words (voice sheet). He withdraws before he knows the page's back.
=== the_page ===
قبل ما يلمّوا الصفحة، دخل فارس. من غير بدلة، ومن غير ابتسامة الشغل. #sys
{
- chose("faris_table") == "hear":
    إنتو سمعتوني للآخر، وأنا ما حكيتلكم كل إشي. سامحوني. #faris #sad
- else:
    طلبتوا منّي أطلع، وكان معكم حق. سامحوني. #faris #sad
}
سألت أبوي عن الاتفاق. قال جملة وحدة: «ما فاصلت. ولا قرش.» #faris
ستّي؟ ما فاصلت؟ #salma #surprised
العرض مسحوب. شو ما كان مكتوب بالورق. #faris
يلّا يا سلمى. نلمّ الصفحة، ونحطّها تحت القزاز. #lana
-> END

// Memory 9 (reassemble): the torn knafeh page. Its front is the recipe, from the day Teta invented it in 1974 with
// young Fadi tasting; its back is letter_5 (STORY_BIBLE.md sections 6 and 7).

=== m9_lead ===
بتعرفوا مين أول واحد داق هالكنافة؟ #abu_fadi #smile
صيف 74. كنت قاعد عالشبّاك، والريحة طلعت للشارع… #abu_fadi
-> END

=== m9_intro ===
فادي! تعال دوق، وقلّي الحقيقة. مش مجاملة. #teta_young
كنافة؟ بالقهوة؟ من إيمتى يا ستّ أم خليل؟ #fadi_young #surprised
من اليوم. إذا عجبتك. #teta_young #smile
-> END

// The page's front, one strip per line in the right order (ReassembleGame shuffles them; balance.json
// levelB.memories.reassemble.strips is 7). No tap-to-explain marks: the strips are not tappable.
=== m9_page ===
كنافة أم خليل #letter
رطل جبنة نابلسية، تنام بالمي من الليل. #letter
الكنافة الناعمة بتنفرك بالسمنة، لحتى تصير متل الرمل. #letter
نصّها بالصينية، فوقها الجبنة، وفوقها النصّ التاني. #letter
على نار هادية، والصينية بتلفّ. الكنافة متل القهوة: بدها صبر. #letter
القطر بارد عالكنافة السخنة، وفيه حبّة هيل. هاد السرّ. #letter
— أم خليل، صيف 74. والشاهد: فادي، أكل صحنين. #letter_signature
-> END

=== m9_perfect ===
يا ستّ أم خليل… هاي مش كنافة. هاي عيد. #fadi_young #surprised
-> END

=== m9_ok ===
ناقصها شوي… بس بنوصل. #teta_young #thinking
-> END

=== m9_outro ===
اكتبيلي الوصفة يا ستّ أم خليل. بدّي أعملها لأمّي. #fadi_young
الوصفة بتنكتب بدفتري، وبتضل فيه. هاي لا بتنباع ولا بتنعطى. #teta_young #smile
وبعد اتنين وتلاتين سنة، انقطعت الصفحة من الدفتر. #sys
-> END

=== m9_return ===
سلمى… ورا الصفحة كتابة. بخطّ ستّي. #lana #surprised
هاي مش إلي أقرأها. أنا بشرب قهوتي برّا. #abu_fadi
حطّوا الصفحة عالكاونتر، جنب الدرج، وقعدوا يقرأوا ضهرها سوا. #sys
-> END

// Letter 5, the back of the page, to Rana and Nawal: the whole 2006 truth (STORY_BIBLE.md section 3), signed,
// with the line Teta added in 2019. The longest letter: check that it fits the paper at large text.
=== letter_5 ===
رنا ونوال، يا روحي، #letter
سنة 2006 غرقنا بالدين، وأبو فارس طلب كنافتي واسمها. #letter
قدّام الناس قلت لا، وبالسرّ بعته إياهم عشر سنين. واستحيت. #letter
نوال عملت الورق ووقّعت شاهدة، وأنا حلّفتها ما تحكي، ولا لأختها. #letter
يا رنا، نوال ما باعت إشي. أنا اللي قلتلك: اسألي أختك. #letter
قطعت الصفحة لحتى ما تنباع مرة تانية، وبرجّع المصاري قرش قرش. #letter
2019: رجّعت آخر قرش. الاتفاق خلص، وما إلهم بالقهوة ولا حجر. #letter
— أمّكم #letter_signature
-> END

// Teta's voice note after letter 5. Needs the voice actor (missing/14).
=== voice_5 ===
يا روحي… إذا قرأتوها، خدوها لأمّهاتكم. أنا ما قدرت. #teta
-> END

// Lana sent her mother a photo of the page; Nawal answers at last (she didn't in book_case).
=== notify_5 ===
وصلتني الصورة يا لانا. عشرين سنة وأنا ساكتة… الحين حجزت على عمّان. #nawal
-> END
