"""Builds the public privacy and terms pages (privacy.ar.html, ...) from the texts below.

Run: python3 legal/build_pages.py. The platform serves the results at /games/gahwit-sitti/legal/{page}?lang=ar|en.
Texts in [brackets] must be filled in before the pages go live (legal/README.md).
"""
import html, pathlib

HERE = pathlib.Path(__file__).parent

STYLE = """
body { margin: 0; background: #FFF8F3; color: #3A2731; font: 17px/1.7 -apple-system, "Segoe UI", "Noto Naskh Arabic", Tahoma, sans-serif; }
main { max-width: 720px; margin: 0 auto; padding: 24px 16px 48px; }
h1 { color: #B84A60; font-size: 28px; margin: 8px 0 4px; }
h2 { font-size: 20px; margin: 28px 0 6px; }
p.meta { color: #7A6670; margin: 0 0 16px; }
ul { padding-inline-start: 22px; }
a { color: #1D6C67; }
"""

def page(lang, title, updated, sections):
    rtl = lang == "ar"
    body = []
    for heading, items in sections:
        body.append(f"<h2>{html.escape(heading)}</h2>")
        for item in items:
            if isinstance(item, list):
                body.append("<ul>" + "".join(f"<li>{html.escape(x)}</li>" for x in item) + "</ul>")
            else:
                body.append(f"<p>{html.escape(item)}</p>")
    other = "en" if rtl else "ar"
    switch = "English" if rtl else "العربية"
    return f"""<!doctype html>
<html lang="{lang}" dir="{'rtl' if rtl else 'ltr'}">
<head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>{html.escape(title)}</title><style>{STYLE}</style></head>
<body><main>
<p><a href="?lang={other}">{switch}</a></p>
<h1>{html.escape(title)}</h1>
<p class="meta">{html.escape(updated)}</p>
{''.join(body)}
</main></body></html>
"""

PRIVACY_EN = ("Gahwit Sitti: privacy policy", "Last updated: [publication date]", [
    ("Who we are", ["[Company legal name], [address] (\"we\") makes the game Gahwit Sitti (قهوة ستّي) and runs its servers. Contact us at [support email], or from Help in the game."]),
    ("What we collect", [[
        "An account id we create for you, and a secret that keeps you signed in on your phone.",
        "If you link Apple or Google: the id Apple or Google gives us for you. We never see your password.",
        "Your birth year (only the year), the game's language, and your country if your phone shares it.",
        "Your game progress, saved on your phone and, when you're online, on our servers.",
        "Purchases: the store's transaction id, the product, its price and date. Apple or Google handle your payment; we never see your card.",
        "How you play: events such as finishing a task or a memory, with your phone's model and the game's version, and error reports when something goes wrong.",
        "Messages you send from Help, and the email address if you choose to give one.",
        "Your network address, only for a moment, to stop abuse. We don't store it.",
    ]]),
    ("What we don't collect", ["We don't collect your location, contacts, photos, microphone or advertising id. The game has no open chat."]),
    ("Why we use it", [[
        "To run the game and keep your progress safe in the cloud.",
        "To deliver what you buy, restore it, and handle refunds.",
        "To keep rewards fair: our servers check them before giving gems.",
        "To improve the game, using play events and error reports.",
        "To answer you when you write to us.",
        "To follow the law, including age rules for children.",
    ]]),
    ("Ads", ["The game shows no ads today. If we add rewarded ads, they will always be your choice, we will name the ad partner here first, and players under 13 or whose age we don't know will get no personalised ads."]),
    ("Children", ["The game asks for your birth year when it first starts. Players under 13 get no personalised ads and no social features until a parent agrees. We collect no more than the game needs. A parent can ask us to delete a child's data at [support email]."]),
    ("Who we share it with", [[
        "Our hosting provider, [hosting provider], which stores our servers' data for us.",
        "Apple and Google, to check purchases and sign-ins.",
        "Nobody else, except when the law requires it. We never sell your data.",
    ]]),
    ("Where it is kept", ["On servers in [region], protected by encryption in transit and access controls. Every time our staff look at a player's account, it is recorded."]),
    ("How long we keep it", ["Until you delete your account. When you do (Settings, Delete my account), your account ends at once and we delete its data within 30 days. We keep records of purchases for accounting and tax, without your account linked to them. Play events may be kept longer only in a form that no longer identifies you."]),
    ("Your rights", ["You can see, correct or delete your data, and withdraw consent, by writing to [support email] or deleting your account in the game. These rights follow Saudi Arabia's Personal Data Protection Law, the UAE's Personal Data Protection Law and Egypt's Personal Data Protection Law (Law 151 of 2020). You may also complain to your country's data protection authority."]),
    ("Changes", ["If we change this policy, we will update the date above, and tell you in the game when the change matters."]),
])

