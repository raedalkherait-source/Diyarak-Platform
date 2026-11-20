"use client";

import { useState, type ReactNode } from "react";
import { LocationPicker } from "@/components/LocationPicker";
import type { MapConfig } from "@/lib/map";
import { Options } from "@/components/Options";
import {
  categoryLabels,
  commercialSubtypeLabels,
  featureLabels,
  finishingLevelLabels,
  governorateLabels,
  label,
  legalTitleLabels,
} from "@/lib/labels";

interface Props {
  value: (name: string) => string;
  error: (name: string) => ReactNode;
  selectedFeatures: string[];
  category: string;
  /** When provided the category is selectable (new listing); otherwise it is shown read-only. */
  onCategoryChange?: (category: string) => void;
  mapConfig: MapConfig;
}

/** Location and property fieldsets shared by the new-listing and property-edit forms. */
export function PropertyFields({ value, error, selectedFeatures, category, onCategoryChange, mapConfig }: Props) {
  const [governorate, setGovernorate] = useState(value("governorate"));
  const latitude = value("latitude");
  const longitude = value("longitude");
  const initialPin = latitude && longitude ? { latitude: Number(latitude), longitude: Number(longitude) } : null;

  return (
    <>
      <fieldset className="panel">
        <legend>الموقع</legend>
        <div className="form-grid">
          <label className="field">
            المحافظة *
            <select name="governorate" defaultValue={value("governorate")} onChange={(event) => setGovernorate(event.target.value)} required>
              <Options labels={governorateLabels} placeholder="اختر" />
            </select>
            {error("governorate")}
          </label>
          <label className="field">
            المدينة *
            <input name="city" defaultValue={value("city")} maxLength={100} required />
            {error("city")}
          </label>
          <label className="field">
            الحي / المنطقة
            <input name="district" defaultValue={value("district")} maxLength={100} />
            {error("district")}
          </label>
          <LocationPicker config={mapConfig} governorate={governorate} initial={initialPin} error={error("location")} />
        </div>
      </fieldset>

      <fieldset className="panel">
        <legend>العقار</legend>
        <div className="form-grid">
          {onCategoryChange ? (
            <label className="field">
              نوع العقار *
              <select name="category" value={category} onChange={(event) => onCategoryChange(event.target.value)} required>
                <Options labels={categoryLabels} placeholder="اختر" />
              </select>
              {error("category")}
            </label>
          ) : (
            <div className="field">
              نوع العقار
              <strong>{label(categoryLabels, category)}</strong>
            </div>
          )}
          {category === "CommercialProperty" ? (
            <label className="field">
              النوع التجاري
              <select name="commercialSubtype" defaultValue={value("commercialSubtype")}>
                <Options labels={commercialSubtypeLabels} placeholder="اختر" />
              </select>
              {error("commercialSubtype")}
            </label>
          ) : null}
          <label className="field">
            المساحة (م²)
            <input name="livingArea" inputMode="decimal" defaultValue={value("livingArea")} />
            {error("livingArea")}
          </label>
          <label className="field">
            عدد الغرف
            <input name="totalRooms" inputMode="numeric" defaultValue={value("totalRooms")} />
            {error("totalRooms")}
          </label>
          <label className="field">
            غرف النوم
            <input name="bedroomCount" inputMode="numeric" defaultValue={value("bedroomCount")} />
            {error("bedroomCount")}
          </label>
          <label className="field">
            الحمامات
            <input name="bathroomCount" inputMode="numeric" defaultValue={value("bathroomCount")} />
            {error("bathroomCount")}
          </label>
          <label className="field">
            الطابق (0 = أرضي)
            <input name="floorNumber" inputMode="numeric" defaultValue={value("floorNumber")} />
            {error("floorNumber")}
          </label>
          <label className="field">
            عدد طوابق البناء
            <input name="buildingFloorCount" inputMode="numeric" defaultValue={value("buildingFloorCount")} />
            {error("buildingFloorCount")}
          </label>
          <label className="field">
            الوضع القانوني
            <select name="legalTitleType" defaultValue={value("legalTitleType")}>
              <Options labels={legalTitleLabels} placeholder="غير محدد" />
            </select>
            {error("legalTitleType")}
          </label>
          <label className="field">
            الإكساء
            <select name="finishingLevel" defaultValue={value("finishingLevel")}>
              <Options labels={finishingLevelLabels} placeholder="غير محدد" />
            </select>
            {error("finishingLevel")}
          </label>
        </div>
        <div className="checkbox-grid" role="group" aria-label="المزايا">
          {Object.entries(featureLabels).map(([feature, text]) => (
            <label key={feature} className="checkbox">
              <input type="checkbox" name="features" value={feature} defaultChecked={selectedFeatures.includes(feature)} />
              {text}
            </label>
          ))}
        </div>
        {error("features")}
      </fieldset>
    </>
  );
}
