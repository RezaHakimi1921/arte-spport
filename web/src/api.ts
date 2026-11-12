const tokenKey = "arte-spport-token";

export function token() {
  return localStorage.getItem(tokenKey);
}

export function setToken(value: string | null) {
  if (value) localStorage.setItem(tokenKey, value);
  else localStorage.removeItem(tokenKey);
}

export class ApiError extends Error {
  constructor(message: string, readonly status: number) {
    super(message);
  }
}

export async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Accept", "application/json");
  if (init.body) headers.set("Content-Type", "application/json");
  const current = token();
  if (current) headers.set("Authorization", `Bearer ${current}`);
  const res = await fetch(path, { ...init, headers });
  if (res.status === 204) return undefined as T;
  const text = await res.text();
  const body = text ? JSON.parse(text) as { title?: string; accessToken?: string } : {};
  if (!res.ok) throw new ApiError(body.title || "خطا در ارتباط با سرور.", res.status);
  if (body.accessToken) setToken(body.accessToken);
  return body as T;
}

export function tehranIso(day: Date, hhmm: string) {
  const [hour, minute] = hhmm.split(":").map(Number);
  return new Date(Date.UTC(day.getFullYear(), day.getMonth(), day.getDate(), hour - 3, minute - 30, 0)).toISOString();
}

export function jalali(value: string) {
  return new Intl.DateTimeFormat("fa-IR-u-ca-persian", {
    weekday: "short",
    day: "numeric",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value));
}

export function dayLabel(day: Date) {
  return new Intl.DateTimeFormat("fa-IR-u-ca-persian", { weekday: "long", day: "numeric", month: "short" }).format(day);
}