PRIVACY_AR = ("قهوة ستّي: سياسة الخصوصية", "آخر تحديث: [تاريخ النشر]", [
    ("من نحن", ["[الاسم القانوني للشركة]، [العنوان] (\"نحن\") نصنع لعبة قهوة ستّي (Gahwit Sitti) ونشغّل خوادمها. للتواصل معنا: [بريد الدعم]، أو من «المساعدة» داخل اللعبة."]),
    ("ما نجمعه", [[
        "رقم حساب ننشئه لكِ، ورمزًا سريًّا يبقيكِ مسجّلة الدخول على هاتفك.",
        "إذا ربطتِ حساب Apple أو Google: المعرّف الذي تعطينا إياه Apple أو Google. لا نرى كلمة مرورك أبدًا.",
        "سنة ميلادك (السنة فقط)، ولغة اللعبة، وبلدك إن شاركه هاتفك.",
        "تقدّمك في اللعبة، محفوظًا على هاتفك، وعلى خوادمنا عند اتصالك بالإنترنت.",
        "المشتريات: رقم العملية من المتجر، والمنتج، وسعره، وتاريخه. تتولّى Apple أو Google الدفع، ولا نرى بطاقتك أبدًا.",
        "طريقة لعبك: أحداث مثل إنهاء مهمة أو ذكرى، مع طراز هاتفك ونسخة اللعبة، وتقارير الأخطاء عند حدوث مشكلة.",
        "الرسائل التي ترسلينها من «المساعدة»، وبريدك الإلكتروني إن اخترتِ كتابته.",
        "عنوان شبكتك، للحظات فقط، لمنع إساءة الاستخدام. لا نحفظه.",
    ]]),
    ("ما لا نجمعه", ["لا نجمع موقعك ولا جهات الاتصال ولا الصور ولا الميكروفون ولا معرّف الإعلانات. لا توجد في اللعبة دردشة مفتوحة."]),
    ("لماذا نستخدمها", [[
        "لتشغيل اللعبة وحفظ تقدّمك بأمان على السحابة.",
        "لتسليم ما تشترينه واستعادته ومعالجة طلبات الاسترداد.",
        "للعدل في المكافآت: تتحقق خوادمنا منها قبل منح الجواهر.",
        "لتحسين اللعبة باستخدام أحداث اللعب وتقارير الأخطاء.",
        "للرد عليكِ عندما تراسليننا.",
        "لاتباع القانون، ومنه قواعد أعمار الأطفال.",
    ]]),
    ("الإعلانات", ["لا تعرض اللعبة إعلانات اليوم. إن أضفنا إعلانات بمكافأة، فستكون دائمًا باختيارك، وسنذكر شريك الإعلانات هنا أولًا، ولن يحصل من هم دون 13 عامًا أو من لا نعرف أعمارهم على إعلانات مخصّصة."]),
    ("الأطفال", ["تسأل اللعبة عن سنة الميلاد عند أول تشغيل. من هم دون 13 عامًا لا يحصلون على إعلانات مخصّصة ولا على الميزات الاجتماعية حتى يوافق أحد الوالدين. لا نجمع أكثر مما تحتاجه اللعبة. يمكن لأحد الوالدين طلب حذف بيانات الطفل على [بريد الدعم]."]),
    ("مع من نشاركها", [[
        "مزوّد الاستضافة لدينا، [مزوّد الاستضافة]، الذي يحفظ بيانات خوادمنا نيابةً عنا.",
        "Apple وGoogle، للتحقق من المشتريات وتسجيل الدخول.",
        "لا أحد غيرهم، إلا إذا طلب القانون ذلك. لا نبيع بياناتك أبدًا.",
    ]]),
    ("أين تُحفظ", ["على خوادم في [المنطقة]، محميّة بالتشفير أثناء النقل وبضوابط الوصول. وكل اطّلاع من فريقنا على حساب لاعبة يُسجَّل."]),
    ("مدة الاحتفاظ", ["حتى تحذفي حسابك. عند الحذف (الإعدادات، احذفي حسابي) ينتهي حسابك فورًا ونحذف بياناته خلال 30 يومًا. نحتفظ بسجلات المشتريات لأغراض المحاسبة والضرائب دون ربطها بحسابك. قد نحتفظ بأحداث اللعب مدة أطول بصيغة لا تدل عليكِ."]),
    ("حقوقك", ["يمكنكِ الاطلاع على بياناتك أو تصحيحها أو حذفها، وسحب موافقتك، بمراسلتنا على [بريد الدعم] أو بحذف حسابك من اللعبة. تتبع هذه الحقوق نظام حماية البيانات الشخصية في المملكة العربية السعودية، وقانون حماية البيانات الشخصية في الإمارات، وقانون حماية البيانات الشخصية في مصر (القانون رقم 151 لسنة 2020). ويمكنكِ أيضًا تقديم شكوى إلى جهة حماية البيانات في بلدك."]),
    ("التغييرات", ["إذا غيّرنا هذه السياسة فسنحدّث التاريخ أعلاه، ونخبرك داخل اللعبة عندما يكون التغيير مهمًّا."]),
])

