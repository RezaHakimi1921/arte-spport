import type { ReactNode } from "react";

type Name =
  | "arrow" | "calendar" | "check" | "checkDouble" | "clock" | "close" | "eye" | "eyeOff"
  | "fingerprint" | "home" | "inbox" | "key" | "logout" | "map" | "message" | "plus"
  | "search" | "shield" | "spark" | "user" | "userCheck" | "userX" | "users";

const paths: Record<Name, ReactNode> = {
  arrow: <path d="M19 12H5M12 19l-7-7 7-7" />,
  calendar: <><rect x="3" y="5" width="18" height="16" rx="3" /><path d="M3 10h18M8 3v4M16 3v4" /></>,
  check: <path d="m5 12 4 4L19 6" />,
  checkDouble: <path d="m2 12 5 5L17 7M8 12l5 5L22 7" />,
  clock: <><circle cx="12" cy="12" r="9" /><path d="M12 7v6l4 2" /></>,
  close: <path d="M6 6l12 12M18 6 6 18" />,
  eye: <><path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6S2 12 2 12Z" /><circle cx="12" cy="12" r="2.5" /></>,
  eyeOff: <path d="m3 3 18 18M10.6 6.2A10 10 0 0 1 12 6c6.5 0 10 6 10 6a17 17 0 0 1-2.1 2.8M6.7 6.7C3.7 8.5 2 12 2 12s3.5 6 10 6a9.8 9.8 0 0 0 3.3-.6M10.2 10.2a2.6 2.6 0 0 0 3.6 3.6" />,
  fingerprint: <path d="M8 11a4 4 0 0 1 8 0c0 3-2 5-2 8M12 11v3c0 3 1.2 5 3 7M7 16c.4 1.6 1 3 1.5 4.2M16.5 13c.3 1 .4 2 .2 3.2M6 13.5A8 8 0 0 1 12 5a8 8 0 0 1 7.2 4.5" />,
  home: <path d="m4 10.5 8-6.5 8 6.5V20a1 1 0 0 1-1 1h-5v-6H10v6H5a1 1 0 0 1-1-1z" />,
  inbox: <><path d="M3 12h5l2 3h4l2-3h5" /><path d="M4 6h16l2 6v8a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1v-8z" /></>,
  key: <><circle cx="8" cy="15" r="4" /><path d="m11 13 9-9 2 2-3 1-1 2-2 1" /></>,
  logout: <path d="M10 7V5a1 1 0 0 1 1-1h8v16h-8a1 1 0 0 1-1-1v-2M4 12h11M7 9l-3 3 3 3" />,
  map: <><path d="M12 21s7-5.4 7-11a7 7 0 1 0-14 0c0 5.6 7 11 7 11Z" /><circle cx="12" cy="10" r="2.5" /></>,
  message: <path d="M5 6h14a1 1 0 0 1 1 1v9a1 1 0 0 1-1 1H9l-4 3v-3H5a1 1 0 0 1-1-1V7a1 1 0 0 1 1-1Z" />,
  plus: <path d="M12 5v14M5 12h14" />,
  search: <><circle cx="11" cy="11" r="6" /><path d="m20 20-3.5-3.5" /></>,
  shield: <path d="M12 22s8-3.8 8-10V5l-8-3-8 3v7c0 6.2 8 10 8 10Z" />,
  spark: <path d="M12 3v4M12 17v4M3 12h4M17 12h4M6 6l2.5 2.5M15.5 15.5 18 18M18 6l-2.5 2.5M8.5 15.5 6 18" />,
  user: <><circle cx="12" cy="8" r="3.5" /><path d="M5 20v-1a7 7 0 0 1 14 0v1" /></>,
  userCheck: <><circle cx="10" cy="8" r="3.5" /><path d="M4 20v-1a6 6 0 0 1 9.5-4.8M16 14l2 2 4-4" /></>,
  userX: <><circle cx="10" cy="8" r="3.5" /><path d="M4 20v-1a6 6 0 0 1 8.2-5.5M17 14l4 4M21 14l-4 4" /></>,
  users: <><circle cx="9" cy="8" r="3" /><path d="M3 19v-1a5 5 0 0 1 8-4" /><circle cx="17" cy="9" r="2.5" /><path d="M16 19v-1a4 4 0 0 1 5-3.5" /></>,
};

export function Icon({ name, size = 20 }: { name: Name; size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      {paths[name]}
    </svg>
  );
}
