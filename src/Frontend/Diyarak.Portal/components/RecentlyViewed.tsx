"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

const STORAGE_KEY = "diyarak_recent";
const MAX_ITEMS = 10;

interface RecentItem {
  id: string;
  headline: string;
  price: string;
  viewedAt: number;
}

export function recordView(id: string, headline: string, price: string) {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    const items: RecentItem[] = raw ? JSON.parse(raw) : [];
    const filtered = items.filter((item) => item.id !== id);
    filtered.unshift({ id, headline, price, viewedAt: Date.now() });
    localStorage.setItem(STORAGE_KEY, JSON.stringify(filtered.slice(0, MAX_ITEMS)));
  } catch {
    // Storage unavailable
  }
}

export function RecentlyViewedRecorder({ id, headline, price }: { id: string; headline: string; price: string }) {
  useEffect(() => {
    recordView(id, headline, price);
  }, [id, headline, price]);
  return null;
}

export function RecentlyViewedList({ excludeId }: { excludeId?: string }) {
  const [items, setItems] = useState<RecentItem[]>([]);

  useEffect(() => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const parsed: RecentItem[] = JSON.parse(raw);
        setItems(parsed.filter((item) => item.id !== excludeId).slice(0, 5));
      }
    } catch {
      // Storage unavailable
    }
  }, [excludeId]);

  if (items.length === 0) return null;

  return (
    <section className="panel" style={{ marginBlockStart: "1.5rem" }}>
      <h2>شاهدت مؤخراً</h2>
      <div style={{ display: "flex", flexDirection: "column", gap: "0.5rem" }}>
        {items.map((item) => (
          <Link
            key={item.id}
            href={`/listings/${item.id}`}
            style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "0.5rem", border: "1px solid var(--border)", borderRadius: "6px", textDecoration: "none", color: "inherit" }}
          >
            <span style={{ fontSize: "0.875rem" }}>{item.headline}</span>
            <span style={{ fontSize: "0.8rem", color: "var(--muted)", whiteSpace: "nowrap" }}>{item.price}</span>
          </Link>
        ))}
      </div>
    </section>
  );
}
