import { useState, type FormEvent, type ReactNode } from "react";
import { api } from "./api";
import { Icon } from "./icons";

const digits = (value: string) => value.replace(/[۰-۹]/g, (d) => String("۰۱۲۳۴۵۶۷۸۹".indexOf(d))).replace(/[٠-٩]/g, (d) => String("٠١٢٣٤٥٦٧٨٩".indexOf(d))).replace(/\D/g, "");
const isMobile = (value: string) => /^09\d{9}$/.test(digits(value));
const message = (e: unknown) => (e instanceof Error ? e.message : "ارتباط با سرور برقرار نشد.");

function Frame({ children }: { children: ReactNode }) {
  return (
    <main className="login-page">
      <div className="login-art" aria-hidden="true">
        <div className="art-ring ring-a" />
        <div className="art-ring ring-b" />
        <div className="art-copy">
          <div className="brand-mark large"><span>آ</span></div>
          <h1>مدیریت،<br />با ریتمی آرام.</h1>
          <p>همهٔ ارتباط‌ها، جلسات و حضورها در یک فضای ساده و متمرکز.</p>
        </div>
      </div>
      <section className="login-panel">
        <div className="login-box">{children}</div>
      </section>
    </main>
  );
}

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

  const sms = mode !== "password";
  return (
    <Frame>
      <p className="eyebrow">خوش آمدی</p>
      <h2>{mode === "code" ? "کد پیامک" : sms ? "ورود با کد پیامکی" : "ورود به آرته"}</h2>
      <p className="login-subtitle">
        {mode === "code"
          ? "کد ۵ رقمی را وارد کنید."
          : sms
            ? "شماره‌ات را وارد کن تا کد یک‌بارمصرف بفرستیم."
            : "با نام کاربری یا موبایل، و رمز عبور."}
      </p>
      {mode !== "code" && (
        <div className="login-tabs" role="tablist">
          <button type="button" className={!sms ? "active" : ""} aria-selected={!sms} onClick={() => { setMode("password"); setError(""); }}>
            <Icon name="key" size={16} /> رمز عبور
          </button>
          <button type="button" className={sms ? "active" : ""} aria-selected={sms} onClick={() => { setMode("phone"); setError(""); }}>
            <Icon name="message" size={16} /> کد پیامکی
          </button>
        </div>
      )}
      <form onSubmit={submit} noValidate>
        {mode === "password" && (
          <>
            <label>موبایل یا نام کاربری
              <input autoComplete="username" dir="ltr" value={username} onChange={(e) => { setUsername(e.target.value); setError(""); }} />
            </label>
            <label>رمز عبور
              <span className="secret">
                <input type={showPassword ? "text" : "password"} autoComplete="current-password" dir="ltr" value={password} onChange={(e) => { setPassword(e.target.value); setError(""); }} />
                <button className="reveal" type="button" onClick={() => setShowPassword(!showPassword)} aria-label={showPassword ? "پنهان کردن رمز" : "نمایش رمز"}>
                  <Icon name={showPassword ? "eyeOff" : "eye"} size={18} />
                </button>
              </span>
            </label>
          </>
        )}
        {mode === "phone" && (
          <label>شماره موبایل
            <input type="tel" inputMode="numeric" autoComplete="tel" dir="ltr" maxLength={11} value={phone} placeholder="09120000000" onChange={(e) => { setPhone(digits(e.target.value).slice(0, 11)); setError(""); }} />
          </label>
        )}
        {mode === "code" && (
          <>
            <label>کد تأیید
              <input inputMode="numeric" autoComplete="one-time-code" dir="ltr" maxLength={5} value={code} onChange={(e) => { setCode(digits(e.target.value).slice(0, 5)); setError(""); }} />
            </label>
            <button type="button" className="text-link" onClick={() => { setMode("phone"); setError(""); }}>تغییر شماره</button>
          </>
        )}
        {error && <p className="err" role="alert">{error}</p>}
        <button className="primary-button full" type="submit" disabled={busy} aria-busy={busy}>
          {busy ? "در حال ورود…" : mode === "phone" ? "ارسال کد ورود" : mode === "code" ? "تأیید و ورود" : "ورود به پنل"}
          {!busy && <Icon name="arrow" size={18} />}
        </button>
      </form>
    </Frame>
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
    <Frame>
      <p className="eyebrow">اولین ورود</p>
      <h2>ساخت باشگاه</h2>
      <p className="login-subtitle">این حساب هنوز به شعبه‌ای وصل نیست. نام باشگاه را بنویسید تا میز کار باز شود.</p>
      <form onSubmit={submit} noValidate>
        <label>نام باشگاه<input value={name} maxLength={80} onChange={(e) => setName(e.target.value)} /></label>
        <label>نام شعبه<input value={branchName} maxLength={80} onChange={(e) => setBranchName(e.target.value)} /></label>
        <label>نام شما<input value={ownerName} maxLength={80} autoComplete="name" onChange={(e) => setOwnerName(e.target.value)} /></label>
        {error && <p className="err" role="alert">{error}</p>}
        <button className="primary-button full" disabled={busy} aria-busy={busy}>{busy ? "در حال ساخت…" : "ساخت باشگاه"}</button>
      </form>
    </Frame>
  );
}
