// The regulars' friendship scenes: 12 regulars × friendship levels 3, 6 and 10 (STORY_BIBLE.md section 9;
// SYSTEMS_B.md section 3). The game plays "<regular>_<level>" when a regular reaches that level (CustomerRules.StoryDue).
// Written by Claude Code; every knot waits for the Amman dialect writer (and the Najdi writer for Lana's lines).
// Level 3 scenes can play from chapter 1, so they mention no later plot. Level 6 assumes chapter 4 or later, level 10
// chapter 7 or later. Faris's 6 and 10 wait for chapters 9 and 10 (chapters.json storyFromChapter). Each level 10 scene
// ends with the regular's decor gift. The speaker is the first tag on a line; [[word]] marks a tap-to-explain word whose
// meaning is in the Glossary table. regulars_en.ink and regulars_ar.ink follow this file line for line (the parity test).

// The saved choice for a knot ("" before it's made), bound by the game (STORY_BIBLE.md section 8). No
// regular scene reads one yet.
EXTERNAL chose(knot)

// Level 3: his lost love. Copied from chapter1.ink, where it was first written.
=== abu_fadi_3 ===
بتعرفي يا سلمى… أنا كمان كان إلي حدا بستنّاه [[هون]]. #abu_fadi
كل جمعة كانت تقعد جنب الشبّاك، وتطلب فنجانها سادة، وما تحكي ولا كلمة. #abu_fadi
وشو صار يا عمّو؟ #salma
سافرت سنة 76. وأنا ضلّيت أجي كل جمعة، يمكن ترجع. ستّك كانت تعرف، وما حكت لحدا. #abu_fadi
-> END

// Level 6: the photo he never shows anyone.
=== abu_fadi_6 ===
تعالي يا سلمى، اقعدي. بدّي [[أفرجيك]] إشي عمري ما فرّجته لحدا. #abu_fadi
طلّع أبو فادي من جيبته صورة صغيرة، أبيض وأسود: صبيّة قاعدة جنب هالشبّاك، وعيونها عالشارع. #sys
يا الله شو حلوة… هاي هي يا عمّو؟ شو اسمها؟ #salma #surprised
الصورة بتكفّي اليوم يا بنتي. الاسم… خلّيه لبعدين. #abu_fadi #sad
-> END

// Level 10: her name, and that Teta knew. Gives the window clock.
=== abu_fadi_10 ===
اسمها نجوى. خمسين سنة وأنا ما قلته بصوت عالي. #abu_fadi
نجوى… وستّي كانت تعرف؟ #salma #surprised
كانت تعرف اسمها، وعمرها ما قالته قدّامي ولا مرة. هاد كان لطفها: تسكت عشان ما توجعني. #abu_fadi
خدي هالساعة يا بنتي، ساعة الشبّاك. خمسين سنة وأنا أعدّ عليها دقايق الجمعة… بس [[هسّا]] ما عاد بدّي أعدّ. #abu_fadi #smile
-> END

// Level 3: Tawjihi in three months; home is loud.
=== tala_3 ===
بتسمحيلي أدرس [[هون]]؟ بالبيت إخواني الأربعة بيصرّخوا، وما بقدر أركّز. #tala
أكيد! شو بتدرسي؟ #salma
أنا [[توجيهي]] هالسنة، والامتحانات بعد تلات شهور. والكيميا لسّا ما خلصت. #tala #sad
خلص، الزاوية الهادية إلك. وكل ما تخلّصي فصل، كرة معمول على حسابي. #salma #smile
-> END

// Level 6: she fails a mock exam and wants to quit.
=== tala_6 ===
تالا قاعدة بالزاوية، وقدّامها ورقة امتحان عليها علامة حمرا كبيرة. #sys
رسبت بالتجريبي. خلص، أنا مش إلها. بدّي أترك المدرسة. #tala #sad
وش فيها؟ أنا رسبت في الرياضيات مرّة، والحين أنا اللي أحسب دفتر القهوة. #lana
التجريبي عشان تعرفي وين الغلط، مش عشان تتركي. يلّا، من أول سؤال. #salma
-> END

