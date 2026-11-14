import { useEffect, useState, type ReactNode } from "react";
import { api, clock, dayLabel, jalali, persianDate, setToken, tehranIso, token } from "./api";
import { Icon } from "./icons";
import { Login, Setup } from "./login";

type Me = {
  id: string;
  mobile: string | null;
  username: string | null;
  displayName: string;
  isPlatformAdmin: boolean;
  hasPassword: boolean;
  branchId: string | null;
  role: string | null;
  permissions: string[];
  branches: { branchId: string; branchName: string }[];
};
type Member = { id: string; fullName: string; mobile: string | null };
type Session = { id: string; title: string; kind: string; startsAt: string; endsAt: string; placeName: string | null; status: string; coachPersonId: string };
type Note = { id: string; title: string; body: string; seen: boolean; createdAt: string };
type Staff = { personId: string; displayName: string; role: string };
type Page = "home" | "members" | "calendar" | "attendance" | "inbox";

const tones = ["#713F55", "#2F6B4F", "#8A5A2F", "#5C5458"];
const tone = (id: string) => tones[[...id].reduce((n, c) => n + c.charCodeAt(0), 0) % tones.length];
const initials = (name: string) => {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length >= 2) return parts[0][0] + parts[1][0];
  return name.trim().slice(0, 2) || "؟";
};
const startOf = (day: Date) => { const copy = new Date(day); copy.setHours(0, 0, 0, 0); return copy; };
const sameDay = (value: string, day: Date) => new Date(value).toDateString() === day.toDateString();
const statusOf = (status: string) => status === "published" ? { text: "تأیید شده", color: "plum" } : status === "cancelled" ? { text: "لغو شده", color: "red" } : { text: status || "در انتظار", color: "amber" };
const roleName = (role: string | null, admin = false) => admin ? "سوپر ادمین" : role === "manager" ? "مدیر" : role === "secretary" ? "منشی" : role === "coach" ? "مربی" : role === "member" ? "شاگرد" : "بدون شعبه";
const greeting = () => {
  const hour = Number(new Intl.DateTimeFormat("en-US", { hour: "numeric", hourCycle: "h23", timeZone: "Asia/Tehran" }).format(new Date()));
  if (hour < 12) return "صبح بخیر";
  if (hour < 17) return "ظهر بخیر";
  return "عصر بخیر";
};

