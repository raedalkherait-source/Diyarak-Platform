"use client";

import { useActionState } from "react";
import { updateProperty, type EditPropertyState } from "@/app/my/actions";
import { PropertyFields } from "@/components/PropertyFields";
import type { FormValues } from "@/lib/listingForm";
import type { MapConfig } from "@/lib/map";

interface Props {
  listingId: string;
  propertyId: string;
  version: number;
  category: string;
  initialValues: FormValues;
  mapConfig: MapConfig;
}

/** Edits location and property details; fields not shown here are preserved by the server action. */
export function EditPropertyForm({ listingId, propertyId, version, category, initialValues, mapConfig }: Props) {
  const [state, action, pending] = useActionState<EditPropertyState, FormData>(
    (previous, form) => updateProperty(listingId, propertyId, version, previous, form),
    {},
  );

  const values = state.values ?? initialValues;
  const value = (name: string) => (typeof values[name] === "string" ? (values[name] as string) : "");
  const selectedFeatures = Array.isArray(values.features) ? values.features : [];
  const error = (name: string) =>
    state.errors?.[name] ? <span className="field-error">{state.errors[name]}</span> : null;

  return (
    <form action={action} className="listing-form" noValidate>
      {state.formError ? <p className="form-error" role="alert">{state.formError}</p> : null}
      <PropertyFields value={value} error={error} selectedFeatures={selectedFeatures} category={category} mapConfig={mapConfig} />
      <button className="button" type="submit" disabled={pending}>
        {pending ? "جارٍ الحفظ…" : "حفظ تفاصيل العقار"}
      </button>
    </form>
  );
}
