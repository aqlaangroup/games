// Chapter 6: الافتتاح · Grand reopening
// [draft] Drafted by Claude Code from STORY_BIBLE.md sections 5, 6 and 8. Every line waits for the Amman dialect writer
// (Salma, Karim, Yazan, Abu Fadi, Samira; the 1974 lines in older Amman) and the Najdi writer (Lana) (CLAUDE.md,
// writing style).
// The speaker is the first tag on a line, the expression the second. [[word]] marks a tap-to-explain word; its meaning
// is in the Glossary table.
// chapter6_en.ink (English) and chapter6_ar.ink (neutral-Arabic subtitles) follow this file line for line
// (the parity test in Story/Tests checks it).

// The saved choice for a knot ("" before it's made), bound by the game (STORY_BIBLE.md section 8).
EXTERNAL chose(knot)

// [draft]
=== rooftop_stairs ===
يا يزن، العود بعدين. الصندوق أول، وبعدين بتغنّي. #karim
بس اسمع هالدرج يا معلّم… كل درجة إلها نغمة. #yazan #smile
يزن بيدقّ عالعود وهو طالع، والصوت بيطلع معه لفوق. #sys
الدرج خشبه عتيق، بس قلبه قوي. متل الحجّة الله يرحمها. #karim
يا يزن، إنت كل يوم بتعزف عالرصيف. ليلة الافتتاح، السطح إلك. #salma #smile
-> END

// [draft]
=== rooftop_lights ===
يا الله… من هون بتبيّن عمّان كلها. الجبال كلها أضواء. #salma #surprised
وش ذا المنظر؟ كأن البيوت نازلة من السما. #lana #surprised
ستّي كانت تطلع عالسطح كل مغرب، وتقول إنها بتسلّم عالحارة. #salma
[[هسّا]] بعرف ليش. #salma #smile
زين، بس لا [[تصيحين]] قبل لا نعلّق الأضواء. #lana #smile
-> END

// [draft]
=== new_sign ===
بدبي كنا نعمل ميتنغ كامل عشان اسم واحد… خلص، نسيت إني رجعت. #salma #smile
أبغى اسم يعرفه أهل الرياض بعد. #lana #thinking
الحارة كلها بتقول «قهوة ستّي». والخشب ما بيتعلّم اسم جديد بهالعمر. #karim
«ستّي»؟ [[ترى]] هي جدّتي أنا بعد. #lana
وإنتِ لما تقوليها، بتصير ستّك. يعني الاسم إلنا التنتين. #salma #smile
-> END

// [draft]
=== invitations ===
كتبتي دعوة لكل بيت في الحارة… و[[ذي]] الأخيرة حق مين؟ #lana
لماما. #salma #sad
«ماما، الافتتاح الخميس الجاي. الكرسي اللي ورا الكاونتر لسّا إلك.» #salma
أنا رسلت لأمي صورة الدعوة. ما ردّت… بس شافتها. #lana #sad
يمكن هيك [[بتبلّش]] الأشيا… بشوفة. #salma
-> END

// [draft]
=== flowers ===
وصل ورد أبيض كتير، وبين الورد عروق [[حبق]]. وما في ولا كرت. #sys
بدون اسم؟ أحد يرسل ورد وما يكتب اسمه؟ #lana #surprised
ماما. ما حدا بيحطّ حبق بالورد غيرها. #salma #surprised
كانت تقول: الورد للعين، والحبق لستّك. #salma #sad
ما إجت… بس إجت ريحتها. هاي خطوة يا بنتي. #abu_fadi
-> END

// [draft]
=== opening_night ===
القهوة مليانة لآخرها، والسطح كمان. حتى الدرج قعدوا عليه ناس. #sys
// [draft] The callback for the choice in chapter 5's menu_board (STORY_BIBLE.md section 8, c5_menu). The game saves a
// choice under the knot it was made in, so this reads chose("menu_board").
{
- chose("menu_board") == "saudi":
    السعودية أول في اللوحة… وأبو فادي ما طلب إلا العمّانية. ما عليه، بيتعوّد. #lana #smile
- else:
    شوفي، العمّانية أول في اللوحة… والناس كلهم يطلبون السعودية. وش قلت لك؟ #lana #smile
}
أنا كنت أول زبون [[هون]]، سنة 74. وما كنت مفكّر إني بشوفها مليانة هيك مرة تانية. #abu_fadi
أم خليل كانت تقول: القهوة مش حيطان، القهوة ناس. وإنتو… إنتو الناس. #abu_fadi #smile
سلمى بتطلّع عالباب كل شوي. الكرسي اللي ورا الكاونتر لسّا فاضي. #sys
-> END

// Memory 6 (trace): Teta sews Im Samira's wedding dress, 1974. Samira's mother was a young widow marrying again, so
// little Samira (memory 2, the 1974 photo) can hold the pins (STORY_BIBLE.md section 6; see report).

// [draft]
=== m6_lead ===
هالسطح شاف عرس قبل الليلة. عرس إمّي، سنة 74. #samira
عرس إمّك؟ بس إنتِ كنتِ موجودة سنة 74! #salma #surprised
أبوي الله يرحمه راح وأنا صغيرة. وبهديك السنة إمّي تجوّزت من جديد… وستّكم خيّطت فستانها. #samira
-> END

// [draft]
=== m6_intro ===
يا أم خليل، الناس رح تحكي. أرملة وبتلبس أبيض؟ #samira_mother_1974
خلّيهم يحكوا. العروس عروس، أول مرة ولّا تاني مرة. #teta_young
وإنتِ يا سميرة، امسكي الدبابيس [[منيح]]، ولا تبلعي ولا وحدة. #teta_young #smile
الخطّ مرسوم بالطبشورة. الإبرة بتمشي وراه، مش قدّامه. #teta_young
-> END

// [draft]
=== m6_perfect ===
ولا غرزة طلعت عن الخطّ. هيك بيتخيّط فستان العمر. #teta_young #smile
-> END

// [draft]
=== m6_ok ===
غرزة عوجا هون وهناك… ما حدا رح يشوفها. العريس ما رح يشوف غير العروس. #teta_young #smile
-> END

// [draft]
=== m6_outro ===
والله يا أم خليل… ما كنت مفكّرة إني بلبس أبيض مرة تانية. #samira_mother_1974
يا روحي، القلب إله أكتر من باب. والليلة الحارة كلها بترقص عالسطح. #teta_young #smile
بهديك الليلة، سميرة الصغيرة رقصت لحد ما نامت بحضن ستّي. وعلبة الخياطة رجعت على رفّها. #sys
-> END

// [draft]
=== m6_return ===
إمّي لليوم بتقول: «أم خليل خيّطتلي فستان، وخيّطتلي معه حظّي.» #samira #smile
يلّا، مين عليها الدور؟ عندي عريس جاهز… #samira
لا يا خالة سميرة. خلّي العرسان في علبتهم. #lana #smile
-> END