export function App() {
  const [me, setMe] = useState<Me | null>(null);
  const [ready, setReady] = useState(false);
  const [page, setPage] = useState<Page>("home");

  async function loadMe() {
    if (!token()) { setMe(null); setReady(true); return; }
    try { setMe(await api<Me>("/api/v1/me")); }
    catch { setToken(null); setMe(null); }
    finally { setReady(true); }
  }
  useEffect(() => { void loadMe(); }, []);

  if (!ready) return <main className="splash">در حال بارگذاری…</main>;
  if (!me) return <Login onDone={loadMe} />;
  if (!me.branchId) return <Setup onDone={loadMe} />;
  if (me.role === "member") return <Student me={me} onLeave={() => { setToken(null); setMe(null); }} />;

  const nav = [
    { id: "home" as const, label: "خانه", icon: "home" as const, show: true },
    { id: "members" as const, label: "شاگردها", icon: "users" as const, show: me.permissions.some((p) => p === "members.manage" || p === "members.read" || p === "members.read_assigned") },
    { id: "calendar" as const, label: "تقویم", icon: "calendar" as const, show: true },
    { id: "attendance" as const, label: "حضور", icon: "user" as const, show: me.permissions.includes("attendance.record") },
    { id: "inbox" as const, label: "صندوق", icon: "inbox" as const, show: true },
  ].filter((item) => item.show);
  const branch = me.branches.find((b) => b.branchId === me.branchId)?.branchName ?? "شعبه";

  return (
    <div className="app-shell">
      <div className="ambient ambient-one" />
      <div className="ambient ambient-two" />
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-mark"><span>آ</span></div>
          <div><strong>آرته</strong><small>{branch}</small></div>
        </div>
        <nav className="side-nav">
          {nav.map((item) => (
            <button key={item.id} className={page === item.id ? "nav-item active" : "nav-item"} aria-current={page === item.id ? "page" : undefined} onClick={() => setPage(item.id)}>
              <Icon name={item.icon} />
              <span>{item.label}</span>
            </button>
          ))}
        </nav>
        <div className="side-note">
          <Icon name="spark" size={18} />
          <div><strong>میز کار امروز</strong><span>اول کار باز، بعد بقیه.</span></div>
        </div>
        <button className="logout" onClick={() => { setToken(null); setMe(null); }}><Icon name="logout" size={18} /> خروج از حساب</button>
      </aside>
      <main className="main-content">
        <div className="desktop-topbar">
          <div className="today"><span className="live-dot" />{persianDate(new Date(), { weekday: "long", day: "numeric", month: "long" })}</div>
          <div className="profile">
            <div><strong>{me.displayName || me.username || "کاربر"}</strong><span>{roleName(me.role, me.isPlatformAdmin)}</span></div>
            <div className="avatar">{initials(me.displayName || me.username || "آ")}</div>
          </div>
        </div>
        <div className="view-wrap">
          {page === "home" && <Home me={me} onGo={setPage} />}
          {page === "members" && <Members me={me} />}
          {page === "calendar" && <Calendar me={me} />}
          {page === "attendance" && <Attendance />}
          {page === "inbox" && <Inbox />}
        </div>
      </main>
      <nav className="bottom-nav" style={{ gridTemplateColumns: `repeat(${nav.length}, 1fr)` }}>
        {nav.map((item) => (
          <button key={item.id} className={page === item.id ? "bottom-item active" : "bottom-item"} aria-current={page === item.id ? "page" : undefined} onClick={() => setPage(item.id)}>
            <Icon name={item.icon} size={21} />
            <span>{item.label}</span>
          </button>
        ))}
      </nav>
    </div>
  );
}

function Toast({ message }: { message: string }) {
  if (!message) return null;
  return <div className="toast" role="status"><span className="toast-icon"><Icon name="check" size={14} /></span>{message}</div>;
}

function Modal({ open, title, onClose, children }: { open: boolean; title: string; onClose: () => void; children: ReactNode }) {
  if (!open) return null;
  return (
    <div className="modal-backdrop" onMouseDown={onClose}>
      <div className="modal-card" role="dialog" aria-modal="true" aria-label={title} onMouseDown={(e) => e.stopPropagation()}>
        <button className="icon-button modal-close" onClick={onClose} aria-label="بستن"><Icon name="close" /></button>
        {children}
      </div>
    </div>
  );
}

