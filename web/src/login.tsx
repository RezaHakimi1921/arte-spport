import { useState, type FormEvent, type ReactNode } from "react";
import { api } from "./api";

type IconName = "alert" | "arrow" | "check" | "eye" | "eyeOff" | "lock" | "phone" | "shield" | "user";

function Icon({ name }: { name: IconName }) {
  const paths: Record<IconName, ReactNode> = {
    alert: <><circle cx="12" cy="12" r="9" /><path d="M12 7v6M12 17h.01" /></>,
    arrow: <path d="m9 18 6-6-6-6" />,
    check: <path d="m5 12 4 4L19 6" />,
    eye: <><path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6S2 12 2 12Z" /><circle cx="12" cy="12" r="2.5" /></>,
    eyeOff: <path d="m3 3 18 18M10.6 6.2A10 10 0 0 1 12 6c6.5 0 10 6 10 6a17 17 0 0 1-2.1 2.8M6.7 6.7C3.7 8.5 2 12 2 12s3.5 6 10 6a9.8 9.8 0 0 0 3.3-.6M10.2 10.2a2.6 2.6 0 0 0 3.6 3.6" />,
    lock: <><rect x="4" y="10" width="16" height="11" rx="3" /><path d="M8 10V7a4 4 0 0 1 8 0v3" /></>,
    phone: <><rect x="6" y="2" width="12" height="20" rx="3" /><path d="M10 18h4" /></>,
    shield: <path d="M12 22s8-3.8 8-10V5l-8-3-8 3v7c0 6.2 8 10 8 10Z" />,
    user: <><circle cx="12" cy="8" r="4" /><path d="M4 21v-1a7 7 0 0 1 16 0v1" /></>,
  };
  return (
    <svg data-icon={name} viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      {paths[name]}
    </svg>
  );
}

function Mark() {
  return (
    <div className="lg-logo" aria-hidden="true">
      <svg viewBox="0 0 32 32">
        <circle cx="16" cy="16" r="7" />
        <path d="M16 4v4M16 24v4M4 16h4M24 16h4" />
      </svg>
    </div>
  );
}

function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <main className="lg-page">
      <section className="lg-story" aria-label="معرفی باشگاه">
        <div className="lg-story-top"><Mark /><span>آرتِه اسپرت</span></div>
        <div className="lg-story-copy">
          <span className="lg-eyebrow"><span className="lg-status-dot" />همراه مدیر باشگاه</span>
          <h1>کلاس سر وقت،<br />حضور <em>روشن.</em></h1>
          <p>شاگرد با موبایل وارد می‌شود. جلسه، حضور و لغو را همین‌جا می‌بینید.</p>
        </div>
        <div className="lg-workflow" aria-hidden="true">
          <div className="lg-workflow-head">
            <div><span>نمونهٔ یک روز</span><strong>جریان کلاس</strong></div>
            <span className="lg-live">ساعت ۱۰</span>
          </div>
          <div className="lg-workflow-row">
            <div className="lg-workflow-icon"><Icon name="user" /></div>
            <div className="lg-workflow-info"><strong>یوگا</strong><span>سالن آینه</span></div>
            <span className="lg-workflow-state">منتظر حضور</span>
          </div>
          <div className="lg-track"><span /></div>
          <div className="lg-workflow-meta">
            <span><i className="lg-dot done" /> رزرو</span>
            <span><i className="lg-dot active" /> حضور</span>
            <span><i className="lg-dot" /> پایان</span>
          </div>
        </div>
        <div className="lg-story-foot"><span><Icon name="shield" /> هر باشگاه فقط داده‌ی خودش را می‌بیند</span></div>
      </section>
      <section className="lg-auth">
        <header className="lg-mobile-header">
          <div className="lg-mobile-brand"><Mark /><span>آرتِه اسپرت</span></div>
        </header>
        <div className="lg-content">{children}</div>
      </section>
    </main>
  );
}

function Heading({ icon, over, title, children }: { icon: IconName; over: string; title: string; children?: ReactNode }) {
  return (
    <>
      <div className="lg-welcome-icon"><Icon name={icon} /></div>
      <div className="lg-heading">
        <span className="lg-overline">{over}</span>
        <h2>{title}</h2>
        {children && <p>{children}</p>}
      </div>
    </>
  );
}

