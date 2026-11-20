"use client";

import { useActionState, useState } from "react";
import { createListing, type CreateListingState } from "@/app/my/actions";
import { Options } from "@/components/Options";
import { PropertyFields } from "@/components/PropertyFields";
import { publishingRoleLabels, transactionIntentLabels } from "@/lib/labels";

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

      <PropertyFields
        value={value}
        error={error}
        selectedFeatures={selectedFeatures}
        category={category}
        onCategoryChange={setCategory}
      />

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