function Home({ me, onGo }: { me: Me; onGo: (page: Page) => void }) {
  const [notes, setNotes] = useState<Note[]>([]);
  const [sessions, setSessions] = useState<Session[]>([]);
  const [password, setPassword] = useState("");
  const [toast, setToast] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  async function load() {
    const from = startOf(new Date());
    const to = new Date(from.getTime() + 8 * 86400000);
    const [inbox, rows] = await Promise.all([
      api<Note[]>("/api/v1/inbox"),
      api<Session[]>(`/api/v1/sessions?from=${encodeURIComponent(from.toISOString())}&to=${encodeURIComponent(to.toISOString())}`),
    ]);
    setNotes(inbox);
    setSessions(rows);
  }
  useEffect(() => { void load().catch((e) => setError(e instanceof Error ? e.message : "خطا")); }, []);

  const openNotes = notes.filter((note) => !note.seen);
  const todaySessions = sessions.filter((session) => sameDay(session.startsAt, new Date()) && session.status !== "cancelled");
  const next = sessions.filter((session) => new Date(session.startsAt) > new Date() && session.status !== "cancelled").sort((a, b) => a.startsAt.localeCompare(b.startsAt))[0];
  const openCount = openNotes.length + (me.permissions.includes("attendance.record") && todaySessions.length ? 1 : 0);
  const name = (me.displayName || me.username || "همراه").split(" ")[0];

  async function see(id: string) {
    await api(`/api/v1/inbox/${id}/seen`, { method: "POST" });
    setToast("دیده‌شده ثبت شد");
    await load();
  }

  return (
    <section>
      <div className="hero">
        <div className="hero-copy">
          <p className="eyebrow light">میز کار امروز</p>
          <h1>{greeting()}، {name}</h1>
          <p>{openCount ? `${openCount.toLocaleString("fa-IR")} موضوع منتظر تصمیم توست.` : "کار بازی روی میز نیست."}</p>
        </div>
        <div className="hero-orbit" aria-hidden="true"><div className="orbit-ring" /><div className="orbit-core"><Icon name="calendar" size={28} /></div></div>
      </div>
      <div className="section-title">
        <div><span className="section-kicker">نیازمند اقدام</span><h2>صف کار تو</h2></div>
        <span className="count-pill">{openCount.toLocaleString("fa-IR")} مورد باز</span>
      </div>
      {error && <p className="err">{error}</p>}
      <div className="task-grid">
        {openNotes.map((note) => (
          <article className="task-card urgent" key={note.id}>
            <div className="task-icon"><Icon name="userX" size={22} /></div>
            <div className="task-content">
              <div className="task-meta"><span>جدید</span><time>{jalali(note.createdAt)}</time></div>
              <h3>{note.title}</h3>
              <p>{note.body}</p>
              <div className="task-actions">
                <button className="primary-button" onClick={() => void see(note.id)}>دیدم</button>
                <button className="text-button" onClick={() => onGo("inbox")}>صندوق</button>
              </div>
            </div>
          </article>
        ))}
        {me.permissions.includes("attendance.record") && todaySessions.length > 0 && (
          <article className="task-card">
            <div className="task-icon warm"><Icon name="clock" size={22} /></div>
            <div className="task-content">
              <div className="task-meta"><span>حضور امروز</span><time>{todaySessions.length.toLocaleString("fa-IR")} جلسه</time></div>
              <h3>{todaySessions[0].title}</h3>
              <p>حضور این جلسه هنوز از همین صف بسته نشده. از صفحهٔ حضور ثبت کن.</p>
              <div className="task-actions"><button className="secondary-button" onClick={() => onGo("attendance")}>ثبت حضور</button></div>
            </div>
          </article>
        )}
      </div>
      {openCount === 0 && <p className="empty-state">صف خالی است. جلسهٔ بعدی را از تقویم ببین.</p>}
      {next && (
        <section className="next-session">
          <div className="session-time"><span>جلسه بعدی</span><strong>{clock(next.startsAt)}</strong></div>
          <div className="session-divider" />
          <div className="session-info">
            <span className="status-dot amber" />
            <div><strong>{next.title}</strong><span>{next.placeName || "بدون فضا"}</span></div>
          </div>
          <button className="round-link" aria-label="تقویم" onClick={() => onGo("calendar")}><Icon name="arrow" size={17} /></button>
        </section>
      )}
      <div className="quick-row">
        <button onClick={() => onGo("inbox")}><Icon name="message" /><span><strong>صندوق</strong><small>{openNotes.length.toLocaleString("fa-IR")} خوانده‌نشده</small></span><Icon name="arrow" size={17} /></button>
        <button onClick={() => onGo("calendar")}><Icon name="calendar" /><span><strong>برنامهٔ هفته</strong><small>جلسه‌های پیش رو</small></span><Icon name="arrow" size={17} /></button>
      </div>
      {!me.hasPassword && (
        <form className="quiet-card" onSubmit={async (e) => {
          e.preventDefault();
          setBusy(true);
          setError("");
          try {
            await api("/api/v1/auth/password", { method: "POST", body: JSON.stringify({ password }) });
            setToast("رمز ذخیره شد");
            setPassword("");
          } catch (err) { setError(err instanceof Error ? err.message : "خطا"); }
          finally { setBusy(false); }
        }}>
          <h2>رمز برای ورود بعدی</h2>
          <label>رمز عبور<input type="password" value={password} autoComplete="new-password" onChange={(e) => setPassword(e.target.value)} /></label>
          {error && <p className="err">{error}</p>}
          <button className="primary-button" disabled={busy} aria-busy={busy}>{busy ? "در حال ذخیره…" : "ذخیرهٔ رمز"}</button>
        </form>
      )}
      <Toast message={toast} />
    </section>
  );
}

