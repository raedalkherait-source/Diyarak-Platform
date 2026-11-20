"use client";

import { useActionState } from "react";
import { publishListing, type ActionResult } from "@/app/my/actions";

export function PublishButton({ listingId, version, photoCount }: { listingId: string; version: number; photoCount: number }) {
  const [state, action, pending] = useActionState<ActionResult, FormData>(
    (previous) => publishListing(listingId, version, previous),
    { ok: false },
  );

  return (
    <form action={action} className="publish">
      {photoCount === 0 ? <p className="notice">ننصح بإضافة صورة واحدة على الأقل قبل النشر.</p> : null}
      <p className="notice">بعد النشر لا يمكن تعديل الإعلان أو صوره.</p>
      <button className="button" type="submit" disabled={pending}>
        {pending ? "جارٍ النشر…" : "نشر الإعلان"}
      </button>
      {state.error ? <p className="form-error" role="alert">{state.error}</p> : null}
    </form>
  );
}
