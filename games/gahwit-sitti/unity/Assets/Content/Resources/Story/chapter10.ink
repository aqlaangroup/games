// Chapter 10: عرس بالحارة · A wedding in the neighbourhood
// Every knot here was drafted by Claude Code from STORY_BIBLE.md (sections 3–8) and waits for the Amman and
// Najdi dialect writers (CLAUDE.md, writing style). The speaker is the first tag on a line; the second is the expression.
// [[word]] marks a tap-to-explain word; its meaning is in the Glossary table.
// chapter10_en.ink (English) and chapter10_ar.ink (neutral-Arabic subtitles) follow this file line for line
// (the parity test in Story/Tests checks it). Change this file first, then those two.
// The family table stands outside on the pavement: Rana still won't step into the café. She calls Teta "your Teta"
// until notify_6, the season's last line, where she says "my mother" for the first time (STORY_BIBLE.md section 10).

// The saved choice for a knot ("" before it's made), bound by the game (STORY_BIBLE.md section 8).
EXTERNAL chose(knot)

// The callback for c2_groom, chosen in chapter 2's basil_pots. The game saves a choice under the knot it was
// made in (CafeScene.PlayScene), so it reads chose("basil_pots"); the ids are honest and polite (STORY_BIBLE.md 8).
=== wedding_lights ===
شايفين الأضواء؟ مش قلتلكم إنه ابن حلال؟ أنا جبته عالقهوة، يعني أنا زوّجته للدكتورة هبة! #samira #smile
{
- chose("basil_pots") == "honest":
    وإنتِ يا سلمى قلتيله الحقيقة بوجهه، وأنا زعلت… طلع أحسن إشي عملتيه. #samira
- else:
    وإنتِ قلتيله: القهوة مفتوحة إلك أي وقت. صدّق المسكين، وإجا بالليل… ولقى الدكتورة! #samira
}
يعني إنتِ اللي زوّجتيهم يا خالتو؟ #salma #smile
طبعًا! ووحياة ولادي، العرس الجاي عرسك. #samira
-> END

=== street_arch ===
قوس الورد صار على باب القهوة، وأم رامي وصلت بأول قِدر. #sys
طلبيّة العرس كلها عليّ: [[منسف]] للحارة كلها. أول طلبيّة كبيرة إلي. #um_rami #smile
وش تحتاجين؟ قولي بس. #lana
ولا إشي يا حبيبتي. اليوم إنتو ضيوف. #um_rami
تحت قوس ورد، ومنسف أم رامي… ستّي كانت رقصت. #salma #smile
-> END

// The page's recipe (chapter 9, m9_page), made right at last; Abu Fadi tasted the first tray in 1974.
=== dessert_table ===
عمّو، دوق. وقلّي الحقيقة، مش مجاملة. #salma
أبو فادي أكل لقمة، وسكت. وأكل التانية. #sys
هاي هي. نفس كنافة صيف 74… حتى حبّة الهيل بالقطر. #abu_fadi #smile
يعني ضبطت؟ أخيرًا! #lana #smile
-> END

// Nawal: Amman grammar with Najdi words. Rana: formal when hurt, warm when she forgets (voice sheets).
=== family_table ===
نوال وصلت من الرياض قبل المغرب. #sys
سلمى؟ يا الله… بتشبهي أمّي [[مرّة]]. #nawal #smile
رنا وصلت متأخرة، بعد الزفّة. وقفت عند باب القهوة، وما فاتت. #sys
حطّيتوا الطاولة برّا… عشاني؟ #rana #surprised
خالتي رنا… أنا لانا. #lana
بعرف. ستّك كانت تبعتلي صورك كل عيد. #rana #sad
-> END

// "The first cup is for the guest, the second for talk" (chapter 1, sign).
=== two_chairs ===
كرسيين جنب بعض. قعدت نوال، وبعد شوي قعدت رنا. ولا وحدة حكت. #sys
أمّك قرت الصفحة؟ #lana
مرّتين. وما حكت ولا كلمة. #salma
وش نسوي الحين؟ #lana #thinking
ولا إشي. ستّي كانت تقول: الفنجان الأول للضيف، والتاني للحكي. #salma
-> END