function ErrorBox({ text }: { text: string }) {
  if (!text) return null;
  return <div className="lg-error" role="alert"><Icon name="alert" /><span>{text}</span></div>;
}

function Submit({ busy, label }: { busy: boolean; label: string }) {
  return (
    <button className="lg-submit" type="submit" disabled={busy} aria-busy={busy}>
      <span>{busy ? "لطفاً صبر کنید…" : label}</span>
      {busy ? <span className="lg-spinner" aria-hidden="true" /> : <Icon name="arrow" />}
    </button>
  );
}

const digits = (value: string) => value.replace(/[۰-۹]/g, (d) => String("۰۱۲۳۴۵۶۷۸۹".indexOf(d))).replace(/[٠-٩]/g, (d) => String("٠١٢٣٤٥٦٧٨٩".indexOf(d))).replace(/\D/g, "");
const isMobile = (value: string) => /^09\d{9}$/.test(digits(value));
const message = (e: unknown) => (e instanceof Error ? e.message : "ارتباط با سرور برقرار نشد.");

export function Login({ onDone }: { onDone: () => Promise<void> }) {
  const [mode, setMode] = useState<"phone" | "password" | "code">("password");
  const [phone, setPhone] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [code, setCode] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (busy) return;
    setError("");
    if (mode === "phone") {
      if (!isMobile(phone)) { setError("شماره موبایل را درست وارد کنید (۱۱ رقم، با ۰۹)."); return; }
      setBusy(true);
      try {
        await api("/api/v1/auth/otp/request", { method: "POST", body: JSON.stringify({ mobile: digits(phone) }) });
        setMode("code");
        setCode("");
      } catch (err) { setError(message(err)); }
      finally { setBusy(false); }
      return;
    }
    if (mode === "code") {
      if (digits(code).length !== 5) { setError("کد ۵ رقمی پیامک‌شده را وارد کنید."); return; }
      setBusy(true);
      try {
        await api("/api/v1/auth/otp/verify", { method: "POST", body: JSON.stringify({ mobile: digits(phone), code: digits(code) }) });
        await onDone();
      } catch (err) { setError(message(err)); }
      finally { setBusy(false); }
      return;
    }
    if (username.trim().length < 3 || password.length < 8) { setError("نام کاربری و رمز را وارد کنید."); return; }
    setBusy(true);
    try {
      await api("/api/v1/auth/login", { method: "POST", body: JSON.stringify({ username: username.trim(), password }) });
      await onDone();
    } catch (err) { setError(message(err)); }
    finally { setBusy(false); }
  }

  return (
    <AuthLayout>
      {mode === "code" ? (
        <Heading icon="shield" over="تأیید شماره" title="کد تأیید را وارد کنید">
          کد ۵ رقمی ارسال‌شده به <bdi dir="ltr">{digits(phone)}</bdi> را بنویسید.
        </Heading>
      ) : (
        <Heading icon="user" over="خوش آمدید" title="وارد حساب خود شوید">
          با نام کاربری، یا با شمارهٔ موبایلی که در باشگاه ثبت شده.
        </Heading>
      )}

      {mode !== "code" && (
        <div className="lg-segment" role="tablist" aria-label="روش ورود">
          <button className={mode === "password" ? "active" : ""} type="button" role="tab" aria-selected={mode === "password"} onClick={() => { setMode("password"); setError(""); }}>رمز عبور</button>
          <button className={mode === "phone" ? "active" : ""} type="button" role="tab" aria-selected={mode === "phone"} onClick={() => { setMode("phone"); setError(""); }}>کد پیامکی</button>
        </div>
      )}

      <form onSubmit={submit} noValidate>
        {mode === "password" && (
          <>
            <label className="lg-label" htmlFor="lg-user">موبایل یا نام کاربری</label>
            <div className="lg-input">
              <span className="lg-input-icon"><Icon name="user" /></span>
              <input id="lg-user" autoComplete="username" dir="ltr" value={username} onChange={(e) => { setUsername(e.target.value); setError(""); }} />
            </div>
            <div className="lg-label-row">
              <label className="lg-label" htmlFor="lg-password">رمز عبور</label>
            </div>
            <div className="lg-input">
              <span className="lg-input-icon"><Icon name="lock" /></span>
              <input id="lg-password" type={showPassword ? "text" : "password"} autoComplete="current-password" dir="ltr" value={password}
                onChange={(e) => { setPassword(e.target.value); setError(""); }} />
              <button className="lg-reveal" type="button" onClick={() => setShowPassword(!showPassword)} aria-label={showPassword ? "پنهان کردن رمز" : "نمایش رمز"}>
                <Icon name={showPassword ? "eyeOff" : "eye"} />
              </button>
            </div>
          </>
        )}

        {mode === "phone" && (
          <>
            <label className="lg-label" htmlFor="lg-phone">شماره موبایل</label>
            <div className={`lg-input${error && !isMobile(phone) ? " invalid" : ""}`}>
              <span className="lg-input-icon"><Icon name="phone" /></span>
              <input id="lg-phone" type="tel" inputMode="numeric" autoComplete="tel" dir="ltr" maxLength={11} value={phone}
                onChange={(e) => { setPhone(digits(e.target.value).slice(0, 11)); setError(""); }} placeholder="09120000000" />
              {isMobile(phone) && <span className="lg-valid"><Icon name="check" /></span>}
            </div>
          </>
        )}

        {mode === "code" && (
          <div className="lg-code-area">
            <label className="lg-label" htmlFor="lg-code">کد تأیید ۵ رقمی</label>
            <input className="lg-code" id="lg-code" inputMode="numeric" autoComplete="one-time-code" dir="ltr" maxLength={5} value={code}
              onChange={(e) => { setCode(digits(e.target.value).slice(0, 5)); setError(""); }} />
            <div className="lg-code-actions">
              <button type="button" className="lg-text" onClick={() => { setMode("phone"); setError(""); }}>تغییر شماره</button>
            </div>
          </div>
        )}

        <ErrorBox text={error} />
        <Submit busy={busy} label={mode === "phone" ? "دریافت کد ورود" : mode === "code" ? "تأیید و ورود" : "ورود به حساب"} />
      </form>
    </AuthLayout>
  );
}

