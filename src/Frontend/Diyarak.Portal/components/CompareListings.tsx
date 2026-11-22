"use client";

import { useEffect, useState, useCallback } from "react";
import Link from "next/link";

const STORAGE_KEY = "diyarak_compare";
const MAX_COMPARE = 4;

interface CompareItem {
  id: string;
  headline: string;
  price: string;
}

function load(): CompareItem[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed.slice(0, MAX_COMPARE) : [];
  } catch {
    return [];
  }
}

function save(items: CompareItem[]) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
  } catch { /* ignore */ }
}

export function CompareToggle({ id, headline, price }: CompareItem) {
  const [active, setActive] = useState(false);

  useEffect(() => {
    setActive(load().some((item) => item.id === id));
  }, [id]);

  function toggle() {
    const items = load();
    const index = items.findIndex((item) => item.id === id);
    if (index >= 0) {
      items.splice(index, 1);
      setActive(false);
    } else {
      if (items.length >= MAX_COMPARE) return;
      items.push({ id, headline, price });
      setActive(true);
    }
    save(items);
    window.dispatchEvent(new Event("diyarak_compare_change"));
  }

  return (
    <button type="button" onClick={toggle} className="compare-toggle" aria-pressed={active}>
      {active ? "✓ في المقارنة" : "⊞ قارن"}
    </button>
  );
}

export function CompareBar() {
  const [items, setItems] = useState<CompareItem[]>([]);

  const refresh = useCallback(() => setItems(load()), []);

  useEffect(() => {
    refresh();
    window.addEventListener("diyarak_compare_change", refresh);
    return () => window.removeEventListener("diyarak_compare_change", refresh);
  }, [refresh]);

  function remove(id: string) {
    const updated = load().filter((item) => item.id !== id);
    save(updated);
    setItems(updated);
    window.dispatchEvent(new Event("diyarak_compare_change"));
  }

  function clearAll() {
    save([]);
    setItems([]);
    window.dispatchEvent(new Event("diyarak_compare_change"));
  }

  if (items.length === 0) return null;

  return (
    <div className="compare-bar">
      <div className="container compare-bar-inner">
        <span>المقارنة ({items.length}/{MAX_COMPARE})</span>
        <div className="compare-bar-items">
          {items.map((item) => (
            <span key={item.id} className="compare-bar-chip">
              {item.headline.slice(0, 25)}{item.headline.length > 25 ? "…" : ""}
              <button type="button" onClick={() => remove(item.id)} aria-label={`إزالة ${item.headline}`}>×</button>
            </span>
          ))}
        </div>
        <div className="compare-bar-actions">
          {items.length >= 2 ? (
            <Link href="/compare" className="button small">عرض المقارنة</Link>
          ) : (
            <span className="notice">أضف إعلاناً آخر للمقارنة</span>
          )}
          <button type="button" onClick={clearAll} className="button secondary small">مسح</button>
        </div>
      </div>
    </div>
  );
}
