// Chapter 8: عرض فارس · Faris's offer
// Every knot here was drafted by Claude Code from STORY_BIBLE.md (sections 3–8) and waits for the Amman and
// Najdi dialect writers (CLAUDE.md, writing style). The speaker is the first tag on a line; the second is the expression.
// [[word]] marks a tap-to-explain word; its meaning is in the Glossary table.
// chapter8_en.ink (English) and chapter8_ar.ink (neutral-Arabic subtitles) follow this file line for line
// (the parity test in Story/Tests checks it). Change this file first, then those two.
// Faris's English work words are written in Arabic letters (براند، أوفر، ديل) so they sit inside the right-to-left line.

// The saved choice for a knot ("" before it's made), bound by the game (STORY_BIBLE.md section 8).
EXTERNAL chose(knot)

=== accounts_desk ===
حسبتها ثلاث مرّات على المكتب الجديد. السقف يبغى ضعف اللي معنا. #lana #thinking
السقف تعبان يا بنات. الجسور عطشانة، وأول شتوة رح [[يدلف]] عالكاونتر. #karim
بدبي كنت أحسب ميزانيات بالملايين… وهون مش لاقية حقّ سقف. #salma #sad
على الأقل المكتب ما يدلف. #lana #smile
ببلّش بكرا، والحساب بعدين. الحجّة ما كانت تخلّي سقف يستنّى مصاري. #karim
-> END

=== roof_repair ===
خلص السقف. الجسور الجديدة تصاحبت مع القديمة، وما عاد في دلف. #karim #smile
مبروك السقف يا جماعة! اليوم الحساب كلّه عليّ… اعتبروه إنفستمنت بالحارة. #faris #smile
هذا كل يوم يقعد على نفس الطاولة، ويعرف المنيو أحسن منّا. #lana #thinking
زبون كريم يا لانا. شو بدّك فيه؟ #salma
بس سؤال صغير: الكنافة… ليش مش عالمنيو؟ #faris
-> END

