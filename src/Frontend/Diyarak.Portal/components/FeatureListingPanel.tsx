"use client";

import { useActionState, useState } from "react";
import { createPaymentOrder, type PaymentFormState } from "@/app/my/actions";

export interface PaymentCatalog {
  products: { product: string; days: number; amount: number; currency: string }[];
  methods: { method: string; instructions: string }[];
}

export interface PaymentOrderView {
  orderId: string;
  listingId: string;
  days: number;
  amount: number;
  currency: string;
  method: string;
  reference: string;
  status: "Pending" | "Paid" | "Rejected";
  decisionNote: string | null;
  createdAt: string;
}

const methodLabels: Record<string, string> = {
  BankTransfer: "تحويل مصرفي",
  SyriatelCash: "Syriatel Cash",
  MtnCash: "MTN Cash",
};

const numberFormat = new Intl.NumberFormat("ar-SY-u-nu-latn");
const dateFormat = new Intl.DateTimeFormat("ar-SY-u-nu-latn", { dateStyle: "medium", timeZone: "Asia/Damascus" });

/** Order a featured period: choose a duration and a local payment method, pay, enter the reference. */
export function FeatureListingPanel({
  listingId,
  catalog,
  featuredUntil,
  orders,
}: {
  listingId: string;
  catalog: PaymentCatalog;
  featuredUntil: string | null;
  orders: PaymentOrderView[];
}) {
  const [state, action, pending] = useActionState<PaymentFormState, FormData>(
    (previous, form) => createPaymentOrder(listingId, previous, form),
    {},
  );
  const [method, setMethod] = useState(catalog.methods[0]?.method ?? "");
  const instructions = catalog.methods.find((item) => item.method === method)?.instructions;
  const pendingOrder = orders.find((order) => order.status === "Pending");
  const lastRejected = orders.find((order) => order.status === "Rejected");

  return (
    <section className="panel feature-panel">
      <h2>ميّز إعلانك</h2>
      {featuredUntil ? (
        <p className="success">★ إعلانك مميز ويظهر أولاً في نتائج البحث حتى {dateFormat.format(new Date(featuredUntil))}.</p>
      ) : (
        <p className="notice">الإعلانات المميزة تظهر أولاً في نتائج البحث وعليها شارة «مميز».</p>
      )}

      {pendingOrder ? (
        <p className="notice" role="status">
          طلب دفع قيد المراجعة: {pendingOrder.days} يوماً · {numberFormat.format(pendingOrder.amount)} {pendingOrder.currency} ·{" "}
          {methodLabels[pendingOrder.method] ?? pendingOrder.method} · رقم العملية <bdi>{pendingOrder.reference}</bdi>. سيُفعّل التمييز بعد تأكيد وصول المبلغ.
        </p>
      ) : (
        <form action={action} className="listing-form" noValidate>
          {lastRejected?.decisionNote ? <p className="form-error">رُفض الطلب السابق: {lastRejected.decisionNote}</p> : null}
          {state.status === "error" ? <p className="form-error" role="alert">{state.message}</p> : null}
          <div className="intent-tabs" role="radiogroup" aria-label="مدة التمييز">
            {catalog.products.map((product, index) => (
              <label key={product.product}>
                <input type="radio" name="product" value={product.product} defaultChecked={index === 0} />
                <span>
                  {product.days} يوماً — {numberFormat.format(product.amount)} {product.currency}
                </span>
              </label>
            ))}
          </div>
          <label className="field">
            طريقة الدفع
            <select name="method" value={method} onChange={(event) => setMethod(event.target.value)}>
              {catalog.methods.map((item) => (
                <option key={item.method} value={item.method}>{methodLabels[item.method] ?? item.method}</option>
              ))}
            </select>
          </label>
          {instructions ? <p className="payment-instructions">{instructions}</p> : null}
          <label className="field">
            رقم العملية أو الحوالة *
            <input name="reference" maxLength={60} required dir="ltr" />
          </label>
          <button className="button" type="submit" disabled={pending}>{pending ? "جارٍ الإرسال…" : "أرسلت المبلغ — تأكيد الطلب"}</button>
        </form>
      )}
    </section>
  );
}
