"use client";

import dynamic from "next/dynamic";
import { useState } from "react";
import { governorateCenters, syriaCenter, type LatLng } from "@/lib/geo";
import type { MapConfig } from "@/lib/map";

const LeafletMap = dynamic(() => import("@/components/LeafletMap").then((module) => module.LeafletMap), {
  ssr: false,
  loading: () => <div className="leaflet-host map-loading" aria-hidden="true" />,
});

interface Props {
  config: MapConfig;
  governorate: string;
  initial: LatLng | null;
  error?: React.ReactNode;
}

/** Optional pin for the property's position. Visitors only ever see an approximate area. */
export function LocationPicker({ config, governorate, initial, error }: Props) {
  const [pin, setPin] = useState<LatLng | null>(initial);
  const center = pin ?? governorateCenters[governorate] ?? syriaCenter;

  return (
    <div className="field wide location-picker">
      <span>الموقع على الخريطة (اختياري)</span>
      <p className="notice">اضغط على الخريطة لتحديد موقع العقار. يرى الزوار منطقة تقريبية بقطر كيلومتر تقريباً، لا العنوان الدقيق.</p>
      <LeafletMap
        config={config}
        center={center}
        zoom={pin ? 16 : governorateCenters[governorate] ? 12 : 6}
        pin={pin}
        onPick={(point) => setPin({ latitude: round6(point.latitude), longitude: round6(point.longitude) })}
        label="خريطة لتحديد موقع العقار"
      />
      <input type="hidden" name="latitude" value={pin ? String(pin.latitude) : ""} />
      <input type="hidden" name="longitude" value={pin ? String(pin.longitude) : ""} />
      <div className="toolbar">
        <span className="notice" aria-live="polite">{pin ? "تم تحديد الموقع." : "لم يُحدد موقع."}</span>
        {pin ? (
          <button type="button" className="button secondary" onClick={() => setPin(null)}>إزالة الموقع</button>
        ) : null}
      </div>
      {error}
    </div>
  );
}

function round6(value: number): number {
  return Math.round(value * 1e6) / 1e6;
}
