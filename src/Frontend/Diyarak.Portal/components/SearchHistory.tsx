"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

const STORAGE_KEY = "diyarak_search_history";
const MAX_ITEMS = 8;

interface SearchEntry {
  query: string;
  label: string;
  ts: number;
}

export function recordSearch(query: string, label: string) {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    const items: SearchEntry[] = raw ? JSON.parse(raw) : [];
    const filtered = items.filter((item) => item.query !== query);
    filtered.unshift({ query, label, ts: Date.now() });
    localStorage.setItem(STORAGE_KEY, JSON.stringify(filtered.slice(0, MAX_ITEMS)));
  } catch {
    // Storage unavailable
  }
}

export function SearchRecorder({ query, label }: { query: string; label: string }) {
  useEffect(() => {
    if (query) recordSearch(query, label);
  }, [query, label]);
  return null;
}

export function SearchHistory() {
  const [items, setItems] = useState<SearchEntry[]>([]);

  useEffect(() => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) setItems(JSON.parse(raw));
    } catch {
      // Storage unavailable
    }
  }, []);

  if (items.length === 0) return null;

  return (
    <div className="search-history">
      <span style={{ fontSize: "0.8rem", color: "var(--muted)" }}>بحثت مؤخراً:</span>
      {items.map((item) => (
        <Link key={item.query} href={`/search?${item.query}`} className="search-history-chip">
          {item.label}
        </Link>
      ))}
    </div>
  );
}