function Members({ me }: { me: Me }) {
  const [rows, setRows] = useState<Member[]>([]);
  const [staff, setStaff] = useState<Staff[]>([]);
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const [fullName, setFullName] = useState("");
  const [mobile, setMobile] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const canManage = me.permissions.includes("members.manage");

  async function load() {
    setRows(await api<Member[]>("/api/v1/members"));
    if (me.permissions.includes("staff.manage")) setStaff(await api<Staff[]>("/api/v1/staff"));
  }
  useEffect(() => { void load().catch((e) => setError(e instanceof Error ? e.message : "خطا")); }, []);
  const filtered = rows.filter((member) => member.fullName.includes(query) || (member.mobile ?? "").includes(query.replace(/\D/g, "")));
  const coaches = staff.filter((person) => person.role === "coach" || person.role === "manager");

  return (
    <section>
      <header className="page-header">
        <div><p className="eyebrow">مدیریت ارتباط</p><h1>شاگردها</h1></div>
        {canManage && <button className="primary-button" onClick={() => setOpen(true)}><Icon name="plus" size={18} /> شاگرد جدید</button>}
      </header>
      <div className="toolbar">
        <label className="search"><Icon name="search" size={19} /><input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="جست‌وجوی نام یا شماره" /></label>
        <span className="soft-label">{filtered.length.toLocaleString("fa-IR")} شاگرد</span>
      </div>
      {error && <p className="err">{error}</p>}
      <div className="member-list">
        {filtered.map((member) => (
          <article className="member-row" key={member.id}>
            <div className="member-avatar" style={{ background: tone(member.id) }}>{initials(member.fullName)}</div>
            <div className="member-main">
              <strong>{member.fullName}</strong>
              <span dir="ltr">{member.mobile || "بدون موبایل"}</span>
              {coaches.length > 0 && (
                <select aria-label="مربی مسئول" defaultValue="" onChange={async (e) => {
                  if (!e.target.value) return;
                  await api(`/api/v1/members/${member.id}/assignment`, { method: "POST", body: JSON.stringify({ coachPersonId: e.target.value }) });
                }}>
                  <option value="">مربی مسئول</option>
                  {coaches.map((person) => <option key={person.personId} value={person.personId}>{person.displayName || person.role}</option>)}
                </select>
              )}
            </div>
          </article>
        ))}
      </div>
      {filtered.length === 0 && <div className="empty-state"><Icon name="search" /><h3>شاگردی پیدا نشد</h3><p>نام یا شماره را دوباره بررسی کن.</p></div>}
      <Modal open={open} title="افزودن شاگرد" onClose={() => setOpen(false)}>
        <form onSubmit={async (e) => {
          e.preventDefault();
          setBusy(true);
          setError("");
          try {
            await api("/api/v1/members", { method: "POST", body: JSON.stringify({ fullName, mobile }) });
            setFullName("");
            setMobile("");
            setOpen(false);
            await load();
          } catch (err) { setError(err instanceof Error ? err.message : "خطا"); }
          finally { setBusy(false); }
        }}>
          <div className="modal-heading">
            <div className="large-icon"><Icon name="user" /></div>
            <p className="eyebrow">پروفایل تازه</p>
            <h2>افزودن شاگرد</h2>
            <p>فقط نام و موبایل. جزئیات را بعداً هم می‌شود کامل کرد.</p>
          </div>
          <div className="field-grid">
            <label>نام و نام خانوادگی<input value={fullName} required onChange={(e) => setFullName(e.target.value)} /></label>
            <label>شماره تماس<input inputMode="numeric" dir="ltr" value={mobile} required onChange={(e) => setMobile(e.target.value)} /></label>
          </div>
          {error && <p className="err">{error}</p>}
          <button className="primary-button full" disabled={busy} aria-busy={busy}>{busy ? "در حال ثبت…" : "ساخت پروفایل شاگرد"}</button>
        </form>
      </Modal>
    </section>
  );
}

