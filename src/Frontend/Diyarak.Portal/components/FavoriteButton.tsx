"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

type State = { status: "loading" } | { status: "signedOut" } | { status: "ready"; isFavorite: boolean; busy: boolean; error?: string };

const messages: Record<string, string> = {
  "market.favorite.limit_reached": "وصلت إلى الحد الأقصى للمحفوظات (200). احذف بعضها أولاً.",
  "market.favorite.listing_not_found": "هذا الإعلان لم يعد متاحاً.",
};

/** Save / unsave a listing. Visitors who are not signed in get a sign-in link that returns here. */
export function FavoriteButton({ listingId }: { listingId: string }) {
  const [state, setState] = useState<State>({ status: "loading" });

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/favorites/${listingId}`, { cache: "no-store" })
      .then((response) => (response.ok ? response.json() : { signedIn: false }))
      .then((body: { signedIn: boolean; isFavorite?: boolean }) => {
        if (cancelled) return;
        setState(body.signedIn ? { status: "ready", isFavorite: Boolean(body.isFavorite), busy: false } : { status: "signedOut" });
      })
      .catch(() => !cancelled && setState({ status: "signedOut" }));
    return () => {
      cancelled = true;
    };
  }, [listingId]);

  if (state.status === "loading") return <span className="favorite-button placeholder" aria-hidden="true" />;

  if (state.status === "signedOut")
    return (
      <Link className="favorite-button" href={`/login?next=${encodeURIComponent(`/listings/${listingId}`)}`}>
        <span aria-hidden="true">♡</span> احفظ الإعلان
      </Link>
    );

  async function toggle() {
    if (state.status !== "ready" || state.busy) return;
    const next = !state.isFavorite;
    setState({ status: "ready", isFavorite: next, busy: true });
    try {
      const response = await fetch(`/api/favorites/${listingId}`, { method: next ? "PUT" : "DELETE" });
      const body = (await response.json()) as { signedIn?: boolean; error?: string };
      if (body.signedIn === false) return setState({ status: "signedOut" });
      if (!response.ok) {
        return setState({ status: "ready", isFavorite: !next, busy: false, error: messages[body.error ?? ""] ?? "تعذر الحفظ. حاول مجدداً." });
      }
      setState({ status: "ready", isFavorite: next, busy: false });
    } catch {
      setState({ status: "ready", isFavorite: !next, busy: false, error: "تعذر الاتصال. حاول مجدداً." });
    }
  }

  return (
    <span className="favorite-wrap">
      <button
        type="button"
        className={`favorite-button${state.isFavorite ? " active" : ""}`}
        aria-pressed={state.isFavorite}
        onClick={toggle}
        disabled={state.busy}
      >
        <span aria-hidden="true">{state.isFavorite ? "♥" : "♡"}</span> {state.isFavorite ? "محفوظ في المفضلة" : "احفظ الإعلان"}
      </button>
      {state.error ? <span className="field-error" role="alert">{state.error}</span> : null}
    </span>
  );
}
