import { useEffect, useState } from "react";
import { api, dayLabel, jalali, setToken, tehranIso, token } from "./api";
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
type Page = "home" | "members" | "calendar" | "attendance" | "inbox";

const days = Array.from({ length: 7 }, (_, i) => {
  const day = new Date();
  day.setHours(0, 0, 0, 0);
  day.setDate(day.getDate() + i);
  return day;
});

export function App() {
  const [me, setMe] = useState<Me | null>(null);
  const [ready, setReady] = useState(false);
  const [page, setPage] = useState<Page>("home");

  async function loadMe() {
    if (!token()) {
      setMe(null);
      setReady(true);
      return;
    }
    try {
      setMe(await api<Me>("/api/v1/me"));
    } catch {
      setToken(null);
      setMe(null);
    } finally {
      setReady(false);
      setReady(true);
    }
  }

  useEffect(() => { void loadMe(); }, []);

  if (!ready) return <main className="shell splash">در حال بارگذاری…</main>;
  if (!me) return <Login onDone={loadMe} />;
  if (!me.branchId) return <Setup onDone={loadMe} />;

  const staff = me.permissions.includes("members.manage") || me.permissions.includes("sessions.manage");
  return (
    <main className="shell">
      <header className="top">
        <strong>باشگاه آرتِه</strong>
        <span>{me.displayName || me.username || me.mobile}</span>
      </header>
      <nav className="nav">
        <button aria-current={page === "home" ? "page" : undefined} onClick={() => setPage("home")}>خانه</button>
        {staff && <button aria-current={page === "members" ? "page" : undefined} onClick={() => setPage("members")}>شاگردها</button>}
        <button aria-current={page === "calendar" ? "page" : undefined} onClick={() => setPage("calendar")}>تقویم</button>
        {me.permissions.includes("attendance.record") && <button aria-current={page === "attendance" ? "page" : undefined} onClick={() => setPage("attendance")}>حضور</button>}
        <button aria-current={page === "inbox" ? "page" : undefined} onClick={() => setPage("inbox")}>صندوق</button>
      </nav>
      {page === "home" && <Home me={me} onGo={setPage} onLeave={() => { setToken(null); setMe(null); }} />}
      {page === "members" && <Members me={me} />}
      {page === "calendar" && <Calendar me={me} />}
      {page === "attendance" && <Attendance />}
      {page === "inbox" && <Inbox />}
    </main>
  );
}

function Home({ me, onLeave, onGo }: { me: Me; onLeave: () => void; onGo: (page: Page) => void }) {
  const [password, setPassword] = useState("");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  return (
    <section className="stack">
      <div className="card stack hero">
        <p className="overline">{roleName(me.role, me.isPlatformAdmin)}</p>
        <h1>سلام {me.displayName || me.username || "همراه"}</h1>
        <p className="muted">{me.branches.find((b) => b.branchId === me.branchId)?.branchName ?? "شعبه"}. کار بعدی را از همین‌جا باز کنید.</p>
      </div>
      <div className="actions">
        {me.permissions.includes("members.manage") && <button className="action" onClick={() => onGo("members")}><strong>شاگردها</strong><span>ثبت و اتصال به مربی</span></button>}
        <button className="action" onClick={() => onGo("calendar")}><strong>تقویم</strong><span>جلسه‌های هفت روز آینده</span></button>
        {me.permissions.includes("attendance.record") && <button className="action" onClick={() => onGo("attendance")}><strong>حضور</strong><span>حاضر و غایب همین امروز</span></button>}
      </div>
      {!me.hasPassword && (
        <form className="card stack" onSubmit={async (e) => {
          e.preventDefault();
          setBusy(true);
          setMessage("");
          try {
            await api("/api/v1/auth/password", { method: "POST", body: JSON.stringify({ password }) });
            setMessage("رمز ذخیره شد.");
            setPassword("");
          } catch (err) { setMessage(err instanceof Error ? err.message : "خطا"); }
          finally { setBusy(false); }
        }}>
          <label>رمز برای ورود بعدی<input type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" /></label>
          <button className="ghost" disabled={busy} aria-busy={busy}>{busy ? "در حال ذخیره…" : "ذخیرهٔ رمز"}</button>
          {message && <p>{message}</p>}
        </form>
      )}
      <button className="bad" onClick={onLeave}>خروج از حساب</button>
    </section>
  );
}

