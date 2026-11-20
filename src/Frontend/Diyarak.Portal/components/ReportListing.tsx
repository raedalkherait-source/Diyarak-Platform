"use client";

import { useState } from "react";
import { reportReasonLabels } from "@/lib/moderation";

export function ReportListing({ listingId }: { listingId: string }) {
  const [state, setState] = useState<"idle" | "sending" | "sent" | "error" | "limited">("idle");

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setState("sending");
    try {
      const response = await fetch(`/api/report/${listingId}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ reason: form.get("reason"), comment: form.get("comment") || undefined }),
      });
      setState(response.ok ? "sent" : response.status === 429 ? "limited" : "error");
    } catch {
      setState("error");
    }
  }

  if (state === "sent") return <p className="success" role="status">شكراً، وصل بلاغك وسيراجعه فريق ديارك.</p>;

  return (
    <details className="report">
      <summary>الإبلاغ عن هذا الإعلان</summary>
      <form onSubmit={submit} className="report-form">
        <label className="field">
          السبب
          <select name="reason" required defaultValue="">
            <option value="" disabled>اختر السبب</option>
            {Object.entries(reportReasonLabels).map(([value, text]) => (
              <option key={value} value={value}>{text}</option>
            ))}
          </select>
        </label>
        <label className="field">
          تفاصيل (اختياري)
          <textarea name="comment" maxLength={500} rows={3} />
        </label>
        <button className="button secondary" type="submit" disabled={state === "sending"}>
          {state === "sending" ? "جارٍ الإرسال…" : "إرسال البلاغ"}
        </button>
        {state === "error" ? <p className="form-error" role="alert">تعذر إرسال البلاغ. حاول مجدداً.</p> : null}
        {state === "limited" ? <p className="form-error" role="alert">أرسلت بلاغات كثيرة. حاول لاحقاً.</p> : null}
      </form>
    </details>
  );
}