// Level 10: results day; she passes and wants pharmacy. Gives her study lamp.
=== tala_10 ===
طلعت النتايج! نجحت، ومعدّلي بيدخّلني صيدلة! #tala #smile
ألف مبروك يا تالا! لو ستّي هون، كانت وزّعت معمول عالحارة كلها. #salma #smile
خدي لمبة الدراسة اللي سهرت عليها كل السنة. خلّيها بالزاوية، لأي حدا بعدي بدّو يدرس [[هون]]. #tala
-> END

// Level 3: a young widow who cooks for her son; cards by the till.
=== um_rami_3 ===
صباح الخير يا سلمى. بتسمحيلي أحط كروتي جنب الصندوق؟ #um_rami
كروت؟ شو بتشتغلي يا أم رامي؟ #salma
بطبخ من البيت. من يوم ما توفّى أبو رامي، الله يرحمه، والطبخ هو اللي معيّشني أنا ورامي. #um_rami #sad
حطّيهم قدّام، جنب الدلّة. وأول طلبية، من عندي. #salma #smile
-> END

// Level 6: her first catering order goes wrong; the cousins help.
=== um_rami_6 ===
خمسين صحن مقلوبة، والطنجرة الكبيرة احترقت، والعزومة بعد ساعتين! #um_rami #sad
[[لا تحاتين]]! أنا أقطّع، وسلمى تطبخ، وإنتي بس قولي لنا وش نسوّي. #lana
ساعتين بيكفّوا. ستّي كانت تطبخ لعرس كامل بساعتين. يلّا! #salma
بعد ساعتين بالضبط، طلعت الصواني من الباب، وريحتها عبّت الشارع. #sys
والله ما بنساها إلكم. لا أنا، ولا رامي. #um_rami #smile
-> END

// Level 10: the big order for the wedding. Gives her painted plate.
=== um_rami_10 ===
سلمى! إجتني أكبر طلبية بحياتي: عرس الدكتورة هبة. ميتين صحن! #um_rami #surprised
ميتين؟! يا أم رامي، هاد مش مطبخ بيت، هاد مطعم! #salma #surprised
بتذكري يوم الطنجرة المحروقة؟ لولاكم، كنت رمّيت الكروت وقعدت بالبيت. #um_rami
خدي هالصحن الفخّار المزخرف، أول إشي اشتريته من [[مصاري]] الطبخ. علّقيه عالحيط، عشان نتذكّر إنّا بدينا سوا. #um_rami #smile
-> END

// Level 3: his master built the counter in 1974.
=== karim_3 ===
هالكاونتر عمله معلّمي، الله يرحمه، سنة 74. أكتر من خمسين سنة، وما اشتكى ولا مرة. #karim
يعني إنت بتعرف ستّي من زمان، عمّو كريم؟ #salma #surprised
من يوم ما صرت صبي عند معلّمي. كل ما الحجّة تحتاج إشي، كان يبعتني أنا. #karim
وكانت تقول: الخشب متل القهوة، بدّو صبر. ومعلّمي يضحك، لأنه هاد كلامه هو. #karim #smile
-> END

// Level 6: he never married; the workshop is his family.
=== karim_6 ===
عم كريم، ليه ما تزوّجت؟ #lana
تزوّجت الورشة. فيها خمس مناشير وتلات مطارق، وكلهم بيسمعوا الكلام. #karim #smile
وكل باب عملته بهالحارة ولد من ولادي. [[بمرق]] من جنبه وبسلّم عليه. #karim
يعني نص الحارة ولادك؟ #salma #smile
والكاونتر أكبرهم. وإنتو التنتين… صرتوا من العيلة كمان. #karim
-> END

