"use client";

import dynamic from "next/dynamic";
import { useMemo, useState } from "react";
import type { MapMarker } from "@/components/LeafletMap";
import { syriaCenter } from "@/lib/geo";
import type { MapConfig } from "@/lib/map";

const LeafletMap = dynamic(() => import("@/components/LeafletMap").then((module) => module.LeafletMap), {
  ssr: false,
  loading: () => <div className="leaflet-host map-loading" aria-hidden="true" />,
});

interface Approximate {
  latitude: number;
  longitude: number;
  radiusMeters: number;
}

/** Listing page: the approximate area only, never a precise pin. */
export function ApproximateAreaMap({ config, area }: { config: MapConfig; area: Approximate }) {
  const circle = useMemo(() => area, [area]);
  return (
    <section className="panel">
      <h2>الموقع التقريبي</h2>
      <LeafletMap config={config} center={area} zoom={14} circle={circle} label="خريطة المنطقة التقريبية للعقار" />
      <p className="notice">تُعرض منطقة تقريبية حفاظاً على خصوصية المعلن. اطلب العنوان الدقيق عند التواصل.</p>
    </section>
  );
}

/** Search page: results with a known area, collapsible so the list stays first on small screens. */
export function ResultsMap({ config, markers }: { config: MapConfig; markers: MapMarker[] }) {
  // Leaflet measures its container when created, so the map mounts only once the panel is open.
  const [open, setOpen] = useState(false);
  const stable = useMemo(() => markers, [markers]);
  const center = stable[0] ?? syriaCenter;

  return (
    <details className="panel results-map" onToggle={(event) => setOpen(event.currentTarget.open)}>
      <summary>عرض النتائج على الخريطة ({stable.length})</summary>
      {open ? <LeafletMap config={config} center={center} zoom={12} markers={stable} label="خريطة نتائج البحث" /> : null}
      <p className="notice">المواقع تقريبية. الإعلانات بلا موقع محدد لا تظهر على الخريطة.</p>
    </details>
  );
}