function Calendar({ me }: { me: Me }) {
  const [week, setWeek] = useState(0);
  const [day, setDay] = useState(0);
  const [rows, setRows] = useState<Session[]>([]);
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<Session | null>(null);
  const [title, setTitle] = useState("");
  const [start, setStart] = useState("10:00");
  const [end, setEnd] = useState("11:00");
  const [place, setPlace] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const days = Array.from({ length: 7 }, (_, i) => { const date = startOf(new Date()); date.setDate(date.getDate() + week * 7 + i); return date; });
  const canManage = me.permissions.includes("sessions.manage");

  async function load() {
    const from = days[0].toISOString();
    const to = new Date(days[6].getTime() + 86400000).toISOString();
    setRows(await api<Session[]>(`/api/v1/sessions?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`));
  }
  useEffect(() => { void load().catch((e) => setError(e instanceof Error ? e.message : "خطا")); }, [week]);
  const chosen = rows.filter((session) => sameDay(session.startsAt, days[day]));

  return (
    <section>
      <header className="page-header">
        <div><p className="eyebrow">{persianDate(days[0], { month: "long", year: "numeric" })}</p><h1>تقویم جلسات</h1></div>
        <div style={{ display: "flex", gap: 8 }}>
          <button className="ghost-button" onClick={() => { setWeek(0); setDay(0); }}><Icon name="calendar" size={18} /> امروز</button>
          {canManage && <button className="primary-button" onClick={() => setOpen(true)}><Icon name="plus" size={18} /> جلسه</button>}
        </div>
      </header>
      <section className="calendar-strip">
        <div className="month-switch">
          <button aria-label="هفته قبل" onClick={() => setWeek(week - 1)}><Icon name="arrow" size={18} /></button>
          <strong>{dayLabel(days[0])} تا {dayLabel(days[6])}</strong>
          <button aria-label="هفته بعد" onClick={() => setWeek(week + 1)} style={{ transform: "scaleX(-1)" }}><Icon name="arrow" size={18} /></button>
        </div>
        <div className="days-row">
          {days.map((item, index) => (
            <button key={item.toISOString()} className={day === index ? "selected" : ""} onClick={() => setDay(index)}>
              <span>{persianDate(item, { weekday: "narrow" })}</span>
              <strong>{persianDate(item, { day: "numeric" })}</strong>
              {rows.some((session) => sameDay(session.startsAt, item)) && <i />}
            </button>
          ))}
        </div>
      </section>
      <div className="section-title calendar-title">
        <div><span className="section-kicker">{dayLabel(days[day])}</span><h2>برنامهٔ روز</h2></div>
        <span className="soft-label">{chosen.length.toLocaleString("fa-IR")} جلسه</span>
      </div>
      {error && <p className="err">{error}</p>}
      <div className="timeline">
        {chosen.map((session) => {
          const status = statusOf(session.status);
          return (
            <button className="timeline-row" key={session.id} onClick={() => setSelected(session)}>
              <time>{clock(session.startsAt)}</time>
              <span className={`timeline-node ${status.color === "red" ? "amber" : status.color}`} />
              <div className="timeline-card">
                <div>
                  <span className={`status-label ${status.color}`}>{status.text}</span>
                  <h3>{session.title}</h3>
                  <p>{session.kind === "group" ? "گروهی" : "خصوصی"}{session.placeName ? ` · ${session.placeName}` : ""}</p>
                </div>
                <span className="session-count"><Icon name="clock" size={16} />{clock(session.endsAt)}</span>
              </div>
            </button>
          );
        })}
      </div>
      {chosen.length === 0 && <p className="empty-state">برای این روز جلسه‌ای نیست.</p>}
      <Modal open={Boolean(selected)} title="جزئیات جلسه" onClose={() => setSelected(null)}>
        <div className="modal-heading left">
          <p className="eyebrow">جزئیات جلسه</p>
          <h2>{selected?.title}</h2>
          <p>{selected ? (selected.kind === "group" ? "کلاس گروهی" : "جلسه خصوصی") : ""}</p>
        </div>
        <div className="detail-lines">
          <div><Icon name="clock" size={18} /><span><small>زمان</small><strong>{selected ? `${clock(selected.startsAt)} تا ${clock(selected.endsAt)}` : ""}</strong></span></div>
          <div><Icon name="users" size={18} /><span><small>نوع</small><strong>{selected?.kind === "group" ? "گروهی" : "خصوصی"}</strong></span></div>
          <div><Icon name="map" size={18} /><span><small>فضا</small><strong>{selected?.placeName || "ثبت نشده"}</strong></span></div>
        </div>
        <button className="primary-button full" onClick={() => setSelected(null)}>متوجه شدم</button>
      </Modal>
      <Modal open={open} title="جلسه جدید" onClose={() => setOpen(false)}>
        <form onSubmit={async (e) => {
          e.preventDefault();
          setBusy(true);
          setError("");
          try {
            await api("/api/v1/sessions", { method: "POST", body: JSON.stringify({ title, kind: "private", startsAt: tehranIso(days[day], start), endsAt: tehranIso(days[day], end), placeName: place, coachPersonId: me.id }) });
            setTitle("");
            setOpen(false);
            await load();
          } catch (err) { setError(err instanceof Error ? err.message : "خطا"); }
          finally { setBusy(false); }
        }}>
          <div className="modal-heading"><p className="eyebrow">برنامه</p><h2>جلسهٔ جدید</h2></div>
          <label>عنوان<input value={title} required onChange={(e) => setTitle(e.target.value)} /></label>
          <div className="field-grid">
            <label>شروع<input dir="ltr" value={start} onChange={(e) => setStart(e.target.value)} /></label>
            <label>پایان<input dir="ltr" value={end} onChange={(e) => setEnd(e.target.value)} /></label>
          </div>
          <label>فضا<input value={place} onChange={(e) => setPlace(e.target.value)} /></label>
          {error && <p className="err">{error}</p>}
          <button className="primary-button full" disabled={busy} aria-busy={busy}>{busy ? "در حال ثبت…" : "ثبت جلسه"}</button>
        </form>
      </Modal>
    </section>
  );
}

