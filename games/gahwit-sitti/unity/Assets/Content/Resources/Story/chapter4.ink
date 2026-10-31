// Chapter 4: دلال ستّي · Teta's dallahs
// [draft] Written by Claude Code from STORY_BIBLE.md sections 5 and 6; every line waits for the Amman dialect writer and
// the Najdi writer (CLAUDE.md, writing style). Karim repairs the old wood and remembers Teta through his master, Subhi
// (proposed name), who built her counter in 1974 and took the opening-day photo (memory 4, restore).
// The speaker is the first tag on a line, the expression the second. [[word]] marks a tap-to-explain word; its meaning
// is in the Glossary table. chapter4_en.ink (English) and chapter4_ar.ink (neutral-Arabic subtitles) follow this file
// line for line (the parity test in Story/Tests checks it).

// The saved choice for a knot ("" before it's made), bound by the game (STORY_BIBLE.md section 8).
EXTERNAL chose(knot)

// [draft] Task 1: Karim's workbench in the corner. He won't take money for Im Khalil's café.
=== workbench ===
عمّو كريم، [[قدّيش]] بدّك عالشغل كلّه؟ قول رقم وما تستحي. #salma
ولا قرش. هاي قهوة [[الحجّة]] أم خليل. #karim
ما يصير. كل شغل له فاتورة، وأنا أبغى كل شي مكتوب. #lana
اكتبي: «مدفوع من زمان». كنت صبي عند معلّمي، والحجّة ما خلّتني يوم جوعان. #karim
طيب، من اليوم صحنك وفنجانك علينا. #salma #smile
-> END

// [draft] Task 2 (styles): a shelf for every dallah. The smallest one is missing.
=== dallah_shelf ===
وحدة، تنتين، تلاتة… ستّة. كانوا سبعة! #salma #surprised
عملته متل رف معلّمي القديم، لسبع دلال. وآخر [[مطرح]]، الصغير، فاضي. #karim
الصغيرة… دلّة ستّي الصغيرة مش هون. #salma #sad
يمكن انكسرت، أو أحد أخذها. ما هي مشكلة، نشتري غيرها. #lana
ما في غيرها يا لانا. هاي اللي ما حدا كان يصبّ منها غير ستّي. #salma
-> END

// [draft] Task 3: the counter repaired. Karim finds his master's mark under it, from 1974, and a line in Teta's hand.
=== counter_repair ===
استنّي يا [[معلّمة]]… في إشي محفور تحت الكاونتر. #karim #surprised
«صبحي، 1974». هاي إيد معلّمي. بعرفها من بين ميّة إيد. #karim
معلّمك هو اللي عمل الكاونتر؟ والله ما كنت بعرف. #salma #surprised
وتحتها بقلم رصاص: «يسلموا إيديك». هاد خطّ الحجّة. #karim #sad
الخشب بيتذكّر مين عامله منيح. خمسين سنة وهو حافظها. #karim
-> END

// [draft] Task 4: the small dallah found and polished. Lana had taken it. Abu Fadi: "it was hers alone" (chapter 1,
// dallahs); his first line reads the choice made there, like chapter 1's counter (STORY_BIBLE.md section 8).
=== small_dallah ===
خذي، لمّعتها. كانت عندي من أول ليلة. #lana
الدلّة الصغيرة! عمّو، شوف، رجعت. #salma #surprised
{
- chose("dallahs") == "ask":
    سألتيني عنها أول يوم، وبعتّك عالدفاتر. بس هالحكاية مش مكتوبة بدفتر. #abu_fadi
- else:
    وعدتك بحكاية عنها يا بنتي، وقلتلك مش اليوم. هسّا إجا وقتها. #abu_fadi
}
كانت إلها لحالها… إلّا لبنتين زغار. تحطّ إيدها فوق إيديكم، وتصبّوا سوا. #abu_fadi #smile
عشان كذا أخذتها. كنت أبغى أتذكّر إيدها. #lana #sad
-> END

// [draft] Task 5: frames for the photo wall. One photo is in pieces, and Samira knows why.
=== frames ===
مغلّف بخطّ ستّي: «يوم الافتتاح، 1974. لا تكبّوها.» وجوّاته [[شقف]] صورة. #salma #surprised
يا فضيحتي! وحياة ولادي، أنا اللي قطّعتها! #samira #surprised
كنت بنت زغيرة، وطلعت فيها مكشّرة. ستّك ما حكتلي ولا كلمة… بس لمّت الشقف كلها. #samira
إذا الشقف كلها هون، فالبرواز الكبير إلها. #karim
رح نرجّعها متل ما كانت يا خالتو. حتى التكشيرة. #salma #smile
-> END

// [draft] Task 6: the opening-day photo framed. Memory 4 (restore) plays after this scene.
=== photo_frame ===
البرواز جاهز، والقزاز جديد. ناقصه بس الصورة. #karim
نرتّبها مع بعض؟ أنا عليّ الأطراف، وإنتِ عليك الوجيه. #lana
اتفقنا. يلّا نشوف مين كان هون أول يوم. #salma #smile
-> END

// Memory 4 (restore): the opening day in 1974. Karim's master takes the photo the player puts back together.

// [draft]
=== m4_lead ===
شايفة هالإيد الماسكة فنجان؟ هاي إيدي. أنا كمان كنت بالصورة. #abu_fadi #smile
ومعلّمي كان ورا الكاميرا. كان يقول: «ولا حدا يتحرّك، الصورة بتعيش أكتر منّا». #karim
-> END

// [draft] 1974: older Amman speech, no modern loanwords.
=== m4_intro ===
ارجعوا لورا شوي يا جماعة. بدّي الخشب يبيّن كلّه. #karim_master_1974
يا معلّم صبحي، صوّر الناس، مش الخشب! #teta_young #smile
وأول زبون، وين بيوقف يا ستّ أم خليل؟ #fadi_young
قدّام، وفنجانه بإيده. وإنتِ يا سميرة يا روحي، بلا تكشير! #teta_young
سميرة، ابتسمي للمعلّم، الله يرضى عليكِ. #samira_mother_1974
-> END

// [draft]
=== m4_perfect ===
كل وجه بمطرحه، ولا حدا رمش. هاي صورة بتعيش. #karim_master_1974
-> END

// [draft]
=== m4_ok ===
مش مهم إذا حدا رمش. المهم إنّا كلنا فيها. #teta_young
-> END

// [draft]
=== m4_outro ===
وإنت يا معلّم؟ إنت اللي عملت هالخشب كلّه، وما إلك مطرح بالصورة؟ #teta_young
اللي بيصوّر ما بيطلع بالصورة يا ستّ. بس بصوّر شغلي لحاله، إلي. #karim_master_1974
صوّر صبحي الكاونتر لحاله، وعلّق الصورة بورشته. بعد سنين، تعلّم الصنعة تحتها صبي اسمه كريم. #sys
-> END

// [draft]
=== m4_return ===
صورة الكاونتر لسّا عندي بالورشة. كبرت وأنا بتطلّع عليها. #karim
وهاي أنا، شوفوا! مكشّرة متل ما قلتلكم. #samira #smile
كلكم كنتوا هون من أول يوم. والصورة رجعت لمطرحها. #salma #smile
-> END
