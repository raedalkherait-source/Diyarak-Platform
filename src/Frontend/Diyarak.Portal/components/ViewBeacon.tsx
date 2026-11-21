"use client";

import { useEffect } from "react";

/** Counts one view per listing per browser session. */
export function ViewBeacon({ listingId }: { listingId: string }) {
  useEffect(() => {
    const key = `diyarak:viewed:${listingId}`;
    try {
      if (sessionStorage.getItem(key)) return;
      sessionStorage.setItem(key, "1");
    } catch {
      // Storage can be unavailable (private mode); counting once per load is acceptable then.
    }
    void fetch(`/api/view/${listingId}`, { method: "POST", keepalive: true }).catch(() => undefined);
  }, [listingId]);

  return null;
}
