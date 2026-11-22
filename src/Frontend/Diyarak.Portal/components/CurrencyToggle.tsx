"use client";

import { useEffect, useState } from "react";

const STORAGE_KEY = "diyarak_currency";
const RATE_KEY = "diyarak_usd_syp_rate";
const DEFAULT_RATE = 14500;

type Currency = "USD" | "SYP";

function getStoredCurrency(): Currency {
  try {
    const v = localStorage.getItem(STORAGE_KEY);
    if (v === "USD" || v === "SYP") return v;
  } catch { /* ignore */ }
  return "USD";
}

function getRate(): number {
  try {
    const v = localStorage.getItem(RATE_KEY);
    if (v) {
      const n = Number(v);
      if (n > 0 && Number.isFinite(n)) return n;
    }
  } catch { /* ignore */ }
  return DEFAULT_RATE;
}

export function CurrencyToggle() {
  const [currency, setCurrency] = useState<Currency>("USD");
  const [rate, setRate] = useState(DEFAULT_RATE);
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState("");

  useEffect(() => {
    setCurrency(getStoredCurrency());
    setRate(getRate());
  }, []);

  function toggle() {
    const next: Currency = currency === "USD" ? "SYP" : "USD";
    setCurrency(next);
    try { localStorage.setItem(STORAGE_KEY, next); } catch { /* ignore */ }
    window.dispatchEvent(new Event("diyarak_currency_change"));
  }

  function saveRate() {
    const n = Number(draft.replace(/[,\s]/g, ""));
    if (n > 0 && Number.isFinite(n)) {
      setRate(n);
      try { localStorage.setItem(RATE_KEY, String(n)); } catch { /* ignore */ }
      window.dispatchEvent(new Event("diyarak_currency_change"));
    }
    setEditing(false);
  }

  return (
    <div className="currency-toggle">
      <button type="button" onClick={toggle} className="currency-toggle-btn" title="تبديل العملة">
        {currency === "USD" ? "$ دولار" : "ل.س ليرة"}
      </button>
      <button
        type="button"
        onClick={() => { setDraft(String(rate)); setEditing(!editing); }}
        className="currency-rate-btn"
        title="تعديل سعر الصرف"
      >
        ⇄ {new Intl.NumberFormat("ar-SY-u-nu-latn").format(rate)}
      </button>
      {editing ? (
        <div className="currency-rate-editor">
          <label>
            سعر الدولار بالليرة:
            <input
              type="text"
              inputMode="numeric"
              value={draft}
              onChange={(e) => setDraft(e.target.value)}
              onKeyDown={(e) => { if (e.key === "Enter") saveRate(); }}
              autoFocus
            />
          </label>
          <button type="button" className="button small" onClick={saveRate}>حفظ</button>
        </div>
      ) : null}
    </div>
  );
}

export function useCurrency(): { currency: Currency; rate: number } {
  const [currency, setCurrency] = useState<Currency>("USD");
  const [rate, setRate] = useState(DEFAULT_RATE);

  useEffect(() => {
    const refresh = () => {
      setCurrency(getStoredCurrency());
      setRate(getRate());
    };
    refresh();
    window.addEventListener("diyarak_currency_change", refresh);
    return () => window.removeEventListener("diyarak_currency_change", refresh);
  }, []);

  return { currency, rate };
}

export function convertPrice(
  amount: number | null,
  fromCurrency: string | null,
  toCurrency: Currency,
  rate: number,
): { amount: number | null; currency: string } {
  if (amount === null) return { amount: null, currency: toCurrency };
  if (!fromCurrency || fromCurrency === toCurrency) return { amount, currency: toCurrency };
  if (fromCurrency === "USD" && toCurrency === "SYP") return { amount: Math.round(amount * rate), currency: "SYP" };
  if (fromCurrency === "SYP" && toCurrency === "USD") return { amount: Math.round((amount / rate) * 100) / 100, currency: "USD" };
  return { amount, currency: fromCurrency };
}