export function Setup({ onDone }: { onDone: () => Promise<void> }) {
  const [name, setName] = useState("");
  const [branchName, setBranchName] = useState("شعبه مرکزی");
  const [ownerName, setOwnerName] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (name.trim().length < 2 || branchName.trim().length < 2) { setError("نام باشگاه و شعبه را بنویسید."); return; }
    setBusy(true);
    setError("");
    try {
      await api("/api/v1/brands", { method: "POST", body: JSON.stringify({ name: name.trim(), branchName: branchName.trim(), ownerName: ownerName.trim() }) });
      await onDone();
    } catch (err) { setError(message(err)); }
    finally { setBusy(false); }
  }

  return (
    <AuthLayout>
      <Heading icon="shield" over="اولین ورود" title="ساخت باشگاه">
        این حساب هنوز به شعبه‌ای وصل نیست. نام باشگاه را بنویسید تا میز کار باز شود.
      </Heading>
      <form onSubmit={submit} noValidate>
        <label className="lg-label" htmlFor="lg-club">نام باشگاه</label>
        <div className="lg-input"><span className="lg-input-icon"><Icon name="user" /></span>
          <input id="lg-club" className="lg-text-input" value={name} onChange={(e) => setName(e.target.value)} maxLength={80} />
        </div>
        <label className="lg-label lg-gap" htmlFor="lg-branch">نام شعبه</label>
        <div className="lg-input"><span className="lg-input-icon"><Icon name="shield" /></span>
          <input id="lg-branch" className="lg-text-input" value={branchName} onChange={(e) => setBranchName(e.target.value)} maxLength={80} />
        </div>
        <label className="lg-label lg-gap" htmlFor="lg-owner">نام شما</label>
        <div className="lg-input"><span className="lg-input-icon"><Icon name="user" /></span>
          <input id="lg-owner" className="lg-text-input" value={ownerName} onChange={(e) => setOwnerName(e.target.value)} maxLength={80} autoComplete="name" />
        </div>
        <ErrorBox text={error} />
        <Submit busy={busy} label="ساخت باشگاه" />
      </form>
    </AuthLayout>
  );
}
