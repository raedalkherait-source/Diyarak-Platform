"use client";

import Link from "next/link";
import { useActionState } from "react";
import { saveSearch, type SaveSearchState } from "@/app/my/actions";

interface Props {
  query: string;
  name: string;
  signedIn: boolean;
  returnTo: string;
}

/** "Save this search": signed-in users save it in place; others sign in first and come back. */
export function SaveSearchButton({ query, name, signedIn, returnTo }: Props) {
  const [state, action, pending] = useActionState<SaveSearchState, FormData>(
    (previous) => saveSearch(query, name, previous),
    {},
  );

  if (!signedIn)
    return (
      <Link className="button secondary save-search" href={`/login?next=${encodeURIComponent(returnTo)}`}>
        🔔 احفظ هذا البحث
      </Link>
    );

  return (
    <form action={action} className="save-search-form">
      {state.status === "saved" ? (
        <p className="success" role="status">{state.message} <Link href="/my/searches">عرضها</Link></p>
      ) : (
        <button className="button secondary save-search" type="submit" disabled={pending}>
          {pending ? "جارٍ الحفظ…" : "🔔 احفظ هذا البحث"}
        </button>
      )}
      {state.status === "error" ? <p className="form-error" role="alert">{state.message}</p> : null}
    </form>
  );
}
