"use client";

import { useActionState } from "react";
import { governorateLabels, contractorSpecialtyLabels } from "@/lib/labels";
import {
  createContractor,
  updateContractor,
  toggleContractorPublish,
  deleteContractor,
  type ContractorFormState,
} from "../actions";

const governorates = Object.entries(governorateLabels);
const specialties = Object.entries(contractorSpecialtyLabels);

interface ContractorData {
  id: string;
  name: string;
  description: string | null;
  governorate: string;
  district: string | null;
  specialty: string;
  phone: string;
  isPublished: boolean;
  version: number;
}

function ContractorForm({ existing }: { existing: ContractorData | null }) {
  const isNew = !existing;
  const action = isNew
    ? createContractor
    : updateContractor.bind(null, existing.id, existing.version);

  const [state, formAction, pending] = useActionState<ContractorFormState, FormData>(action, {});

  return (
    <form action={formAction} className="form-stack">
      {state.status === "error" && <p className="form-error" role="alert">{state.message}</p>}
      {state.status === "saved" && <p className="form-success" role="status">{state.message}</p>}

      <label className="field">
        الاسم
        <input name="name" autoComplete="name" required minLength={2} maxLength={100} defaultValue={existing?.name ?? ""} />
      </label>

      <label className="field">
        الوصف
        <textarea name="description" maxLength={2000} rows={3} defaultValue={existing?.description ?? ""} />
      </label>

      <label className="field">
        المحافظة
        <select name="governorate" required defaultValue={existing?.governorate ?? ""}>
          <option value="">اختر</option>
          {governorates.map(([k, v]) => <option key={k} value={k}>{v}</option>)}
        </select>
      </label>

      <label className="field">
        المنطقة
        <input name="district" maxLength={100} defaultValue={existing?.district ?? ""} />
      </label>

      <label className="field">
        التخصص
        <select name="specialty" required defaultValue={existing?.specialty ?? "GeneralConstruction"}>
          {specialties.map(([k, v]) => <option key={k} value={k}>{v}</option>)}
        </select>
      </label>

      <label className="field">
        رقم الهاتف
        <input name="phone" type="tel" inputMode="tel" autoComplete="tel" enterKeyHint="done" spellCheck={false} required maxLength={20} defaultValue={existing?.phone ?? ""} dir="ltr" />
      </label>

      <button type="submit" className="button" disabled={pending}>
        {isNew ? "إنشاء الملف" : "حفظ التعديلات"}
      </button>
    </form>
  );
}

export default function ContractorManagementPage() {
  return (
    <div className="container section narrow-form">
      <h1>ملف المقاول</h1>
      <p className="notice">
        أنشئ ملفك كمقاول أو متعهد ليظهر في دليل المقاولين.
      </p>
      <ContractorForm existing={null} />
    </div>
  );
}
