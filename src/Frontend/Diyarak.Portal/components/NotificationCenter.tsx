"use client";

import { useState, useEffect, useRef, useCallback } from "react";
import Link from "next/link";

export interface Notification {
  id: string;
  type: "saved-search" | "payment" | "info";
  title: string;
  message: string;
  href?: string;
  time: string;
}

const STORAGE_KEY = "diyarak_notifications_read";

function getReadIds(): Set<string> {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? new Set(JSON.parse(raw)) : new Set();
  } catch {
    return new Set();
  }
}

function markAllRead(ids: string[]) {
  try {
    const current = getReadIds();
    for (const id of ids) current.add(id);
    const arr = [...current].slice(-100);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(arr));
  } catch { /* ignore */ }
}

export function NotificationBell({ notifications }: { notifications: Notification[] }) {
  const [open, setOpen] = useState(false);
  const [readIds, setReadIds] = useState<Set<string>>(new Set());
  const ref = useRef<HTMLDivElement>(null);
  const bellRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    setReadIds(getReadIds());
  }, []);

  const close = useCallback(() => {
    setOpen(false);
    bellRef.current?.focus();
  }, []);

  useEffect(() => {
    if (!open) return;
    function handleClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    }
    function handleKey(e: KeyboardEvent) {
      if (e.key === "Escape") close();
    }
    document.addEventListener("click", handleClick);
    document.addEventListener("keydown", handleKey);
    return () => {
      document.removeEventListener("click", handleClick);
      document.removeEventListener("keydown", handleKey);
    };
  }, [open, close]);

  const unread = notifications.filter((n) => !readIds.has(n.id)).length;

  function handleOpen() {
    setOpen(!open);
    if (!open && unread > 0) {
      markAllRead(notifications.map((n) => n.id));
      setReadIds(getReadIds());
    }
  }

  return (
    <div className="notification-center" ref={ref}>
      <button ref={bellRef} type="button" onClick={handleOpen} className="notification-bell" aria-expanded={open} aria-haspopup="true" aria-label={unread > 0 ? `الإشعارات (${unread} جديدة)` : "الإشعارات"}>
        🔔
        {unread > 0 ? <span className="notification-badge">{unread}</span> : null}
      </button>
      {open ? (
        <div className="notification-dropdown" role="region" aria-label="الإشعارات">
          <h3>الإشعارات</h3>
          {notifications.length === 0 ? (
            <p className="notification-empty">لا توجد إشعارات جديدة</p>
          ) : (
            <ul className="notification-list" role="list">
              {notifications.map((n) => (
                <li key={n.id} className={readIds.has(n.id) ? "notification-item read" : "notification-item"}>
                  {n.href ? (
                    <Link href={n.href} onClick={() => setOpen(false)}>
                      <strong>{n.title}</strong>
                      <span>{n.message}</span>
                    </Link>
                  ) : (
                    <div>
                      <strong>{n.title}</strong>
                      <span>{n.message}</span>
                    </div>
                  )}
                </li>
              ))}
            </ul>
          )}
        </div>
      ) : null}
    </div>
  );
}
