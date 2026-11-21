"use client";

import { useActionState } from "react";
import { requestAgencyVerification, saveAgency, type AgencyFormState } from "@/app/my/actions";
import { Options } from "@/components/Options";
import type { OwnerAgency } from "@/lib/agency";
import { governorateLabels } from "@/lib/labels";

export function AgencyProfileForm({ agency }: { agency: OwnerAgency | null }) {
  const [state, action, pending] = useActionState<AgencyFormState, FormData>(
    (previous, form) => saveAgency(agency?.version ?? null, previous, form),
    {},
  );
  const value = (name: keyof OwnerAgency & string) =>
    state.values?.[name] ?? (agency ? String(agency[name] ?? "") : "");

  return (
    <form action={action} className="listing-form" noValidate>
      {state.status === "error" ? <p className="form-error" role="alert">{state.message}</p> : null}
      <div className="form-grid">
        <label className="field wide">
          اسم المكتب *
          <input name="name" defaultValue={value("name")} maxLength={80} required />
        </label>
        <label className="field">
          المحافظة
          <select name="governorate" defaultValue={value("governorate")}>
            <Options labels={governorateLabels} placeholder="اختر" />
          </select>
        </label>
        <label className="field">
          المدينة *
          <input name="city" defaultValue={value("city")} maxLength={100} required />
        </label>
        <label className="field wide">
          نبذة عن المكتب
          <textarea name="description" defaultValue={value("description")} maxLength={1000} rows={4} />
        </label>
      </div>
      {agency?.verificationStatus === "Verified" ? (
        <p className="notice">تغيير اسم المكتب يلغي التوثيق ويتطلب طلباً جديداً.</p>
      ) : null}
      <button className="button" type="submit" disabled={pending}>
        {pending ? "جارٍ الحفظ…" : agency ? "حفظ التعديلات" : "إنشاء صفحة المكتب"}
      </button>
    </form>
  );
}

export function VerificationRequestForm({ agency }: { agency: OwnerAgency }) {
  const [state, action, pending] = useActionState<AgencyFormState, FormData>(
    (previous, form) => requestAgencyVerification(agency.version, previous, form),
    {},
  );

  return (
    <form action={action} className="listing-form" noValidate>
      {state.status === "error" ? <p className="form-error" role="alert">{state.message}</p> : null}
      <label className="field">
        رقم السجل التجاري أو ترخيص المكتب *
        <input name="registrationNumber" maxLength={60} defaultValue={agency.registrationNumber ?? ""} required />
      </label>
      <p className="notice">يراجع فريق ديارك الطلب وقد يتواصل معك لطلب صورة عن الترخيص. لا يظهر الرقم للزوار.</p>
      <button className="button" type="submit" disabled={pending}>{pending ? "جارٍ الإرسال…" : "طلب التوثيق"}</button>
    </form>
  );
}