// The choice c8_offer (STORY_BIBLE.md section 8): hear him out, or ask him to leave. The game saves a choice
// under the knot it was made in (CafeScene.PlayScene), so chapter 9's the_page reads it with chose("faris_table").
=== faris_table ===
خلّيني أكون صريح معكم: أنا فارس. البراند اللي بكل مول بعمّان، براند أبوي. #faris
وعندي أوفر للبناية كلها. سعر ممتاز، كاش، والآرمة بتضل زي ما هي. #faris
يعني كل هالقعدات، كنت تدرس المكان؟ #lana #surprised
* [«احكي، إحنا سامعينك.» #hear]
    شكرًا. فكّروا فيها بس… الأرقام ما بتكذب. #faris #smile
* [«لو سمحت، تفضّل اطلع.» #leave]
    حقّكم. بس الأوفر بيضل عالطاولة. #faris #neutral
- فارس ترك ورقة صغيرة عليها رقم. ولانا ما شالت عيونها عنها. #sys
-> END

// The cousins' first argument since chapter 3. Salma goes quiet instead of getting cruel (voice sheet).
=== family_photos ===
سلمى، اسمعيني بس. بفلوسه نصلّح كل شي، ونبدأ بمكان ثاني. #lana #thinking
بمكان تاني؟ شوفي الصورة يا لانا: أمّي وأمّك على نفس الكرسي، هون. #salma #angry
وأمّي وأمّك ما تكلّموا عشرين سنة بسبب هالمكان. يمكن لو راح، يرتاحون. #lana #sad
سلمى ما ردّت. علّقت الصورة، وعدّلتها، ورجعت عدّلتها. #sys
لا تسكتين. سكوت أمّي يكفيني. #lana #sad
-> END

// Faris thinks his father's 2006 papers give him the first right to buy the building (STORY_BIBLE.md section 3).
=== knafeh_sign ===
هون كان لوح الكنافة. كنت أجي مع أبوي كل جمعة، وأنا صغير. #faris
وليش انشال اللوح؟ #salma
أبوي عمل ديل مع ستّكم سنة 2006. وبالورق، إلنا الأولوية إذا البناية انباعت. #faris
ستّي سوّت ديل؟ مع أبوك؟ وش باعت؟ #lana #surprised
هاد اللي ما بعرفه. كل ما أسأل أبوي بيقول: كانت ديل. وبيسكت. #faris #thinking
-> END

// Lana straightens the photo: her apology, without words.
=== decision_table ===
قبل ما تقعد، لانا عدّلت الصورة اللي سلمى عدّلتها مرّتين. #sys
زين. وش نقرّر؟ #lana
ولا إشي. مش قبل ما نعرف شو صار سنة 2006. #salma
اتفقنا: لا نبيع، ولا نقول لا. [[لين]] نعرف. #lana #smile
أول مرة بنتّفق على إنّا ما نتّفق. #salma #smile
-> END

// Memory 8 (choose): young Teta haggles at the souk for her first dallah, 1974 (STORY_BIBLE.md section 6).
// The prices are story, not game numbers (lira, as in memory_practice.ink).

=== m8_lead ===
ستّكم؟ عمرها ما دفعت أول سعر. ولا مرّة بحياتها. #abu_fadi #smile
أول دلّة إلها اشترتها من [[البلد]] قبل ما تفتح بشهر. حكتلي القصة مية مرة… وكل مرة السعر بينزل. #abu_fadi
-> END

=== m8_intro ===
يا عمّي، هاي الدلّة اللي بالواجهة… بكم؟ #teta_young
نحاس أصلي، شغل إيد. بعشر ليرات يا ستّ، ولا قرش أقل. #seller_1974
ولا قرش؟ طيّب. خلّينا نحكي. #teta_young #smile
-> END

// The timed conversation (ChooseGame): six rounds, one per balance.json levelB.memories.choose.choices, so
// six good answers can reach the third star. Each round is the seller's line, then a good and a weak answer.
=== m8_game ===
شوفي النقش يا ستّ. ما في متلها بكل السوق. #seller_1974
* [«حلو النقش… بس منقارها مايل شوي.» #good]
    مايل؟! …شوي. ماشي، بنزّلك. #seller_1974
* [«صحيح، ما في متلها.» #weak]
    الله يرضى عليكِ. #seller_1974
-
احمليها، شوفي قدّيش تقيلة. #seller_1974
* [«خفيفة يا عمّي. النحاس رقيق.» #good]
    إنتِ بتفهمي بالنحاس؟ …ماشي. #seller_1974
* [«تقيلة، يعني ممتازة.» #weak]
    ممتازة، وسعرها ممتاز. #seller_1974
-
إنتِ متجوّزة؟ خلّي جوزك ييجي يحكي بالسعر. #seller_1974
* [«جوزي بيدفع أول سعر. عشان هيك أنا اللي جيت.» #good]
    …فهمت ليش هو قاعد بالبيت. #seller_1974
* [«ماشي، ببعتلك إياه بكرا.» #weak]
    أهلا وسهلا فيه. السعر ما بيتغيّر. #seller_1974
-
إذا مش عاجبتك، الدكاكين كتيرة. #seller_1974
* [«معك حق. الله معك يا عمّي.» #good]
    استنّي، استنّي! وين رايحة؟ #seller_1974
* [«لا لا، عاجبتني كتير!» #weak]
    عرفت. #seller_1974
-
بدّك فناجين معها؟ عندي صيني من الشام. #seller_1974
* [«فنجانين هديّة، إذا أخدت الدلّة.» #good]
    هديّة؟! …ماشي، فنجانين وبس. #seller_1974
* [«أكيد. بكم الفنجان؟» #weak]
    بنتّفق، بنتّفق. #seller_1974
-
آخر كلمة؟ تسع ليرات. #seller_1974
* [«سبعة. وبحكي لكل عمّان إنك أشطر بيّاع بالبلد.» #good]
    سبعة! بس احكيلهم إني ما نزلت بسهولة. #seller_1974
* [«ماشي، تسعة.» #weak]
    مبروكة عليكِ يا ستّ. #seller_1974
-
-> END

// After 3 stars: every answer was good, so the last price was seven.
=== m8_perfect ===
والله بعمري ما نزّلت هالقدّ لحدا. مبروكة الدلّة يا ستّ. #seller_1974
-> END

=== m8_ok ===
إنتِ صعبة يا ستّ… الله يعين جوزك. #seller_1974 #smile
-> END

=== m8_outro ===
أول دلّة إلي. منها بصبّ لأول زبون… ولولادي، إذا الله أعطاني. #teta_young #smile
وإذا أعطاكِ، لا تعلّميهم المفاصلة. خرّبتي سوقي. #seller_1974
لا تخاف. بعلّمهم إشي أصعب: إنه في أشيا ما إلها سعر. #teta_young
-> END

// Abu Fadi keeps his promise: he changes the subject instead of lying (voice sheet).
=== m8_return ===
«في أشيا ما إلها سعر»… بس ستّي نفسها باعت شي لأبو فارس. #lana #thinking
عمّو أبو فادي… إنت بتعرف شو باعت؟ #salma
بعرف إنه الجواب مش عند فارس. اشربي قهوتك يا بنتي. #abu_fadi
-> END