function Attendance() {
  const [rows, setRows] = useState<Session[]>([]);
  const [members, setMembers] = useState<Member[]>([]);
  const [picked, setPicked] = useState("");
  const [present, setPresent] = useState<string[]>([]);
  const [punched, setPunched] = useState(false);
  const [toast, setToast] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  useEffect(() => {
    const from = startOf(new Date());
    const to = new Date(from.getTime() + 86400000);
    void api<Session[]>(`/api/v1/sessions?from=${encodeURIComponent(from.toISOString())}&to=${encodeURIComponent(to.toISOString())}`).then((list) => { setRows(list); setPicked(list[0]?.id ?? ""); });
    void api<Member[]>("/api/v1/members").then(setMembers).catch((e) => setError(e instanceof Error ? e.message : "خطا"));
  }, []);
  const session = rows.find((item) => item.id === picked) ?? rows[0];

  async function punch(path: string) {
    setError("");
    try {
      await api(path, { method: "POST" });
      setPunched(path.endsWith("in"));
      setToast(path.endsWith("in") ? "ورود ثبت شد" : "خروج ثبت شد");
    } catch (e) { setError(e instanceof Error ? e.message : "خطا"); }
  }

  return (
    <section>
      <header className="page-header"><div><p className="eyebrow">ثبت روزانه</p><h1>حضور و غیاب</h1></div></header>
      {error && <p className="err">{error}</p>}
      <div className="attendance-layout">
        <section className="attendance-card">
          <div className="card-heading">
            <div>
              <span className="section-kicker">جلسهٔ امروز</span>
              <h2>{session?.title ?? "جلسه‌ای نیست"}</h2>
              {session && <p><Icon name="clock" size={16} /> {clock(session.startsAt)} تا {clock(session.endsAt)}</p>}
            </div>
            {session && <span className="status-label amber">باز</span>}
          </div>
          {rows.length > 1 && (
            <div className="session-switch">
              {rows.map((item) => <button key={item.id} aria-pressed={item.id === session?.id} onClick={() => setPicked(item.id)}>{item.title}</button>)}
            </div>
          )}
          <div className="check-list">
            {members.map((member) => {
              const checked = present.includes(member.id);
              return (
                <button key={member.id} className={checked ? "checked" : ""} onClick={() => setPresent(checked ? present.filter((id) => id !== member.id) : [...present, member.id])}>
                  <span className="check-avatar">{initials(member.fullName)}</span>
                  <strong>{member.fullName}</strong>
                  <span className="check-control">{checked && <Icon name="check" size={16} />}</span>
                </button>
              );
            })}
          </div>
          {session && (
            <button className="primary-button full" disabled={busy} aria-busy={busy} onClick={async () => {
              setBusy(true);
              setError("");
              try {
                if (!present.length) { setError("حداقل یک شاگرد را حاضر انتخاب کن."); return; }
                for (const id of present) {
                  await api(`/api/v1/sessions/${session.id}/attendance`, { method: "POST", body: JSON.stringify({ memberId: id, status: "present" }) });
                }
                setToast(`حضور ${present.length.toLocaleString("fa-IR")} شاگرد ثبت شد`);
              } catch (e) { setError(e instanceof Error ? e.message : "خطا"); }
              finally { setBusy(false); }
            }}>
              <Icon name="userCheck" size={19} /> {busy ? "در حال ثبت…" : "نهایی‌کردن حضور"}
            </button>
          )}
          {rows.length === 0 && <p className="empty-state">امروز جلسه‌ای برای ثبت حضور نیست.</p>}
        </section>
        <aside className={punched ? "punch-card done" : "punch-card"}>
          <div className="fingerprint-wrap"><Icon name="fingerprint" size={36} /></div>
          <p className="eyebrow">حضور پرسنل</p>
          <h2>{punched ? "ورودت ثبت شد" : "شروع شیفت"}</h2>
          <p>{punched ? "می‌توانی خروج را هم همین‌جا ثبت کنی." : "برای ثبت زمان ورود امروز لمس کن."}</p>
          <button className={punched ? "secondary-button full" : "primary-button full"} onClick={() => void punch(punched ? "/api/v1/punches/out" : "/api/v1/punches/in")}>
            {punched ? "ثبت خروج" : "ثبت ورود من"}
          </button>
        </aside>
      </div>
      <Toast message={toast} />
    </section>
  );
}

