"use client";

import { useState, useEffect } from "react";
import Link from "next/link";

const fmt = new Intl.NumberFormat("ar-SY-u-nu-latn", { maximumFractionDigits: 0 });

const governorates: Record<string, string> = {
  Damascus: "دمشق", RifDimashq: "ريف دمشق", Aleppo: "حلب", Homs: "حمص",
  Hama: "حماة", Latakia: "اللاذقية", Tartus: "طرطوس", Idlib: "إدلب",
  Daraa: "درعا", AsSuwayda: "السويداء", Quneitra: "القنيطرة",
  DeirEzZor: "دير الزور", Raqqa: "الرقة", AlHasakah: "الحسكة",
};

const categories: Record<string, string> = {
  Apartment: "شقة", House: "بيت", Plot: "أرض",
  CommercialProperty: "عقار تجاري",
};

interface PriceData {
  governorate: string;
  propertyCategory: string;
  transactionIntent: string;
  averagePriceUsd: number;
  count: number;
}

export default function EstimatorPage() {
  const [governorate, setGovernorate] = useState("Damascus");
  const [category, setCategory] = useState("Apartment");
  const [area, setArea] = useState(100);
  const [data, setData] = useState<PriceData[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch("/api/public/price-reports")
      .then((r) => r.ok ? r.json() : { items: [] })
      .then((d) => setData(d.items ?? []))
      .catch(() => setData([]))
      .finally(() => setLoading(false));
  }, []);

  const match = data.find(
    (d) => d.governorate === governorate && d.propertyCategory === category && d.transactionIntent === "Sell",
  );

  const estimated = match ? Math.round(match.averagePriceUsd * area) : null;
  const perSqm = match ? match.averagePriceUsd : null;

  return (
    <div className="container section">
      <nav className="breadcrumbs" aria-label="مسار التنقل">
        <Link href="/">الرئيسية</Link><span>›</span>
        <Link href="/tools">أدوات عقارية</Link><span>›</span>
        <span>تقدير قيمة العقار</span>
      </nav>
      <h1>تقدير قيمة العقار</h1>
      <p className="notice">
        تقدير تقريبي بناءً على متوسط أسعار المتر المربع في منطقتك. القيمة الفعلية تعتمد على عوامل كثيرة.
      </p>

      <div className="calculator-layout">
        <div className="calculator-inputs">
          <label className="field">
            المحافظة
            <select value={governorate} onChange={(e) => setGovernorate(e.target.value)}>
              {Object.entries(governorates).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </label>
          <label className="field">
            نوع العقار
            <select value={category} onChange={(e) => setCategory(e.target.value)}>
              {Object.entries(categories).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </label>
          <label className="field">
            المساحة (م²)
            <input
              type="text"
              inputMode="numeric"
              value={area}
              onChange={(e) => { const n = Number(e.target.value.replace(/[,\s]/g, "")); if (!isNaN(n) && n >= 0) setArea(n); }}
            />
          </label>
        </div>

        <div className="calculator-results panel">
          <h2>التقدير</h2>
          {loading ? (
            <p>جارٍ التحميل...</p>
          ) : !match ? (
            <p className="notice">لا تتوفر بيانات كافية لهذا النوع في المحافظة المختارة.</p>
          ) : (
            <dl className="calculator-summary">
              <div>
                <dt>القيمة التقديرية</dt>
                <dd className="price">{fmt.format(estimated!)} $</dd>
              </div>
              <div>
                <dt>متوسط سعر المتر المربع</dt>
                <dd>{fmt.format(perSqm!)} $/م²</dd>
              </div>
              <div>
                <dt>عدد الإعلانات المرجعية</dt>
                <dd>{match.count} إعلان</dd>
              </div>
            </dl>
          )}
          <p className="notice disclaimer">
            هذا تقدير تقريبي فقط ولا يُعتبر تقييماً رسمياً. تعتمد القيمة الفعلية على الموقع الدقيق والإكساء والطابق وعوامل أخرى.
          </p>
        </div>
      </div>
    </div>
  );
}
