"use client";

import { useState } from "react";
import Link from "next/link";

const fmt = new Intl.NumberFormat("ar-SY-u-nu-latn", { maximumFractionDigits: 0 });
const fmtDec = new Intl.NumberFormat("ar-SY-u-nu-latn", { maximumFractionDigits: 2 });

export default function MortgagePage() {
  const [principal, setPrincipal] = useState(50000);
  const [years, setYears] = useState(20);
  const [rate, setRate] = useState(5);

  const monthlyRate = rate / 100 / 12;
  const months = years * 12;
  const monthly = monthlyRate > 0
    ? (principal * monthlyRate * Math.pow(1 + monthlyRate, months)) / (Math.pow(1 + monthlyRate, months) - 1)
    : principal / months;
  const totalPayment = monthly * months;
  const totalInterest = totalPayment - principal;

  return (
    <div className="container section">
      <nav className="breadcrumbs" aria-label="مسار التنقل">
        <Link href="/">الرئيسية</Link><span>›</span>
        <Link href="/tools">أدوات عقارية</Link><span>›</span>
        <span>حاسبة الأقساط</span>
      </nav>
      <h1>حاسبة الأقساط الشهرية</h1>
      <p style={{ color: "var(--muted)", marginBottom: 20 }}>
        احسب القسط الشهري لقرض عقاري. الحساب تقديري وقد يختلف حسب شروط البنك.
      </p>

      <div className="calculator-layout">
        <div className="calculator-inputs">
          <label className="field">
            مبلغ القرض ($)
            <input
              type="text"
              inputMode="numeric"
              value={principal}
              onChange={(e) => { const n = Number(e.target.value.replace(/[,\s]/g, "")); if (!isNaN(n) && n >= 0) setPrincipal(n); }}
            />
          </label>
          <label className="field">
            مدة السداد (سنوات): {years}
            <input type="range" min={1} max={30} value={years} onChange={(e) => setYears(Number(e.target.value))} />
          </label>
          <label className="field">
            نسبة الفائدة السنوية (%): {fmtDec.format(rate)}
            <input type="range" min={0} max={20} step={0.25} value={rate} onChange={(e) => setRate(Number(e.target.value))} />
          </label>
        </div>

        <div className="calculator-results panel">
          <h2>النتائج</h2>
          <dl className="calculator-summary">
            <div>
              <dt>القسط الشهري</dt>
              <dd className="price">{fmtDec.format(monthly)} $</dd>
            </div>
            <div>
              <dt>إجمالي المدفوعات</dt>
              <dd>{fmt.format(totalPayment)} $</dd>
            </div>
            <div>
              <dt>إجمالي الفوائد</dt>
              <dd>{fmt.format(totalInterest)} $</dd>
            </div>
            <div>
              <dt>عدد الأقساط</dt>
              <dd>{months} قسط</dd>
            </div>
          </dl>
        </div>
      </div>
    </div>
  );
}
