# دليل النشر والتشغيل في الإنتاج

يشرح هذا الدليل تشغيل منصة ديارك على خادم واحد باستخدام Docker. القرار المعماري موثّق في ADR-0060.

## المكوّنات

```
الإنترنت ──► Caddy (HTTPS تلقائي) ──► portal (Next.js) ──► api (.NET) ──► PostgreSQL
                     └──────────────────────────────────► api (للتطبيقات لاحقاً)
migrations: تُطبَّق تلقائياً قبل تشغيل الـAPI في كل نشر
backup: نسخة يومية من قاعدة البيانات والصور مع حذف الأقدم من 14 يوماً
```

## 1. متطلبات الخادم

- **نظام التشغيل:** Ubuntu 24.04 أو أحدث، مع Docker Engine و Docker Compose v2.
- **الحد الأدنى للبداية:** معالجان (vCPU)، وذاكرة 4 GB، ومساحة SSD بحجم 40 GB.
- **المنافذ المفتوحة:** 80 و443 فقط. لا تفتح منفذ قاعدة البيانات (5432).
- **عند اختيار مزوّد الاستضافة وشبكة CDN:** تأكد أن شروط استخدامهما تسمح بخدمة مستخدمين في سوريا.

## 2. النطاق (DNS)

نطاق المنصة هو `diyarak.app`. أنشئ في لوحة مسجّل النطاق السجلات التالية، وكلها تشير إلى عنوان IP الخادم. أضف `AAAA` بالأسماء نفسها إذا كان للخادم عنوان IPv6:

| النوع | الاسم | القيمة | الاستخدام |
|---|---|---|---|
| `A` | `@` | عنوان IP الخادم | الموقع `diyarak.app` |
| `A` | `www` | عنوان IP الخادم | يُحوَّل تلقائياً إلى `diyarak.app` |
| `A` | `api` | عنوان IP الخادم | الـAPI `api.diyarak.app` |

يحصل Caddy على شهادات HTTPS ويجدّدها تلقائياً بمجرد أن يشير النطاق إلى الخادم.

> نطاقات `.app` تعمل عبر HTTPS فقط، فالمتصفحات ترفض فتحها عبر HTTP. لذلك لن يفتح الموقع قبل أن يصدر Caddy الشهادة. تأكد أن المنفذين 80 و443 مفتوحان قبل التشغيل الأول.

للتحقق من أن السجلات وصلت قبل التشغيل:

```bash
dig +short diyarak.app www.diyarak.app api.diyarak.app
```

### خرائط المواقع

تعرض الخرائط بلاطات OpenStreetMap العامة افتراضياً. هذا يكفي للإطلاق والزيارات القليلة، لكن سياسة استخدام OpenStreetMap تطلب ممن تكثر زياراته استخدام مزوّد بلاطات خاص به. عند ازدياد الزيارات:

1. أنشئ حساباً لدى مزوّد بلاطات، مثل MapTiler أو Stadia Maps.
2. ضع رابط البلاطات في `MAP_TILE_URL` داخل `deploy/.env`.
3. ضع نص حقوق الخريطة الذي يطلبه المزوّد في `MAP_ATTRIBUTION`.
4. أعد تشغيل `portal` لتطبيق الإعداد. لا حاجة لإعادة البناء.

لا يرى الزوار الموقع الدقيق للعقار أبداً، بل منطقة تقريبية بنصف قطر 500 متر (ADR-0065).

### تنبيهات البحث المحفوظ برسائل SMS

هذه التنبيهات مطفأة افتراضياً، لأن كل رسالة مدفوعة. لتفعيلها:

1. ضع `SavedSearchAlerts__Enabled=true` في `deploy/api.env`.
2. اضبط `SavedSearchAlerts__PortalUrl` على عنوان الموقع.
3. تأكد أن مزوّد الرسائل يستخدم `{message}` في إعداداته، فـTwilio مثلاً يستخدمه.

بعد التفعيل يتلقى المستخدم الذي فعّل التنبيه رسالة واحدة يومياً على الأكثر، بين الساعة 10 صباحاً و8 مساءً بتوقيت دمشق (ADR-0070).

## 3. التثبيت لأول مرة

```bash
git clone https://github.com/raedalkherait-source/Diyarak-Platform.git
```

```bash
cd Diyarak-Platform
```

```bash
cp deploy/.env.example deploy/.env
```

```bash
cp deploy/api.env.example deploy/api.env
```

بعد النسخ، عدّل الملفين:

- **`deploy/.env`:** النطاقان (مضبوطان مسبقاً على `diyarak.app` و`api.diyarak.app`)، والبريد الإلكتروني لإشعارات الشهادات، و`POSTGRES_PASSWORD`. أنشئ كلمة المرور بالأمر `openssl rand -base64 32`.
- **`deploy/api.env`:**
  - `Authentication__Phone__Issuer`: عنوان الـAPI العام `https://api.diyarak.app`.
  - `Authentication__Phone__SigningKey`: أنشئه بالأمر `openssl rand -base64 32`.
  - إعدادات مزوّد الرسائل `Messaging__Sms__Http__*` حسب توثيق المزوّد. ارجع إلى قسم «SMS gateway» في `development.md`.
  - أرقام المشرفين في `Moderation__ModeratorPhoneNumbers__0` وما بعده.