// Level 10: a sign for a café in Riyadh, for the day it opens. Gives the sign.
=== karim_10 ===
يا لانا، كل يوم بتقولي إنه الرياض ما فيها قهوة متل هاي. #karim
وأقولها بعد! وش فيه؟ #lana
عمّو كريم، شو مخبّي ورا ضهرك؟ #salma #surprised
آرمة صغيرة من خشب الزيتون: «قهوة ستّي · الرياض». علّقيها هون لحد ما ييجي يومها. الخشب بيعرف قبل الناس. #karim #smile
-> END

// Level 3: she brags about her husband in the Gulf.
=== samira_3 ===
جوزي بدبي يا سلمى. أكيد شفتيه هناك! الكل بيعرفه. #samira
خالتو، دبي فيها تلات مليون إنسان. #salma
وكلهم بيعرفوه! مدير يا حبيبتي، مدير. وكل عيد بيبعتلي إسوارة دهب. #samira
هالسنة تأخّرت شوي… أكيد الإسوارة كبيرة، وما [[فاتت]] بالظرف. #samira #smile
-> END

// Level 6: he hasn't visited in two years; she admits it.
=== samira_6 ===
سلمى، بدّك الصدق؟ جوزي إله سنتين ما إجا. ولا مرة. #samira #sad
سنتين يا خالتو؟ وإنتِ كل يوم بتحكي عنه… #salma
بحكي عنه عشان ما أنساه. ولا عشان ما ينساني هو… مش عارفة. #samira
بس ولا كلمة لحدا! وحياة ولادي، إذا انحكت، بجيبلك عريس كل خميس. #samira #smile
-> END

// Level 10: he comes home for the wedding; she is quiet for once. Gives her embroidered cushion.
=== samira_10 ===
[[فاتت]] خالتو سميرة ساكتة، ووراها رجّال شعره أبيض، وبإيده شنطة سفر. #sys
هاد… جوزي. إجا عشان عرس الدكتورة هبة. قال: عرس إنتِ مرتّبيته، ما بفوّته. #samira
أهلين عمّو! خالتو، ليش ساكتة؟ أول مرة بشوفك هيك. #salma #smile
خلّيني ساكتة شوي يا سلمى. بس شوي. #samira #smile
خدي هالمخدّة المطرّزة. طرّزتها بالليالي اللي كنت أستنّاه فيها، و[[هسّا]] صار في حدا يقعد جنبي. #samira #smile
-> END

// Level 3: night shifts; the café stays open late for her.
=== dr_hiba_3 ===
الساعة تنتين بالليل، وإنتِ لسّا فاتحة؟ الله يخلّيكِ. #dr_hiba #surprised
ولمين بدّي أسكّر؟ إنتِ كل ليلة بتطلّي علينا بعد المستشفى. #salma
مناوبة ليلية كل يوم. فنجانك هو الإشي الوحيد الدافي بالليل كله. #dr_hiba
خلص، الضو بيضل مولّع. إلك، ولكل اللي بيشتغلوا والناس نايمة. #salma #smile
-> END

// Level 6: at 2 am she meets the pharmacist from chapter 2.
=== dr_hiba_6 ===
الساعة تنتين بالليل. عالطاولة اللي جنب د. هبة، قاعد الصيدلي اللي جابته خالتو سميرة. #sys
سلمى، مين هاد؟ بيعرف كل دوا بكتبه، وبيقرا خطّي كمان! #dr_hiba #surprised
هاد هاني، صيدلي المناوبة اللي عالزاوية. خالتو جابته إلي… بس شكله إجا عشانك. #salma #smile
إذا ناوي على خير، بيعرف وين بيت أهلي. لحد [[هسّا]]… ولا كلمة لخالتك سميرة. #dr_hiba #smile
-> END

// Level 10: she asks the cousins to cater her wedding. Gives a night jasmine.
=== dr_hiba_10 ===
هاني إجا مع أهله لبيتنا، وأبوي وافق. قرينا الفاتحة يوم الجمعة! #dr_hiba #smile
مبروك يا دكتورة! والله الخبر أحلى من القهوة. #salma #surprised
وبدّي إنتِ ولانا تعملوا ضيافة العرس. القهوة، والحلو، وكل إشي. #dr_hiba
[[أبشري]]! بس وش تبين: قهوة عمّانية ولا سعودية؟ #lana #smile
التنتين، متلكم. وخدي شتلة [[مسك الليل]] هاي من جنينة أمي: بتفتّح بالليل، متلي ومتل قهوتكم. #dr_hiba #smile
-> END

