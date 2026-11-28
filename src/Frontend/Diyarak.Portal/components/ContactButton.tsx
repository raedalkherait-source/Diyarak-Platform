"use client";

import { useState } from "react";
import { displayPhone, whatsAppLink } from "@/lib/contact";
import type { PublicListingContact } from "@/lib/types";

type State =
  | { kind: "idle" }
  | { kind: "loading" }
  | { kind: "shown"; contact: PublicListingContact }
  | { kind: "error"; message: string };

export function ContactButton({ listingId, headline }: { listingId: string; headline: string }) {
  const [state, setState] = useState<State>({ kind: "idle" });

  async function reveal() {
    setState({ kind: "loading" });
    try {
      const response = await fetch(`/api/contact/${listingId}`, { cache: "no-store" });
      if (response.status === 429) {
        setState({ kind: "error", message: "طلبات كثيرة. حاول مرة أخرى بعد قليل." });
        return;
      }
      if (!response.ok) {
        setState({ kind: "error", message: "معلومات التواصل غير متاحة لهذا الإعلان." });
        return;
      }
      setState({ kind: "shown", contact: (await response.json()) as PublicListingContact });
    } catch {
      setState({ kind: "error", message: "تعذر الاتصال. تحقق من الإنترنت وحاول مجدداً." });
    }
  }

  if (state.kind === "shown") {
    const { contact } = state;
    return (
      <div className="contact-actions">
        {contact.callUrl ? (
          <a className="button" href={contact.callUrl} dir="ltr" translate="no">
            📞 {displayPhone(contact.phoneNumber)}
          </a>
        ) : null}
        {contact.whatsAppUrl ? (
          <a
            className="button whatsapp"
            href={whatsAppLink(contact.whatsAppUrl, headline, window.location.href)}
            target="_blank"
            rel="noopener noreferrer nofollow"
            referrerPolicy="no-referrer"
          >
            مراسلة عبر واتساب
          </a>
        ) : null}
      </div>
    );
  }

  return (
    <div className="contact-actions">
      <button className="button" type="button" onClick={reveal} disabled={state.kind === "loading"}>
        {state.kind === "loading" ? "جارٍ التحميل…" : "إظهار رقم التواصل"}
      </button>
      {state.kind === "error" ? <p className="notice" role="alert">{state.message}</p> : null}
    </div>
  );
}
