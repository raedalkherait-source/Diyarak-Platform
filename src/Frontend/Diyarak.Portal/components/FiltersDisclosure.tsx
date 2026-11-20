"use client";

import { useEffect, useRef } from "react";

/**
 * Collapsible wrapper for the search filters. It starts collapsed so phones show results first,
 * and opens automatically on wide screens where the filters sit beside the results.
 */
export function FiltersDisclosure({ activeCount, children }: { activeCount: number; children: React.ReactNode }) {
  const ref = useRef<HTMLDetailsElement>(null);

  useEffect(() => {
    const wide = window.matchMedia("(min-width: 901px)");
    const sync = () => {
      if (ref.current && wide.matches) ref.current.open = true;
    };
    sync();
    wide.addEventListener("change", sync);
    return () => wide.removeEventListener("change", sync);
  }, []);

  return (
    <details ref={ref} className="filters-disclosure">
      <summary className="button secondary">
        تصفية النتائج{activeCount > 0 ? ` (${activeCount})` : ""}
      </summary>
      {children}
    </details>
  );
}