// Level 3: he busks with an oud outside; Salma invites him in.
=== yazan_3 ===
برّا عالرصيف، يزن بيعزف عالعود، وقدّامه علبة فيها كم قرش. #sys
يزن! ليش دايمًا برّا بالبرد؟ [[فوت]] لجوّا واعزف عنّا. #salma
أنا؟ بس أنا ما معي حق فنجان. #yazan #surprised
العزف حقّه. ستّي كانت تقول: قهوة من غير غنا، متل فنجان من غير هيل. #salma #smile
عيني إلك. بس انتبهي، رح أعزف لحد ما تطرديني. #yazan #smile
-> END

// Level 6: he plays Teta's radio song (memory 7 comes later and plays it again).
=== yazan_6 ===
يزن [[بيدوزن]] العود، وبعدين بيعزف لحن قديم وبطيء. #sys
استنّى… هاي الغنية! ستّي كانت تغنّيها كل جمعة مع الراديو. #salma #surprised
جدّي علّمني إياها. كان يقول: بجبل عمّان زمان، كل الشبابيك كانت تغنّيها. #yazan
حتى أمي تدندنها في المطبخ… وعمرها ما قالت لي من وين جابتها. #lana #thinking
خلص، صارت غنية القهوة. كل ما تطلبوها، بعزفها. #yazan #smile
-> END

// Level 10: he will play the wedding. Gives his old oud.
=== yazan_10 ===
سلمى! عرس الدكتورة هبة… طلبوني أعزف فيه. أول عرس إلي، وبأجرة كمان! #yazan #smile
من الرصيف لعرس؟ يا يزن، إنت صرت فنّان! #salma #smile
من الرصيف لهون، ومن هون للعرس. وكله من باب قهوتكم. #yazan
خدي عودي القديم، اللي كنت أعزف عليه برّا بالبرد. علّقيه عالحيط، يمكن حدا تاني يلاقي حاله فيه. #yazan
-> END

// Level 3: newlyweds, arguing gently over money. One tag for the couple; the line names who speaks.
=== sara_noor_3 ===
سارة ونور، عرسان جداد، قاعدين عطاولة وحدة وبيحسبوا على [[محرمة]]. #sys
سارة: إذا ما اشترينا الكنباية [[هسّا]]، رح نضل قاعدين عالأرض لآخر الشهر. #sara_noor
نور: والأرض شو فيها؟ ستّي قعدت عالأرض ستين سنة، وعاشت مبسوطة. #sara_noor
فنجانين على حسابي. وبس تخلّصوا الحساب، احكولي مين ربح. #salma #smile
-> END

// Level 6: they're expecting a baby girl.
=== sara_noor_6 ===
سارة: عنّا خبر… بس لسّا ما حكينا لحدا. #sara_noor #smile
نور: يعني حكينا لنص الحارة. بس إنتو أول ناس بالقهوة. #sara_noor
لا! مبروك! ولد ولا بنت؟ #salma #surprised
سارة: بنت. ونور صار يحسب حق الجامعة من هسّا. #sara_noor #smile
أخيرًا، [[هوشة]] على فلوس تستاهل. #lana #smile
-> END

// Level 10: the café is the first place they'll bring her. Gives the little wooden chair.
=== sara_noor_10 ===
سارة: قرّرنا. أول مشوار إلها برّا البيت، رح يكون لهون. #sara_noor #smile
نور: وبنقعّدها على نفس الطاولة اللي كنّا نتخانق عليها عالمصاري. #sara_noor
وأنا بحجزلها الطاولة من هسّا. شو رح تسمّوها؟ #salma #smile
سارة: لسّا ما اتفقنا… بس اتفقنا على هالكرسي الخشب الصغير. خلّيه هون يا سلمى، يستنّاها. #sara_noor
-> END