function Inbox() {
  const [rows, setRows] = useState<Note[]>([]);
  const [active, setActive] = useState("");
  const [error, setError] = useState("");
  async function load() {
    const notes = await api<Note[]>("/api/v1/inbox");
    setRows(notes);
    setActive((current) => current || notes[0]?.id || "");
  }
  useEffect(() => { void load().catch((e) => setError(e instanceof Error ? e.message : "خطا")); }, []);
  const current = rows.find((note) => note.id === active) ?? rows[0];

  return (
    <section>
      <header className="page-header"><div><p className="eyebrow">اطلاع‌ها</p><h1>صندوق</h1></div></header>
      {error && <p className="err">{error}</p>}
      {rows.length === 0 && <p className="empty-state">چیزی در صندوق نیست.</p>}
      {rows.length > 0 && current && (
        <div className="inbox-layout">
          <div className="message-list">
            {rows.map((note) => (
              <button key={note.id} className={`${current.id === note.id ? "active" : ""} ${note.seen ? "" : "unread"}`} onClick={() => setActive(note.id)}>
                <span className="message-avatar" style={{ background: tone(note.id) }}>{initials(note.title)}</span>
                <span className="message-preview">
                  <span><strong>{note.title}</strong><time>{jalali(note.createdAt)}</time></span>
                  <small>{note.body}</small>
                </span>
                {!note.seen && <i />}
              </button>
            ))}
          </div>
          <section className="conversation">
            <header>
              <span className="message-avatar" style={{ background: tone(current.id) }}>{initials(current.title)}</span>
              <div><strong>{current.title}</strong><span>{current.seen ? "دیده شد" : "جدید"}</span></div>
              {!current.seen && <button className="ghost-button" onClick={async () => { await api(`/api/v1/inbox/${current.id}/seen`, { method: "POST" }); await load(); }}><Icon name="checkDouble" size={17} /> علامت‌گذاری به‌عنوان دیده‌شده</button>}
            </header>
            <div className="conversation-body">
              <div className="bubble">{current.body}<time>{jalali(current.createdAt)}</time></div>
            </div>
          </section>
        </div>
      )}
    </section>
  );
}

