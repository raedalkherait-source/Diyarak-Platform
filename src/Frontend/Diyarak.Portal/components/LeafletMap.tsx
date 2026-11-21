"use client";

import "leaflet/dist/leaflet.css";
import { useEffect, useRef } from "react";
import type { Circle, CircleMarker, LayerGroup, Map as LeafletInstance } from "leaflet";
import type { LatLng } from "@/lib/geo";
import type { MapConfig } from "@/lib/map";

export interface MapMarker extends LatLng {
  href: string;
  label: string;
}

interface Props {
  config: MapConfig;
  center: LatLng;
  zoom: number;
  /** Approximate area shown as a circle (listing page). */
  circle?: LatLng & { radiusMeters: number };
  /** Clickable result markers (search page); the view fits them. */
  markers?: MapMarker[];
  /** Picker mode: the current pin and a callback for map clicks. */
  pin?: LatLng | null;
  onPick?: (point: LatLng) => void;
  className?: string;
  label: string;
}

const brand = "#0f6b5c";

/**
 * Thin Leaflet wrapper. Leaflet touches `window`, so it is imported inside the effect and the
 * component renders an empty container on the server. Circle markers avoid image assets.
 */
export function LeafletMap({ config, center, zoom, circle, markers, pin, onPick, className, label }: Props) {
  const container = useRef<HTMLDivElement>(null);
  const map = useRef<LeafletInstance | null>(null);
  const pinLayer = useRef<CircleMarker | null>(null);
  const overlay = useRef<LayerGroup | Circle | null>(null);
  const pickRef = useRef(onPick);
  pickRef.current = onPick;

  // Create the map once.
  useEffect(() => {
    let disposed = false;
    void import("leaflet").then((L) => {
      if (disposed || !container.current || map.current) return;
      const instance = L.map(container.current, { scrollWheelZoom: false, attributionControl: true });
      instance.attributionControl.setPrefix(false);
      L.tileLayer(config.tileUrl, { attribution: config.attribution, maxZoom: 19 }).addTo(instance);
      instance.setView([center.latitude, center.longitude], zoom);
      instance.on("click", (event) => pickRef.current?.({ latitude: event.latlng.lat, longitude: event.latlng.lng }));
      map.current = instance;
      container.current.dispatchEvent(new Event("leaflet-ready"));
    });
    return () => {
      disposed = true;
      // Stop pan/zoom animations first: removing a map mid-animation throws in Leaflet.
      map.current?.stop();
      map.current?.remove();
      map.current = null;
    };
    // The map is created once; later prop changes are applied by the effects below.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Circle or markers.
  useEffect(() => {
    let cancelled = false;
    const apply = () =>
      void import("leaflet").then((L) => {
        const instance = map.current;
        if (cancelled || !instance) return;
        overlay.current?.remove();
        overlay.current = null;
        if (circle) {
          overlay.current = L.circle([circle.latitude, circle.longitude], {
            radius: circle.radiusMeters, color: brand, fillColor: brand, fillOpacity: 0.18, weight: 2,
          }).addTo(instance);
        } else if (markers && markers.length > 0) {
          const group = L.featureGroup(
            markers.map((marker) =>
              L.circleMarker([marker.latitude, marker.longitude], {
                radius: 9, color: "#fff", weight: 2, fillColor: brand, fillOpacity: 0.95,
              }).bindPopup(() => {
                const link = document.createElement("a");
                link.href = marker.href;
                link.textContent = marker.label;
                return link;
              }),
            ),
          ).addTo(instance);
          overlay.current = group;
          instance.fitBounds(group.getBounds().pad(0.2), { maxZoom: 15, animate: false });
        }
      });
    if (map.current) apply();
    else container.current?.addEventListener("leaflet-ready", apply, { once: true });
    return () => {
      cancelled = true;
    };
  }, [circle, markers]);

  // Picker pin.
  useEffect(() => {
    if (pin === undefined) return;
    let cancelled = false;
    const apply = () =>
      void import("leaflet").then((L) => {
        const instance = map.current;
        if (cancelled || !instance) return;
        pinLayer.current?.remove();
        pinLayer.current = pin
          ? L.circleMarker([pin.latitude, pin.longitude], { radius: 10, color: "#fff", weight: 3, fillColor: "#c2410c", fillOpacity: 1 }).addTo(instance)
          : null;
      });
    if (map.current) apply();
    else container.current?.addEventListener("leaflet-ready", apply, { once: true });
    return () => {
      cancelled = true;
    };
  }, [pin]);

  // Re-centre when the requested centre changes (e.g. another governorate was chosen).
  useEffect(() => {
    if (map.current && !pin) map.current.setView([center.latitude, center.longitude], zoom, { animate: false });
    // Only react to centre changes; zoom follows them.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [center.latitude, center.longitude]);

  return <div ref={container} className={`leaflet-host ${className ?? ""}`} role="region" aria-label={label} />;
}