// Level 3: he apologises for a letter he never delivered.
=== postman_3 ===
يا بنتي، بدّي أعتذر منك… وما بعرف من وين أبدا. #postman
تعتذر؟ عن شو يا عمّو؟ #salma #surprised
من سنين، كان في رسالة لهالقهوة، وأنا ما وصّلتها. #postman #sad
ضلّت على قلبي متل الحجر. عطيني وقت يا بنتي… اليوم بدّي فنجان وبس. #postman
-> END

// Level 6: the letter was to Teta, from Riyadh, 2007, from Nawal.
=== postman_6 ===
الرسالة اللي حكيتلك عنها… كانت لستّك. من الرياض، سنة 2007. #postman
من الرياض؟ سنة 2007؟ #salma #thinking
عالظرف، اسم المرسل: نوال. #postman
خالتي نوال… بعد الزعل بسنة. #salma #surprised
لقيتها ورا خزانة الفرز لما سكّروا المكتب القديم. إجيت عالباب تلات مرات، وما قدرت أدقّ. #postman #sad
-> END

// Level 10: he hands it over; Lana reads her mother's apology. Gives a wooden letterbox.
=== postman_10 ===
يا لانا، هاي إلك هسّا. كان لازم توصل من زمان… سامحيني. #postman
لانا بتفتح الظرف. خطّ أمّها، والتاريخ: الرياض، 2007. #sys
«يمّا، سامحيني إني رحت بلا وداع. الوعد بعده وعد… بس قولي لرنا إني مشتاقتلها.» #lana #sad
أمي كتبت لها، وجدّتي ما وصلها شي. ولا وحدة منهم [[درت]]. #lana #sad
ويا سلمى، هاد صندوق بريد خشب للباب. من اليوم، ولا رسالة بتضيع عن هالقهوة. #postman
-> END

// Level 3: she taught Rana at school and asks about her.
=== mrs_odeh_3 ===
إنتِ بنت رنا؟ والله نفس العيون. أنا علّمت إمّك عربي، بصف التاسع. #mrs_odeh #smile
الست عودة؟ ماما بتذكرك كتير. #salma #surprised
كيفها رنا؟ وليش ما بتيجي؟ هاي قهوة إمّها! #mrs_odeh
ماما… ما [[فاتت]] القهوة من زمان. #salma #sad
سلّميلي عليها، وقوليلها: الست عودة لسّا بتستنّى الواجب. #mrs_odeh #smile
-> END

// Level 6: Rana was her best pupil, and her angriest.
=== mrs_odeh_6 ===
إمّك كانت أشطر بنت عندي… وأعصب بنت كمان. #mrs_odeh
ماما؟ عصبية؟ ماما لما تزعل بتسكت. #salma #surprised
وهي صغيرة كانت تعلّي صوتها، وتبكي إذا حدا ظلم صاحبتها. السكوت تعلّمته بعدين. #mrs_odeh
حتى أمي… تسكت كذا بالضبط. #lana #thinking
اللي بيسكت هيك يا بنات، مش زعلان. موجوع. #mrs_odeh #sad
-> END

// Level 10: she calls Rana and asks her to the wedding. Gives her school bell.
=== mrs_odeh_10 ===
الست عودة طلّعت تلفونها، ودقّت رقم بتحفظه من غير ما تدوّر عليه. #sys
رنا؟ أنا الست عودة. في عرس بحارة إمّك، وبدّي إياكِ تقعدي جنبي. #mrs_odeh
ما طلبت منك تدخلي القهوة يا بنتي. العرس بالشارع. طلبت تقعدي جنب معلّمتك. #mrs_odeh
قالت: «بشوف». و«بشوف» من رنا، يعني آه. #mrs_odeh #smile
وهاد إلك يا سلمى: جرس المدرسة النحاس، رنّيته تلاتين سنة. علّقيه جنب الباب… يمكن يوم يرنّ لرنا. #mrs_odeh
-> END