function Student({ me, onLeave }: { me: Me; onLeave: () => void }) {
  const [rows, setRows] = useState<Session[]>([]);
  const [cancelId, setCancelId] = useState<string | null>(null);
  const [toast, setToast] = useState("");
  const [error, setError] = useState("");
  async function load() { setRows(await api<Session[]>("/api/v1/me/sessions")); }
  useEffect(() => { void load().catch((e) => setError(e instanceof Error ? e.message : "خطا")); }, []);
  const next = rows.filter((session) => session.status !== "cancelled" && new Date(session.endsAt) > new Date()).sort((a, b) => a.startsAt.localeCompare(b.startsAt))[0];
  const name = (me.displayName || "شاگرد").split(" ")[0];

  return (
    <main className="student-page">
      <header>
        <button onClick={onLeave}><Icon name="arrow" size={16} /> خروج</button>
        <div className="mini-brand"><span>آ</span> آرته</div>
        <div className="student-avatar">{initials(me.displayName || "ش")}</div>
      </header>
      <section className="student-hero">
        <p className="eyebrow light">{persianDate(new Date(), { weekday: "long", day: "numeric", month: "long" })}</p>
        <h1>سلام {name}</h1>
        <p>{next ? "جلسهٔ بعدی تو اینجاست." : "جلسهٔ بازی برای تو ثبت نشده."}</p>
      </section>
      {error && <p className="err">{error}</p>}
      {next && (
        <section className="student-session">
          <span className={`status-label ${statusOf(next.status).color}`}>{statusOf(next.status).text}</span>
          <h2>{next.title}</h2>
          <div className="student-details">
            <span><Icon name="calendar" size={16} />{dayLabel(new Date(next.startsAt))}</span>
            <span><Icon name="clock" size={16} />{clock(next.startsAt)} تا {clock(next.endsAt)}</span>
            <span><Icon name="map" size={16} />{next.placeName || "فضا ثبت نشده"}</span>
          </div>
          <button className="danger-text" onClick={() => setCancelId(next.id)}>نمی‌توانم در این جلسه شرکت کنم</button>
        </section>
      )}
      <p className="empty-state">در این صفحه فقط برنامهٔ خودت نمایش داده می‌شود.</p>
      <Modal open={Boolean(cancelId)} title="لغو رزرو" onClose={() => setCancelId(null)}>
        <div className="modal-heading">
          <p className="eyebrow">لغو رزرو</p>
          <h2>از لغو جلسه مطمئنی؟</h2>
          <p>این تغییر فقط روی رزرو خودت است و مدیر باشگاه در صندوق می‌بیند.</p>
        </div>
        <div className="modal-buttons">
          <button className="danger-button" onClick={async () => {
            if (!cancelId) return;
            await api(`/api/v1/me/sessions/${cancelId}/cancel`, { method: "POST" });
            setCancelId(null);
            setToast("درخواست لغو ثبت شد");
            await load();
          }}>بله، جلسه را لغو کن</button>
          <button className="ghost-button" onClick={() => setCancelId(null)}>بازگشت</button>
        </div>
      </Modal>
      <Toast message={toast} />
    </main>
  );
}