function Members({ me }: { me: Me }) {
  const [rows, setRows] = useState<Member[]>([]);
  const [staff, setStaff] = useState<{ personId: string; displayName: string; role: string }[]>([]);
  const [fullName, setFullName] = useState("");
  const [mobile, setMobile] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  async function load() {
    setRows(await api<Member[]>("/api/v1/members"));
    if (me.permissions.includes("staff.manage")) setStaff(await api("/api/v1/staff"));
  }
  useEffect(() => { void load(); }, []);

  return (
    <section className="stack">
      <form className="card stack" onSubmit={async (e) => {
        e.preventDefault();
        setBusy(true);
        setError("");
        try {
          await api("/api/v1/members", { method: "POST", body: JSON.stringify({ fullName, mobile }) });
          setFullName("");
          setMobile("");
          await load();
        } catch (err) { setError(err instanceof Error ? err.message : "خطا"); }
        finally { setBusy(false); }
      }}>
        <h2>شاگرد جدید</h2>
        <label>نام<input value={fullName} onChange={(e) => setFullName(e.target.value)} /></label>
        <label>موبایل، برای ورود خودش<input inputMode="numeric" value={mobile} onChange={(e) => setMobile(e.target.value)} /></label>
        <button className="primary" disabled={busy} aria-busy={busy}>{busy ? "در حال ثبت…" : "ثبت شاگرد"}</button>
        {error && <p className="err">{error}</p>}
      </form>
      {rows.map((member) => (
        <article className="card stack" key={member.id}>
          <strong>{member.fullName}</strong>
          <span className="muted">{member.mobile || "بدون موبایل"}</span>
          {staff.some((s) => s.role === "coach") && (
            <label>مربی مسئول
              <select onChange={async (e) => {
                if (!e.target.value) return;
                await api(`/api/v1/members/${member.id}/assignment`, { method: "POST", body: JSON.stringify({ coachPersonId: e.target.value }) });
              }}>
                <option value="">انتخاب مربی</option>
                {staff.filter((s) => s.role === "coach" || s.role === "manager").map((s) => (
                  <option key={s.personId} value={s.personId}>{s.displayName || s.role}</option>
                ))}
              </select>
            </label>
          )}
        </article>
      ))}
    </section>
  );
}

function Calendar({ me }: { me: Me }) {
  const [rows, setRows] = useState<Session[]>([]);
  const [day, setDay] = useState(0);
  const [title, setTitle] = useState("");
  const [start, setStart] = useState("10:00");
  const [end, setEnd] = useState("11:00");
  const [place, setPlace] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const canManage = me.permissions.includes("sessions.manage");

  async function load() {
    const from = days[0].toISOString();
    const to = new Date(days[6].getTime() + 86400000).toISOString();
    const path = me.role === "member" ? "/api/v1/me/sessions" : `/api/v1/sessions?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`;
    setRows(await api<Session[]>(path));
  }
  useEffect(() => { void load(); }, []);

  const chosen = rows.filter((s) => new Date(s.startsAt).toDateString() === days[day].toDateString());

  return (
    <section className="stack">
      <div className="chips">
        {days.map((item, i) => (
          <button className="ghost" key={item.toISOString()} aria-pressed={i === day} onClick={() => setDay(i)}>{dayLabel(item)}</button>
        ))}
      </div>
      {canManage && (
        <form className="card stack" onSubmit={async (e) => {
          e.preventDefault();
          setBusy(true);
          setError("");
          try {
            await api("/api/v1/sessions", { method: "POST", body: JSON.stringify({
              title, kind: "private", startsAt: tehranIso(days[day], start), endsAt: tehranIso(days[day], end),
              placeName: place, coachPersonId: me.id,
            }) });
            setTitle("");
            await load();
          } catch (err) { setError(err instanceof Error ? err.message : "خطا"); }
          finally { setBusy(false); }
        }}>
          <h2>جلسهٔ جدید</h2>
          <label>عنوان<input value={title} onChange={(e) => setTitle(e.target.value)} /></label>
          <div className="row">
            <label>شروع<input value={start} onChange={(e) => setStart(e.target.value)} /></label>
            <label>پایان<input value={end} onChange={(e) => setEnd(e.target.value)} /></label>
          </div>
          <label>فضا<input value={place} onChange={(e) => setPlace(e.target.value)} /></label>
          <button className="primary" disabled={busy} aria-busy={busy}>{busy ? "در حال ثبت…" : "ثبت جلسه"}</button>
          {error && <p className="err">{error}</p>}
        </form>
      )}
      {chosen.map((session) => (
        <article className="card stack" key={session.id}>
          <strong>{session.title}</strong>
          <span>{jalali(session.startsAt)} تا {new Intl.DateTimeFormat("fa-IR", { hour: "2-digit", minute: "2-digit" }).format(new Date(session.endsAt))}</span>
          <span className="muted">{session.placeName || "بدون فضا"} · {session.kind === "group" ? "گروهی" : "خصوصی"} · {session.status === "published" ? "منتشرشده" : session.status}</span>
          {me.role === "member" && (
            <button className="bad" onClick={async () => {
              await api(`/api/v1/me/sessions/${session.id}/cancel`, { method: "POST" });
              await load();
            }}>لغو وقت من</button>
          )}
        </article>
      ))}
      {chosen.length === 0 && <p className="muted">برای این روز جلسه‌ای نیست.</p>}
    </section>
  );
}