اجعل الملفين مقروءين للمستخدم الحالي فقط، ولا تضفهما إلى git، فهما مُستثنيان في `.gitignore`:

```bash
chmod 600 deploy/.env deploy/api.env
```

ثم شغّل المنصة:

```bash
docker compose -f deploy/compose.production.yaml --env-file deploy/.env up -d --build
```

تحقق من الحالة:

```bash
docker compose -f deploy/compose.production.yaml --env-file deploy/.env ps
```

النتيجة المتوقعة:

- `migrations` في حالة `Exited (0)`.
- `postgres` و`api` و`portal` في حالة `healthy`.
- `caddy` و`backup` في حالة `Up`.

اختبر من جهازك:

```bash
curl -s https://api.diyarak.app/health/ready
```

ثم افتح `https://diyarak.app`، وسجّل الدخول برقم مشرف، وافتح `/admin`.

## 4. نشر تحديث

```bash
git pull
```

```bash
docker compose -f deploy/compose.production.yaml --env-file deploy/.env up -d --build
```

خطوات النشر بالترتيب:

1. تُبنى الصور الجديدة.
2. تُطبَّق الـmigrations الجديدة.
3. إن فشلت الـmigrations، لا يتغيّر الـAPI.
4. إن نجحت، يُعاد تشغيل الـAPI ثم الواجهة.

## 5. الرجوع إلى إصدار سابق

```bash
git checkout <الإصدار-السابق>
```

```bash
docker compose -f deploy/compose.production.yaml --env-file deploy/.env up -d --build
```

إن كان الإصدار الجديد قد أضاف migrations غيّرت شكل البيانات، فاستعد نسخة احتياطية من قبل النشر (القسم 6). لا تعتمد على الرجوع بالكود وحده.

## 6. النسخ الاحتياطي والاستعادة

- النسخ موجودة في volume اسمه `diyarak_backups`:
  - قاعدة البيانات في `db/`.
  - الصور في `media/`.
- **انسخها دورياً إلى مكان خارج الخادم.** النسخة على الخادم نفسه لا تحميك إن تعطّل الخادم.

عرض النسخ الموجودة:

```bash
docker compose -f deploy/compose.production.yaml --env-file deploy/.env exec backup ls -la /backups/db /backups/media
```

استعادة قاعدة البيانات من نسخة معيّنة (أوقف الـAPI أولاً):

```bash
docker compose -f deploy/compose.production.yaml --env-file deploy/.env stop api portal
```

```bash
docker compose -f deploy/compose.production.yaml --env-file deploy/.env exec backup pg_restore --clean --if-exists --no-owner -d diyarak /backups/db/diyarak-<التاريخ>.dump
```

```bash
docker compose -f deploy/compose.production.yaml --env-file deploy/.env start api portal
```

**جرّب الاستعادة على قاعدة اختبار مرة كل شهر على الأقل.** نسخة احتياطية لم تُجرَّب استعادتها لا يمكن الاعتماد عليها.

## 7. المراقبة اليومية

السجلات:

```bash
docker compose -f deploy/compose.production.yaml --env-file deploy/.env logs --tail 200 api
```

ما يجب متابعته:

- **فشل إرسال الرموز:** أخطاء `identity.phone.delivery_failed` المتكررة تعني مشكلة لدى مزوّد الرسائل أو في رصيدك عنده.
- **مساحة القرص:** الصور والنسخ الاحتياطية تكبر مع الوقت.
- **بلاغات المستخدمين:** راجعها يومياً من `/admin`.

## 8. ملاحظات أمنية

- **الأسرار موجودة فقط في `deploy/.env` و`deploy/api.env` على الخادم**: كلمة مرور قاعدة البيانات، ومفتاح توقيع الجلسات، ومفتاح مزوّد الرسائل.
- **تغيير `Authentication__Phone__SigningKey` يُخرج جميع المستخدمين.** استخدمه إن شككت بتسريب المفتاح.
- **الـAPI لا يثق بعنوان IP يرسله الزائر.** يقبل العنوان المُمرَّر فقط من داخل شبكة Docker الداخلية `172.28.0.0/24`، وCaddy يكتب هذا العنوان بنفسه.

## 9. ما يلزم لاحقاً عند نمو المنصة

- **تخزين الصور:** نقلها إلى تخزين كائنات متوافق مع S3، مع CDN أمامه.
- **قاعدة البيانات:** خادم مستقل مع نسخ احتياطية مُدارة.
- **المراقبة:** أداة لتجميع السجلات ومراقبة توفر الخدمة.
