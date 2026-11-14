# فیگما Make روی شاخهٔ design

تاریخ: ۱۴۰۵/۰۸/۲۳  
ریپو: `https://github.com/RezaHakimi1921/arte-spport`  
شاخهٔ طراحی: `design`  
شاخهٔ پایدار: `main`

این سند می‌گوید لینک ریپو را چطور به Figma Make بدهی، طراحی را روی شاخهٔ `design` بگذاری، و بعد از پوش بگویی تا همین‌جا بررسی شود. منبع رفتار فیگما: راهنمای [Use Git with Figma Make](https://help.figma.com/hc/en-us/articles/44220165255063-Use-Git-with-Figma-Make-and-code-layers).

## این کار چیست

Make ریپوی عمومی باشگاه را کلون می‌کند، اپ را داخل فیگما اجرا می‌کند، و با گفتگو رابط را عوض می‌کند. تغییرها تا وقتی پوش نشوند روی گیت‌هاب نیستند. پوش باید روی شاخهٔ `design` باشد، نه روی `main`.

بعد از پوش، بررسی از روی همان شاخه در گیت انجام می‌شود: فرق با `main`، پالت، راست‌چین بودن، و اینکه API و رمزها دست نخورده باشند. لازم نیست لینک فریم بفرستی. کافی است بگویی شاخه پوش شد.

## این کار چه نیست

- فایل طراحی قبلی (`آرته اسپرت — طراحی باشگاه`) این مسیر نیست. آن فریم ثابت است.
- فایل خالی `https://www.figma.com/make/TFUSS6hG9eGT8hn9IOccMh` هم تا وقتی ریپو را کلون نکند همین مسیر نیست.
- چسباندن فقط آدرس عمومی، بدون اتصال حساب گیت‌هاب، پوش و پول‌ریکوئست نمی‌سازد. فیگما این را صریح گفته: کلون عمومی با URL برای دیدن است، برای برگرداندن شاخه نیست.
- Make اپ وب را عوض می‌کند. API دات‌نت، مهاجرت دیتابیس، و فایل راز سرور کار او نیست.

## شرط‌ها

ریپو این‌ها را دارد و Make قبولشان می‌کند:

- اپ وب در پوشهٔ `web/`
- فرمان توسعه: `npm run dev`
- فرمان ساخت: `npm run build`
- حجم ریپو خیلی کمتر از ۳ گیگابایت است
- ریپو عمومی است و VPN نمی‌خواهد

ریشهٔ ریپو خودِ اپ نیست. `package.json` داخل `web/` است. اگر Make پرسید برنامه کجاست، بگو:

```text
The React app is in web/. Dev script is npm run dev. Build script is npm run build. The API under src/ is not part of the UI.
```

اولین کلون چند فایل پیکربندی در `.figma/make` می‌سازد (`install`، `dev`، `deploy` و گاهی `.mise.toml`). این فایل‌ها خودبه‌خود به گیت‌هاب نمی‌روند. اگر شاخه را پوش کنی همراهش می‌آیند. در بررسی دیده می‌شوند. تا تأیید، به `main` نمی‌آیند.

هرکس اجازهٔ ویرایش فایل Make را داشته باشد کد کلون‌شده را می‌بیند، حتی اگر در گیت‌هاب به ریپو دسترسی نداشته باشد. فایل Make را عمومی نکن.

## اتصال گیت‌هاب

یک بار، با همان حسابی که ریپو مال اوست: `RezaHakimi1921`.

1. در Figma Make، زیر جعبهٔ پرامپت، Clone from GitHub یا Clone repository را بزن.
2. Connect to GitHub را بزن.
3. در گیت‌هاب Authorize را بزن. اگر چند حساب است، `RezaHakimi1921` را انتخاب کن. فیگما فقط یک حساب گیت‌هاب را برای این اتصال نگه می‌دارد.
4. برگرد به فیگما و Next را بزن.

اگر ریپو در فهرست نبود، کلون با URL به‌تنهایی کافی نیست برای پوش. اتصال حساب باید برقرار باشد. تیم فیگما روی پلن Professional فقط به یک سازمان گیت‌هاب وصل می‌شود. برای این ریپوی شخصی، اتصال همان حساب `RezaHakimi1921` است.

## کلون و شاخه

1. فایل Make را باز کن. اگر فایل تازه می‌خواهی، از فایل‌براوزر Make بساز.
2. Clone from GitHub.
3. ریپو را انتخاب کن، یا این آدرس را بگذار:

```text
https://github.com/RezaHakimi1921/arte-spport
```

4. شاخهٔ مبدأ را `main` بگذار. Clone را بزن و صبر کن. ممکن است چند دقیقه طول بکشد. اگر عامل فیگما اجازه خواست فایل پیکربندی بسازد، Run را بزن.
5. پیش از هر پرامپت طراحی، شاخهٔ تازه بساز. اسمش دقیقاً این باشد:

```text
design
```

از روی `main`. در ویرایشگر کد، نام شاخهٔ فعلی را بزن، Create branch، اسم `design`، مبدأ `main`.

یا همین را به عامل Make بگو:

```text
Create a branch called design based on main, and switch to it. Do not commit or push to main.
```

تا این شاخه ساخته نشده، هیچ پرامپت ظاهری نده. کار روی `main` یعنی پوش بعدی می‌تواند همان شاخهٔ زنده را عوض کند.

## پرامپت طراحی

بعد از اینکه پیش‌نمایش اپ بالا آمد و شاخه `design` است، این را بده:

```text
Redesign only the React app in web/. Keep Persian, dir rtl, and mobile-first.
Make it modern and calm. No pink cliché, no purple gradient, no fake statistics, no decorative cards.

Use this palette exactly:
page background #F4EDE8
brand and primary button #713F55 with white label
cards #FFFFFF
text and icons #343033
decoration only #D9A6AC and #B88C68, never for small text
present or success #2F6B4F
waiting #8A5A2F
error or cancel #8E2F3A
Every status needs words as well as color.

Screens to redesign:
password login and SMS code login
manager home as a work queue: a new cancellation and attendance still open
members and create member
Jalali calendar and session detail
attendance and staff punch in
inbox, with a seen action
student link with no receipt amount and no receipt image
student home, where the student can cancel only their own booking

Bottom navigation: خانه، شاگردها، تقویم، حضور، صندوق.
Do not build receipts or AvalAI.
Do not edit src/, tests/, deploy/.env, or secrets.
Stay on branch design.
```

اگر نتیجه هنوز ساده بود، به جای شلوغ کردن پالت، بگو کدام صفحه مدرن نیست. مثلاً:

```text
The home screen still looks like a plain form. Keep the same palette and the same actions, but make the hierarchy, spacing, and type feel like a current mobile product.
```

## پوش

وقتی خودت در پیش‌نمایش Make راضی بودی:

1. مطمئن شو شاخه هنوز `design` است، نه `main`.
2. به عامل بگو:

```text
Push my changes to the design branch. Do not push main. Open a pull request from design into main.
```

دکمهٔ Push changes همان کار را می‌کند. دکمهٔ Create PR فقط وقتی دیده می‌شود که شاخهٔ فعلی با شاخه‌ای که اول کلون شده فرق داشته باشد. به همین دلیل مبدأ `main` و کار روی `design` است.

پوش ممکن است یکی دو دقیقه طول بکشد. اگر نتوانست:

- اجازهٔ پوش به ریپو نیست. با حساب `RezaHakimi1921` وصل باش.
- `main` در گیت‌هاب از زمان کلون جلو رفته. به عامل بگو تغییرهای `main` را بگیرد و تداخل را حل کند، بعد دوباره روی `design` پوش کند.
- فقط URL عمومی را کلون کرده‌ای و حساب گیت‌هاب وصل نیست. پوش از این حالت ممکن نیست. از اول با Connect to GitHub کلون کن.

پول‌ریکوئست را در گیت‌هاب Ready for review نکن و مرج نکن. بررسی اینجاست، نه مرج.

## بررسی این طرف

بعد از پوش، در چت فقط این را بنویس:

```text
شاخهٔ design پوش شد
```

اگر شمارهٔ پول‌ریکوئست را دیدی، همان شماره را هم بگذار.

بررسی این‌هاست:

- فرق `design` با `main`
- تغییرها داخل `web/` باشند، جز فایل‌های `.figma/make` که خود Make ساخته
- `src/` و رازها دست نخورده باشند
- پالت همان رنگ‌های قفل‌شده باشد
- متن فارسی و `dir="rtl"` مانده باشد
- صفحه‌های لیست بالا وجود داشته باشند
- رسید و AvalAI اضافه نشده باشند
- اپ وب ساخته شود

نتیجه در چت گفته می‌شود: چه قبول است، چه باید در Make عوض شود. مرج به `main` فقط با گفتن صریح تو انجام می‌شود. استقرار سرور هم جداست و با این پوش انجام نمی‌شود.

## ترتیب یک‌باره

1. اتصال گیت‌هاب با `RezaHakimi1921`
2. کلون `https://github.com/RezaHakimi1921/arte-spport` از `main`
3. ساخت و سوییچ به شاخهٔ `design`
4. پرامپت طراحی
5. پوش `design` و ساخت پول‌ریکوئست، بدون مرج
6. پیام «شاخهٔ design پوش شد»
