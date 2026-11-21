"use client";

import { useActionState } from "react";
import Link from "next/link";
import { bulkImportCsv, type BulkImportState } from "../actions";

const initialState: BulkImportState = {};

export default function ImportPage() {
  const [state, action, pending] = useActionState(bulkImportCsv, initialState);

  return (
    <div className="container section">
      <div className="section-title">
        <h1>استيراد إعلانات من ملف CSV</h1>
        <Link className="button secondary" href="/my">العودة</Link>
      </div>

      <div className="panel">
        <p>ارفع ملف CSV يحتوي على إعلاناتك. يتم إنشاء الإعلانات كمسودات (بدون نشر).</p>
        <details>
          <summary>تنسيق الملف المطلوب</summary>
          <p>الأعمدة المطلوبة:</p>
          <code style={{ display: "block", direction: "ltr", textAlign: "left", padding: "0.5rem", background: "var(--color-surface-alt)", borderRadius: "4px", fontSize: "0.85rem", overflowX: "auto" }}>
            category,city,headline,publishingRole,transactionIntent,price,currency,isOnRequest,allowCalls,allowWhatsApp
          </code>
          <p>أعمدة اختيارية: <code>governorate</code>، <code>district</code></p>
          <p>القيم المقبولة:</p>
          <ul>
            <li><strong>category</strong>: Apartment, House, Plot, InvestmentProperty, GarageOrParkingSpace, CommercialProperty</li>
            <li><strong>publishingRole</strong>: Owner, Tenant, ProfessionalOrAgent</li>
            <li><strong>transactionIntent</strong>: Rent, Sell, RentForLimitedPeriod</li>
            <li><strong>governorate</strong>: Damascus, RifDimashq, Aleppo, Homs, Hama, Latakia, Tartus, Idlib, Daraa, AsSuwayda, Quneitra, DeirEzZor, Raqqa, AlHasakah</li>
            <li><strong>currency</strong>: SYP, USD, EUR</li>
            <li><strong>isOnRequest</strong>: true / false</li>
          </ul>
        </details>

        <form action={action} style={{ marginTop: "1rem" }}>
          <label htmlFor="csv">ملف CSV</label>
          <input type="file" id="csv" name="csv" accept=".csv,text/csv" required />
          <button className="button" type="submit" disabled={pending} style={{ marginTop: "0.5rem" }}>
            {pending ? "جارٍ الاستيراد..." : "استيراد"}
          </button>
        </form>

        {state.status === "error" ? (
          <p className="warning" role="alert" style={{ marginTop: "1rem" }}>{state.message}</p>
        ) : null}

        {state.status === "success" ? (
          <div style={{ marginTop: "1rem" }}>
            <p className="success" role="status">{state.message}</p>
            {state.failed && state.failed > 0 ? (
              <details>
                <summary>{state.failed} صف فشل في الاستيراد</summary>
                <ul style={{ direction: "ltr", textAlign: "left", fontSize: "0.85rem" }}>
                  {state.errors?.map((err, i) => <li key={i}>{err}</li>)}
                </ul>
              </details>
            ) : null}
            <Link className="button" href="/my" style={{ marginTop: "0.5rem", display: "inline-block" }}>عرض إعلاناتي</Link>
          </div>
        ) : null}
      </div>
    </div>
  );
}
