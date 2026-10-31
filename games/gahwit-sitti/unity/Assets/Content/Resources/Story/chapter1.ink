// Chapter 1: رجعة سلمى · Homecoming
// The intro and sign scenes come from prototype/index.html. Knots marked [draft] were drafted by
// Claude Code and wait for the Amman dialect writers (CLAUDE.md, writing style).
// The speaker is the first tag on a line. [[word]] marks a tap-to-explain word; its meaning is in the Glossary table.
// chapter1_en.ink (English) and chapter1_ar.ink (neutral-Arabic subtitles) follow this file line for line
// (missing/11; the parity test in Story/Tests checks it).

// The saved choice for a knot ("" before it's made), bound by the game (STORY_BIBLE.md section 8).
EXTERNAL chose(knot)

=== intro ===
هاي هي… قهوة ستّي. الآرمة مايلة، والكراسي مكوّمة [[هون]] بالزاوية. #salma
«يا سلمى يا روحي، القهوة مش حيطان، القهوة ناس. افتحيها، والناس بترجع.» #teta
-> END

// Tutorial step 3: the door bell rings and Abu Fadi walks in with the first order. The kitchen's instructions are
// UI text now (tutorial.* keys), not story lines.
=== abu_fadi_arrives ===
أهلين يا بنت خليل! والله إنك نسخة عن ستّك. بتعمليلي فنجان؟ من زمان ما شربت قهوة [[على أصولها]]. #abu_fadi
-> END

=== sign ===
شوفي الآرمة! كأنها رجعت سنة 74. ستّك كانت تقعد [[هون]] كل صبح وتسقي الحبق. #abu_fadi
عمّو أبو فادي، احكيلي عنها. أنا كنت بعيدة كتير. #salma
كانت تقول: الفنجان الأول للضيف، والتاني للحكي، والتالت للمحبة. تعالي كل يوم، وأنا بحكيلك. #abu_fadi
-> END

// [draft]
=== floor ===
شوف هالبلاط! ستّي كانت تقول إنه أقدم من الحارة كلها. #salma
كل بلاطة إلها حكاية. هون كسرت أول فنجان وأنا صغير، وستّك ما زعّلتني ولا بكلمة. #abu_fadi
-> END

// [draft]
=== chairs ===
هاي طاولتي. من ثلاثين سنة وأنا بقعد عليها، مقابل الشبّاك. #abu_fadi
خلص يا عمّو، من اليوم هاي الطاولة [[محجوزة]] إلك. #salma
-> END

// [draft]
=== dallahs ===
بتلمع كأنها جديدة… ستّي كانت تلمّعها كل جمعة. #salma
والدلّة الصغيرة؟ كانت إلها لحالها. ما حدا يصبّ منها غيرها. #abu_fadi
* [«احكيلي عنها يا عمّو.» #ask]
    في أشيا أحسن تعرفيها منها هي. دوّري بدفاترها. #abu_fadi
* [«بكرا بتحكيلي، صح؟» #tomorrow]
    بكرا… إذا طلع الفنجان [[مزبوط]]. #abu_fadi
- -> END

// [draft]
=== lights ===
ضوّت القهوة! حتى الجيران طلّوا من الشبابيك. #salma
القهوة اللي ضوّها مولّع ما بتضل فاضية. بكرا بتشوفي. #abu_fadi
-> END

// [draft]
=== counter ===
تفضّل يا عمّو أبو فادي. أول فنجان من الدلّة الصغيرة. #salma
هيك كانت تعملها… بالهيل، ومن غير استعجال. الله يرحمها. #abu_fadi
// [draft] The callback for the choice in dallahs (STORY_BIBLE.md section 8).
{
- chose("dallahs") == "ask":
    سألتيني عنها، وأنا ما جاوبت. هي هيك كانت: تحكي بالفنجان أكتر من الكلام. #abu_fadi
- else:
    طلع الفنجان [[مزبوط]]… وأنا وعدتك بحكاية. بس مش اليوم، اليوم بدّي أشرب. #abu_fadi
}
في آخر دفتر الوصفات صفحة مقطوعة: «كنافة أم خليل»… مين أخدها؟ #sys
-> END

// Memory 1 (pour) and the letter. Lines from feel-demo/index.html.

=== m1_lead ===
يا الله… هاي قهوتها. نفس الهيل، ونفس الحرقة الخفيفة بآخرها. #abu_fadi
أول مرة قعدت على هالطاولة كان عمري خمسة وعشرين سنة. وستّك كانت بعمرك. #abu_fadi
-> END

=== m1_intro ===
أهلا بأول زبون بقهوتي! شو بتشرب يا أستاذ؟ #teta_young
قهوة… وإذا في، ابتسامة كمان. #fadi_young
القهوة بس يا أستاذ فادي. الابتسامة محجوزة لجوزي. #teta_young
-> END

=== m1_perfect ===
هيك بالضبط. القهوة بدها صبر وإيد ثابتة. #teta_young
-> END

=== m1_ok ===
ولا يهمّك. القهوة بتسامح، متل الناس الطيبين. #teta_young
-> END

=== m1_outro ===
شو رح تسمّي القهوة؟ لازم يكون إلها اسم. #fadi_young
لسّا ما إلها اسم. لما ييجوا ولادي، بسمّيها على اسمهم. #teta_young
إجت بنتين: رنا ونوال. وبعد سنين، كتبت ستّي رسالة، وخبّتها تحت الكاونتر. #sys
خمسين سنة، وما حدا قرأها. #sys
-> END

=== m1_return ===
ستّك الله يرحمها كانت تقول: هالكاونتر إله سرّ، بس مش وقته. #abu_fadi
سرّ؟ أي سرّ يا عمّو؟ #salma
اسألي الدرج يا بنتي. أنا بشرب قهوتي وبسكت. #abu_fadi
-> END

// The letter: one tag per line style (letter, letter_faded, letter_signature).
=== letter_1 ===
لبناتي، رنا ونوال، #letter
إذا عم تقرأوا هالرسالة، معناه إني ما قدرت أقولها بوجهكم. #letter
القهوة إلكم التنتين. مش لأنكم تصالحتوا… لأني بدّي إياكم تتصالحوا. #letter
الغلط من الأول كان غلطي أنا، مش غلط حدا فيكم. #letter
وإذا بدكم تعرفوا شو صار سنة 2006… #letter_faded
— أمّكم #letter_signature
-> END

=== voice_1 ===
سلمى يا روحي… إذا لقيتي الرسالة، اعملي فنجانين. واحد إلك، وواحد للانا. #teta
-> END

=== lana_notify ===
وصلت عمّان. أمّي بتقول ما بدها تشوف خالتي… وأنا بدّي أعرف ليش. #lana
-> END

// [draft] Abu Fadi's first friendship scene (level 3, SYSTEMS_B.md section 3): his lost love. Needs the native writer.
=== abu_fadi_3 ===
بتعرفي يا سلمى… أنا كمان كان إلي حدا بستنّاه [[هون]]. #abu_fadi
كل جمعة كانت تقعد جنب الشبّاك، وتطلب فنجانها سادة، وما تحكي ولا كلمة. #abu_fadi
وشو صار يا عمّو؟ #salma
سافرت سنة 76. وأنا ضلّيت أجي كل جمعة، يمكن ترجع. ستّك كانت تعرف، وما حكت لحدا. #abu_fadi
-> END
