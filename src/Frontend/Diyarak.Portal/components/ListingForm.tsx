"use client";

import { useActionState, useState } from "react";
import { createListing, type CreateListingState } from "@/app/my/actions";
import { Options } from "@/components/Options";
import {
  categoryLabels,
  commercialSubtypeLabels,
  featureLabels,
  finishingLevelLabels,
  governorateLabels,
  legalTitleLabels,
  publishingRoleLabels,
  transactionIntentLabels,
} from "@/lib/labels";

export function ListingForm() {
  const [state, action, pending] = useActionState<CreateListingState, FormData>(createListing, {});
  const values = state.values ?? {};
  const value = (name: string) => (typeof values[name] === "string" ? (values[name] as string) : "");
  const [category, setCategory] = useState(value("category"));
  const [priceMode, setPriceMode] = useState(value("priceMode") || "known");
  const selectedFeatures = Array.isArray(values.features) ? values.features : [];

  const error = (name: string) =>
    state.errors?.[name] ? <span className="field-error">{state.errors[name]}</span> : null;

  return (
    <form action={action} className="listing-form" noValidate>
      {state.formError ? <p className="form-error" role="alert">{state.formError}</p> : null}

      <fieldset className="panel">
        <legend>العرض</legend>
        <div className="form-grid">
          <label className="field">
            نوع العرض *
            <select name="transactionIntent" defaultValue={value("transactionIntent")} required>
              <Options labels={transactionIntentLabels} placeholder="اختر" />
            </select>
            {error("transactionIntent")}
          </label>
          <label className="field">
            صفتك *
            <select name="publishingRole" defaultValue={value("publishingRole") || "Owner"} required>
              <Options labels={publishingRoleLabels} placeholder="اختر" />
            </select>
            {error("publishingRole")}
          </label>
          <label className="field wide">
            عنوان الإعلان *
            <input name="headline" defaultValue={value("headline")} maxLength={120} placeholder="مثال: شقة سوبر ديلوكس في المزة مع إطلالة" required />
            {error("headline")}
          </label>
        </div>
      </fieldset>

      <fieldset className="panel">
        <legend>الموقع</legend>
        <div className="form-grid">
          <label className="field">
            المحافظة *
            <select name="governorate" defaultValue={value("governorate")} required>
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
        </div>
      </fieldset>

      <fieldset className="panel">
        <legend>العقار</legend>
        <div className="form-grid">
          <label className="field">
            نوع العقار *
            <select name="category" value={category} onChange={(event) => setCategory(event.target.value)} required>
              <Options labels={categoryLabels} placeholder="اختر" />
            </select>
            {error("category")}
          </label>
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

      <fieldset className="panel">
        <legend>السعر</legend>
        <div className="intent-tabs" role="radiogroup" aria-label="طريقة عرض السعر">
          <label>
            <input type="radio" name="priceMode" value="known" checked={priceMode === "known"} onChange={() => setPriceMode("known")} />
            <span>سعر محدد</span>
          </label>
          <label>
            <input type="radio" name="priceMode" value="onRequest" checked={priceMode === "onRequest"} onChange={() => setPriceMode("onRequest")} />
            <span>عند الاتصال</span>
          </label>
        </div>
        {priceMode === "known" ? (
          <div className="form-grid">
            <label className="field">
              السعر *
              <input name="priceAmount" inputMode="decimal" defaultValue={value("priceAmount")} />
              {error("priceAmount")}
            </label>
            <label className="field">
              العملة
              <select name="priceCurrency" defaultValue={value("priceCurrency") || "USD"}>
                <option value="USD">دولار أمريكي</option>
                <option value="SYP">ليرة سورية</option>
              </select>
              {error("priceCurrency")}
            </label>
          </div>
        ) : null}
      </fieldset>

      <fieldset className="panel">
        <legend>التواصل</legend>
        <p className="notice">يظهر للزوار رقمك الموثّق الذي سجّلت الدخول به، فقط عند ضغطهم على زر إظهار الرقم.</p>
        <label className="checkbox">
          <input type="checkbox" name="allowCalls" defaultChecked={value("allowCalls") === "on" || !state.values} /> السماح بالاتصال الهاتفي
        </label>
        <label className="checkbox">
          <input type="checkbox" name="allowWhatsApp" defaultChecked={value("allowWhatsApp") === "on" || !state.values} /> السماح بالمراسلة عبر واتساب
        </label>
      </fieldset>

      <button className="button" type="submit" disabled={pending}>
        {pending ? "جارٍ الحفظ…" : "حفظ كمسودة والمتابعة لإضافة الصور"}
      </button>
    </form>
  );
}
