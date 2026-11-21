"use client";

import dynamic from "next/dynamic";
import { useMemo, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import type { MapMarker } from "@/components/LeafletMap";
import { searchWithArea, type MapBounds } from "@/lib/search";
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

/**
 * Search page: results with a known area. Collapsible so the list stays first on small screens;
 * open by default when the search is limited to a map area. After the visitor moves the map, a
 * button searches the visible area.
 */
export function ResultsMap({
  config,
  markers,
  query,
  area,
}: {
  config: MapConfig;
  markers: MapMarker[];
  query: string;
  area: MapBounds | null;
}) {
  const router = useRouter();
  // Leaflet measures its container when created, so the map mounts only once the panel is open.
  const [open, setOpen] = useState(area !== null);
  const [moved, setMoved] = useState<MapBounds | null>(null);
  const stable = useMemo(() => markers, [markers]);
  const center = stable[0] ?? (area ? { latitude: (area.south + area.north) / 2, longitude: (area.west + area.east) / 2 } : syriaCenter);

  return (
    <details className="panel results-map" open={area !== null} onToggle={(event) => setOpen(event.currentTarget.open)}>
      <summary>{area ? "البحث ضمن منطقة على الخريطة" : `عرض النتائج على الخريطة (${stable.length})`}</summary>
      {open ? (
        <LeafletMap
          config={config}
          center={center}
          zoom={area ? 13 : 12}
          markers={stable}
          onUserMove={setMoved}
          label="خريطة نتائج البحث"
        />
      ) : null}
      <div className="toolbar map-actions">
        {moved ? (
          <button type="button" className="button" onClick={() => router.push(searchWithArea(query, moved))}>
            ابحث في هذه المنطقة
          </button>
        ) : (
          <span className="notice">حرّك الخريطة أو كبّرها ثم ابحث في المنطقة الظاهرة.</span>
        )}
        {area ? <Link className="button secondary" href={searchWithArea(query, null)}>إلغاء حدود الخريطة</Link> : null}
      </div>
      <p className="notice">المواقع تقريبية. الإعلانات بلا موقع محدد لا تظهر على الخريطة.</p>
    </details>
  );
}