// The cousins stay partners and plan a café in Riyadh. The last two lines are the season 2 teaser: a letter
// from Jeddah for Abu Fadi (the woman he waited for left Amman in 1976; STORY_BIBLE.md sections 2 and 9). The task's
// scene plays before memory 10, so Abu Fadi pockets the letter and m10_lead picks it up.
=== wedding_night ===
يزن عزف، والحارة كلها رقصت [[دبكة]] تحت الأضواء، والعرسان بالوسط. #sys
سلمى… ما أبغى أبيع نصّي. #lana
ولا أنا. يعني… بارتنرز؟ لا، أخوات. #salma #smile
أخوات. وبعدين؟ قهوة ستّي، فرع الرياض. #lana #smile
وبين الزغاريد، وصلت رسالة لأبو فادي. على الظرف طابع من جدّة. #sys
عرف الخطّ من أول نظرة، وحطّها بجيبته من غير ما يفتحها. #sys
-> END

// Memory 10 (pour): in the present, not 1974 (STORY_BIBLE.md section 6). Salma and Lana make the two cups Teta's
// voice note asked for (voice_1), and each carries one to the other's mother.

=== m10_lead ===
الرسالة بتستنّى. الليلة إلها شغل تاني. #abu_fadi
ستّكم وصّت على فنجانين. يلّا يا بنات، جيبوا الدلّة الصغيرة. #abu_fadi #smile
-> END

=== m10_intro ===
فنجانين، متل ما وصّت ستّي: واحد إلي، وواحد إلك. #salma
لا. إنتِ تصبّين لأمّي، وأنا أصبّ لأمّك. #lana
متل الضيوف. الفنجان الأول للضيف. #salma #smile
-> END

// Echoes m1_perfect.
=== m10_perfect ===
هيك بالضبط. صبر وإيد ثابتة… متلها. #abu_fadi #smile
-> END

// Echoes m1_ok.
=== m10_ok ===
ستّي كانت تقول: القهوة بتسامح، متل الناس الطيبين. #salma #smile
-> END

// No speeches: one question that isn't a question, two words, and a hand.
=== m10_outro ===
سلمى حطّت الفنجان قدّام خالتها نوال، ولانا قدّام خالتها رنا. #sys
عشرين سنة يا نوال… #rana #sad
وعدتها. #nawal
رنا ما ردّت. مدّت إيدها عالطاولة، ومسكت إيد أختها. #sys
-> END

// Letter 6 is Abu Fadi's to give; he leaves it in the counter drawer, where the game finds letters.
=== m10_return ===
ستّكم قالتلي: ما رح يقعدوا سوا إلا بعرس. ولمّا يقعدوا، أعطيهم هاي. #abu_fadi
حطّيتها بالدرج يا بنات، مطرح أول رسالة. #abu_fadi #smile
-> END

// Letter 6, to Salma and Lana: the café was always meant to carry her girls' names (memory 1, m1_outro). They
// add «رنا ونوال» under Teta's sign; notify_6 shows it done.
=== letter_6 ===
لسلمى ولانا، #letter
إذا وصلتكم هالورقة، يعني أمّهاتكم شربوا قهوة على طاولة وحدة. الحمد لله. #letter
سنة 74 وعدت أبو فادي أسمّي القهوة على اسم ولادي، وما كتبت اسمهم ولا مرة. #letter
كنت ناطرة أشوفهم سوا. هسّا إنتو شفتوهم عنّي. #letter
اكتبوا تحت الآرمة: رنا ونوال. بخطّ إيدكم، مش بخطّ حدا غريب. #letter
والدلّة الصغيرة إلكم التنتين. صبّوا لبعض، وما حدا يعدّ. #letter
— ستّكم #letter_signature
-> END

// Teta's voice note after letter 6: a blessing for the wedding. Needs the voice actor (missing/14).
=== voice_6 ===
يا روحي، الأعراس بتلمّ اللي فرّقه الزعل. ألف مبروك للعرسان، مين ما كانوا. #teta
-> END

// The season's last line. Salma sent her mother a photo of the sign.
=== notify_6 ===
وصلتني الصورة: اسمي واسم أختي تحت آرمة أمّي. بكرا الصبح بفوت. #rana
-> END