// Level 3: a "normal customer" who knows too much about the menu. Plays before chapter 8.
=== faris_3 ===
الهيل عندكم فوق الستاندرد بشوي. اختيار جريء… بحترم الكونسبت. #faris
إنت شو، مفتّش صحة؟ #salma #surprised
لا لا، زبون عادي. بس بحب القهوة… كتير. #faris #smile
بس زمان، القهوة هون كانت تنقدّم مع كاسة مي قبلها، مش بعدها. #faris
…وإنت كيف بتعرف شو كان زمان؟ #salma #thinking
-> END

// Level 6: he grew up on Teta's knafeh. Waits for chapter 9 (storyFromChapter), after his offer in chapter 8.
=== faris_6 ===
قلتلكم إنّي كنت أجي مع أبوي كل جمعة. بس ما حكيتلكم شو كنت آكل. #faris
ستّكم كانت تعطيني قطعة الكنافة اللي عالطرف، المقرمشة، وتقلّي: هاي للشاطرين بس. #faris #smile
وبعدين صارت في فروعكم، صح؟ #lana
صارت. بس عمرها ما طلعت بنفس الطعم. قلتها لأبوي وأنا بعمر الستّطعش، وسكت… متل ما بيسكت [[هسّا]]. #faris #sad
يمكن الطعم ما كان بالوصفة يا فارس. كان بالإيد اللي بتعطيك القطعة. #salma
-> END

// Level 10: after the page, he offers beans for a Riyadh café. Waits for chapter 10. Gives his father's first grinder.
=== faris_10 ===
عرفت شو كلّفت صفقة أبوي ستّكم. تلاتطعشر سنة وهي تدفع، فنجان ورا فنجان. #faris #sad
وش تبي تقول يا فارس؟ #lana
إنّي ما بقدر أرجّع ولا قرش. بس إذا يوم فتحتوا بالرياض، أول شوال بن مني، والباقي بسعر الكلفة. #faris
وهاد بزنس ولا اعتذار؟ #salma #thinking
اعتذار. وخدي هاي، مطحنة أبوي الأولى، من أول دكّانة فتحها. مكانها هون، مش عنّا. #faris #smile
-> END

// Level 3: a retired woman from Jeddah, revisiting Amman. Hejazi, as in chapter 5, with her Amman years in it.
=== tourist_3 ===
ممكن أجلس جنب الشبّاك؟ وفنجان سادة، لو سمحتي. #tourist
أكيد. أول مرة ضيفة من جدّة بتطلب قهوتها سادة. #salma #smile
عشت في عمّان زمان، وأنا صبيّة. خمسين سنة في جدّة، وما نسيت طعم قهوتها. #tourist #smile
القهوة عندكم حلوة [[مرّة]]… زي ما كانت. #tourist #thinking
-> END

// Level 6: she asks about a man who used to wait at the window.
=== tourist_6 ===
يا بنتي، كان فيه رجّال يقعد هنا كل جمعة، قدّام الشبّاك… لسّا يجي؟ #tourist
عمّو أبو فادي؟ بييجي كل يوم! هاي طاولته، [[محجوزة]] إله. #salma
كل يوم…؟ لا تقولي له إنّي سألت. مو [[دحين]]. #tourist #sad
سلمى، وش القصة؟ #lana #thinking
ما بعرف… بس صار عندي سؤال لعمّو. #salma #thinking
-> END

// Level 10: she leaves a letter for Abu Fadi (season 2 teaser). Gives a small roshan.
=== tourist_10 ===
بكرة راجعة جدّة. بس قبل ما أمشي… هادي الرسالة لأبو فادي. #tourist
ليش ما بتعطيه إياها بإيدك؟ هو جاي بعد شوي! #salma #surprised
لأنّي لو شفته، ما راح أقدر أمشي. اعطيه إيّاها بعد ما أسافر. #tourist #sad
وهادا لك يا بنتي: [[روشان]] خشب صغيّر من بيوت جدّة القديمة. حطّيه جنب الشبّاك حقّه. #tourist #smile
-> END
