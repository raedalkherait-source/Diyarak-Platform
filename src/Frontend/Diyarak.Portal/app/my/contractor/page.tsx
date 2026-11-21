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
    <form action={formAction}>
      {state.status === "error" && <p style={{ color: "crimson" }}>{state.message}</p>}
      {state.status === "saved" && <p style={{ color: "green" }}>{state.message}</p>}

      <label style={{ display: "block", marginBottom: "0.75rem" }}>
        الاسم
        <input name="name" required minLength={2} maxLength={100} defaultValue={existing?.name ?? ""} style={{ display: "block", width: "100%" }} />
      </label>

      <label style={{ display: "block", marginBottom: "0.75rem" }}>
        الوصف
        <textarea name="description" maxLength={2000} rows={3} defaultValue={existing?.description ?? ""} style={{ display: "block", width: "100%" }} />
      </label>

      <label style={{ display: "block", marginBottom: "0.75rem" }}>
        المحافظة
        <select name="governorate" required defaultValue={existing?.governorate ?? ""} style={{ display: "block", width: "100%" }}>
          <option value="">اختر</option>
          {governorates.map(([k, v]) => <option key={k} value={k}>{v}</option>)}
        </select>
      </label>

      <label style={{ display: "block", marginBottom: "0.75rem" }}>
        المنطقة
        <input name="district" maxLength={100} defaultValue={existing?.district ?? ""} style={{ display: "block", width: "100%" }} />
      </label>

      <label style={{ display: "block", marginBottom: "0.75rem" }}>
        التخصص
        <select name="specialty" required defaultValue={existing?.specialty ?? "GeneralConstruction"} style={{ display: "block", width: "100%" }}>
          {specialties.map(([k, v]) => <option key={k} value={k}>{v}</option>)}
        </select>
      </label>

      <label style={{ display: "block", marginBottom: "0.75rem" }}>
        رقم الهاتف
        <input name="phone" required maxLength={20} defaultValue={existing?.phone ?? ""} dir="ltr" style={{ display: "block", width: "100%" }} />
      </label>

      <button type="submit" className="button" disabled={pending}>
        {isNew ? "إنشاء الملف" : "حفظ التعديلات"}
      </button>
    </form>
  );
}

export default function ContractorManagementPage() {
  // This is a client component that fetches data via useEffect.
  // For simplicity, we render a form shell and fetch existing data client-side.
  // In a real scenario, we'd use a server component wrapper.
  // For the MVP, the page shows the create form; editing requires server-side data.
  return (
    <div className="container" style={{ paddingBlock: "2rem", maxWidth: "600px" }}>
      <h1 style={{ marginBottom: "1rem" }}>ملف المقاول</h1>
      <p style={{ color: "var(--muted)", marginBottom: "1.5rem" }}>
        أنشئ ملفك كمقاول أو متعهد ليظهر في دليل المقاولين.
      </p>
      <ContractorForm existing={null} />
    </div>
  );
}
