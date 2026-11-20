"use client";

import { useActionState, useState } from "react";
import { updateListing, type EditListingState } from "@/app/my/actions";
import { Options } from "@/components/Options";
import { publishingRoleLabels, transactionIntentLabels } from "@/lib/labels";
import type { ManagementListing } from "@/lib/managementApi";

/** Edits offer, headline, price and contact of a Draft, Published or Paused listing. */
export function EditListingForm({ listing }: { listing: ManagementListing }) {
  const [state, action, pending] = useActionState<EditListingState, FormData>(
    (previous, form) => updateListing(listing.listingId, listing.version, previous, form),
    {},
  );

  const submitted = state.values;
  const value = (name: string, fallback: string) =>
    submitted && typeof submitted[name] === "string" ? (submitted[name] as string) : fallback;
  const [priceMode, setPriceMode] = useState(
    value("priceMode", listing.price?.isOnRequest === false ? "known" : "onRequest"),
  );
  const checked = (name: string, fallback: boolean) => (submitted ? submitted[name] === "on" : fallback);
  const error = (name: string) =>
    state.errors?.[name] ? <span className="field-error">{state.errors[name]}</span> : null;

  return (
    <form action={action} className="listing-form" noValidate>
      {state.formError ? <p className="form-error" role="alert">{state.formError}</p> : null}
      <div className="form-grid">
        <label className="field">
          نوع العرض
          <select name="transactionIntent" defaultValue={value("transactionIntent", listing.context?.transactionIntent ?? "")}>
            <Options labels={transactionIntentLabels} placeholder="اختر" />
          </select>
          {error("transactionIntent")}
        </label>
        <label className="field">
          صفتك
          <select name="publishingRole" defaultValue={value("publishingRole", listing.context?.publishingRole ?? "Owner")}>
            <Options labels={publishingRoleLabels} placeholder="اختر" />
          </select>
          {error("publishingRole")}
        </label>
        <label className="field wide">
          عنوان الإعلان
          <input name="headline" maxLength={120} defaultValue={value("headline", listing.headline ?? "")} />
          {error("headline")}
        </label>
      </div>

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
            السعر
            <input name="priceAmount" inputMode="decimal" defaultValue={value("priceAmount", listing.price?.amount?.toString() ?? "")} />
            {error("priceAmount")}
          </label>
          <label className="field">
            العملة
            <select name="priceCurrency" defaultValue={value("priceCurrency", listing.price?.currency ?? "USD")}>
              <option value="USD">دولار أمريكي</option>
              <option value="SYP">ليرة سورية</option>
            </select>
            {error("priceCurrency")}
          </label>
        </div>
      ) : null}

      <label className="checkbox">
        <input type="checkbox" name="allowCalls" defaultChecked={checked("allowCalls", listing.contact?.allowCalls ?? false)} /> السماح بالاتصال الهاتفي
      </label>
      <label className="checkbox">
        <input type="checkbox" name="allowWhatsApp" defaultChecked={checked("allowWhatsApp", listing.contact?.allowWhatsApp ?? false)} /> السماح بالمراسلة عبر واتساب
      </label>

      <button className="button" type="submit" disabled={pending}>
        {pending ? "جارٍ الحفظ…" : "حفظ التعديلات"}
      </button>
    </form>
  );
}
