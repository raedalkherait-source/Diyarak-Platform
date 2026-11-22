"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

type Currency = "USD" | "SYP";

interface Prefs {
  currency: Currency;
  exchangeRate: number;
  defaultGovernorate: string;
  searchAlertsEnabled: boolean;
}

const DEFAULTS: Prefs = {
  currency: "USD",
  exchangeRate: 14500,
  defaultGovernorate: "",
  searchAlertsEnabled: true,
};

function load(): Prefs {
  try {
    const currency = (localStorage.getItem("diyarak_currency") as Currency) || DEFAULTS.currency;
    const rateStr = localStorage.getItem("diyarak_usd_syp_rate");
    const exchangeRate = rateStr ? Number(rateStr) || DEFAULTS.exchangeRate : DEFAULTS.exchangeRate;
    const defaultGovernorate = localStorage.getItem("diyarak_default_governorate") ?? "";
    const alertsStr = localStorage.getItem("diyarak_search_alerts");
    const searchAlertsEnabled = alertsStr !== "false";
    return { currency, exchangeRate, defaultGovernorate, searchAlertsEnabled };
  } catch {
    return DEFAULTS;
  }
}

function persist(prefs: Prefs) {
  try {
    localStorage.setItem("diyarak_currency", prefs.currency);
    localStorage.setItem("diyarak_usd_syp_rate", String(prefs.exchangeRate));
    localStorage.setItem("diyarak_default_governorate", prefs.defaultGovernorate);
    localStorage.setItem("diyarak_search_alerts", String(prefs.searchAlertsEnabled));
    window.dispatchEvent(new Event("diyarak_currency_change"));
  } catch { /* ignore */ }
}

const governorates: Record<string, string> = {
  Damascus: "دمشق", RifDimashq: "ريف دمشق", Aleppo: "حلب", Homs: "حمص",
  Hama: "حماة", Latakia: "اللاذقية", Tartus: "طرطوس", Idlib: "إدلب",
  Daraa: "درعا", AsSuwayda: "السويداء", Quneitra: "القنيطرة",
  DeirEzZor: "دير الزور", Raqqa: "الرقة", AlHasakah: "الحسكة",
};

export default function SettingsPage() {
  const [prefs, setPrefs] = useState<Prefs>(DEFAULTS);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    setPrefs(load());
  }, []);

  function update(partial: Partial<Prefs>) {
    setPrefs((prev) => ({ ...prev, ...partial }));
    setSaved(false);
  }

  function save() {
    persist(prefs);
    setSaved(true);
  }

  return (
    <div className="container section">
      <div className="section-title">
        <h1>الإعدادات</h1>
        <Link href="/my" className="button secondary">← العودة</Link>
      </div>

      <section className="panel settings-section">
        <h2>العملة وسعر الصرف</h2>
        <label className="field">
          العملة المفضلة
          <select value={prefs.currency} onChange={(e) => update({ currency: e.target.value as Currency })}>
            <option value="USD">دولار أمريكي ($)</option>
            <option value="SYP">ليرة سورية (ل.س)</option>
          </select>
        </label>
        <label className="field">
          سعر صرف الدولار (بالليرة)
          <input
            type="text"
            inputMode="numeric"
            value={prefs.exchangeRate}
            onChange={(e) => {
              const n = Number(e.target.value.replace(/[,\s]/g, ""));
              if (!isNaN(n)) update({ exchangeRate: n });
            }}
          />
        </label>
      </section>

      <section className="panel settings-section">
        <h2>البحث</h2>
        <label className="field">
          المحافظة الافتراضية
          <select value={prefs.defaultGovernorate} onChange={(e) => update({ defaultGovernorate: e.target.value })}>
            <option value="">كل المحافظات</option>
            {Object.entries(governorates).map(([key, name]) => (
              <option key={key} value={key}>{name}</option>
            ))}
          </select>
        </label>
      </section>

      <section className="panel settings-section">
        <h2>الإشعارات</h2>
        <label className="checkbox-field">
          <input
            type="checkbox"
            checked={prefs.searchAlertsEnabled}
            onChange={(e) => update({ searchAlertsEnabled: e.target.checked })}
          />
          تلقي تنبيهات عمليات البحث المحفوظة
        </label>
      </section>

      <div className="settings-actions">
        <button type="button" className="button" onClick={save}>حفظ الإعدادات</button>
        {saved ? <span className="success">تم الحفظ</span> : null}
      </div>
    </div>
  );
}