TERMS_EN = ("Gahwit Sitti: terms of use", "Last updated: [publication date]", [
    ("Agreement", ["By playing Gahwit Sitti you agree to these terms and to our privacy policy. If you are under the age of majority where you live, a parent or guardian must agree for you."]),
    ("Your account", ["Keep your phone and linked Apple or Google account safe. You can delete your account at any time from Settings."]),
    ("Gems, coins and other items", ["Gems, coins, energy and items in the game have no cash value and can't be exchanged for money or moved to another person. We may change their amounts or prices when we balance the game. Every pack lists exactly what it contains; nothing paid is random."]),
    ("Purchases", ["Apple or Google take your payment under their own terms. Refunds follow their policies and the law where you live. If a store refunds a purchase, we take back what it gave, as far as it hasn't been used."]),
    ("Fair play", ["Don't cheat, change the game, use bots, or harm the game, its servers or other players. We may suspend accounts that do."]),
    ("Our content", ["The game, its story, characters, art, music and code belong to [Company legal name] or its licensors. You may play it for your own enjoyment."]),
    ("Changes and availability", ["We may update the game, change events and features, or stop the service, telling you in the game beforehand when we can. Events need an internet connection."]),
    ("Responsibility", ["We provide the game as it is. As far as the law allows, we are not responsible for indirect losses. Nothing here limits rights the law gives you that can't be limited."]),
    ("Law", ["These terms follow the laws of [jurisdiction], without taking away protections your own country's law gives you."]),
    ("Contact", ["[support email], or Help in the game."]),
])

TERMS_AR = ("قهوة ستّي: شروط الاستخدام", "آخر تحديث: [تاريخ النشر]", [
    ("الموافقة", ["باللعب بقهوة ستّي توافقين على هذه الشروط وعلى سياسة الخصوصية. إن كنتِ دون سن الرشد في بلدك، فيجب أن يوافق أحد الوالدين أو وليّ الأمر نيابةً عنك."]),
    ("حسابك", ["حافظي على هاتفك وعلى حساب Apple أو Google المرتبط. يمكنكِ حذف حسابك في أي وقت من الإعدادات."]),
    ("الجواهر والعملات وغيرها", ["الجواهر والعملات والطاقة والقطع في اللعبة ليست لها قيمة نقدية، ولا يمكن استبدالها بالمال ولا نقلها إلى شخص آخر. قد نغيّر كمياتها أو أسعارها عند موازنة اللعبة. كل حزمة تذكر محتواها بالضبط، ولا شيء مدفوعًا عشوائي."]),
    ("المشتريات", ["تتولّى Apple أو Google الدفع وفق شروطهما. يتبع الاسترداد سياستهما والقانون في بلدك. إن استردّ المتجر عملية شراء، نستعيد ما منحته بقدر ما لم يُستخدم."]),
    ("اللعب العادل", ["لا تغشّي، ولا تعدّلي اللعبة، ولا تستخدمي برامج آلية، ولا تُضرّي باللعبة أو خوادمها أو اللاعبات الأخريات. قد نوقف الحسابات التي تفعل ذلك."]),
    ("محتوانا", ["اللعبة وقصتها وشخصياتها ورسومها وموسيقاها وشيفرتها ملك لـ[الاسم القانوني للشركة] أو للجهات المرخِّصة لها. يمكنكِ اللعب بها لمتعتك الشخصية."]),
    ("التغييرات والتوفّر", ["قد نحدّث اللعبة أو نغيّر الفعاليات والميزات أو نوقف الخدمة، ونخبرك داخل اللعبة مسبقًا متى أمكن. تحتاج الفعاليات إلى اتصال بالإنترنت."]),
    ("المسؤولية", ["نقدّم اللعبة كما هي. وبقدر ما يسمح القانون، لسنا مسؤولين عن الخسائر غير المباشرة. لا شيء هنا يحدّ من حقوق يمنحك إياها القانون ولا يجوز الحدّ منها."]),
    ("القانون", ["تخضع هذه الشروط لقوانين [الجهة القضائية]، دون أن تُسقط الحماية التي يمنحك إياها قانون بلدك."]),
    ("التواصل", ["[بريد الدعم]، أو «المساعدة» داخل اللعبة."]),
])

for name, (title, updated, sections), lang in [
    ("privacy.en.html", PRIVACY_EN, "en"), ("privacy.ar.html", PRIVACY_AR, "ar"),
    ("terms.en.html", TERMS_EN, "en"), ("terms.ar.html", TERMS_AR, "ar")]:
    (HERE / name).write_text(page(lang, title, updated, sections), encoding="utf-8")
    print("wrote", name)