function Attendance() {
  const [rows, setRows] = useState<Session[]>([]);
  const [members, setMembers] = useState<Member[]>([]);
  const [message, setMessage] = useState("");
  useEffect(() => {
    const from = new Date();
    from.setHours(0, 0, 0, 0);
    const to = new Date(from.getTime() + 86400000);
    void api<Session[]>(`/api/v1/sessions?from=${encodeURIComponent(from.toISOString())}&to=${encodeURIComponent(to.toISOString())}`).then(setRows);
    void api<Member[]>("/api/v1/members").then(setMembers);
  }, []);

  async function mark(sessionId: string, memberId: string, status: string) {
    setMessage("");
    try {
      await api(`/api/v1/sessions/${sessionId}/attendance`, { method: "POST", body: JSON.stringify({ memberId, status }) });
      setMessage("حضور ثبت شد.");
    } catch (e) { setMessage(e instanceof Error ? e.message : "خطا"); }
  }

  return (
    <section className="stack">
      <Punch />
      {message && <p>{message}</p>}
      {rows.map((session) => (
        <article className="card stack" key={session.id}>
          <h2>{session.title}</h2>
          <span className="muted">{jalali(session.startsAt)}</span>
          {members.map((member) => (
            <div className="row" key={member.id}>
              <span>{member.fullName}</span>
              <button className="primary" onClick={() => mark(session.id, member.id, "present")}>حاضر</button>
              <button className="ghost" onClick={() => mark(session.id, member.id, "absent")}>غایب</button>
            </div>
          ))}
        </article>
      ))}
      {rows.length === 0 && <p className="muted">امروز جلسه‌ای نیست.</p>}
    </section>
  );
}

function Punch() {
  const [message, setMessage] = useState("");
  async function go(path: string) {
    try {
      await api(path, { method: "POST" });
      setMessage(path.endsWith("in") ? "ورود ثبت شد." : "خروج ثبت شد.");
    } catch (e) { setMessage(e instanceof Error ? e.message : "خطا"); }
  }
  return (
    <div className="card row">
      <button className="primary" onClick={() => go("/api/v1/punches/in")}>ورود شیفت</button>
      <button className="ghost" onClick={() => go("/api/v1/punches/out")}>خروج شیفت</button>
      {message && <span>{message}</span>}
    </div>
  );
}

function Inbox() {
  const [rows, setRows] = useState<Note[]>([]);
  async function load() { setRows(await api<Note[]>("/api/v1/inbox")); }
  useEffect(() => { void load(); }, []);
  return (
    <section className="stack">
      {rows.map((note) => (
        <article className="card stack" key={note.id}>
          <strong>{note.title}</strong>
          <span>{note.body}</span>
          <span className="muted">{jalali(note.createdAt)} · {note.seen ? "دیده شد" : "جدید"}</span>
          {!note.seen && <button className="ghost" onClick={async () => { await api(`/api/v1/inbox/${note.id}/seen`, { method: "POST" }); await load(); }}>دیدم</button>}
        </article>
      ))}
      {rows.length === 0 && <p className="muted">چیزی در صندوق نیست.</p>}
    </section>
  );
}

function roleName(role: string | null, admin = false) {
  if (admin) return "سوپر ادمین";
  if (role === "manager") return "مدیر";
  if (role === "secretary") return "منشی";
  if (role === "coach") return "مربی";
  if (role === "member") return "شاگرد";
  return "بدون شعبه";
}
