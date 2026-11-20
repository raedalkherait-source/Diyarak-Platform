"use server";

import { redirect } from "next/navigation";
import { errorMessage } from "@/lib/errors";
import { publicPost } from "@/lib/managementApi";
import { normalizePhone } from "@/lib/phone";
import { safeNextPath } from "@/lib/redirects";
import { setSessionToken } from "@/lib/session";

export interface LoginState {
  step: "phone" | "code";
  phone: string;
  error?: string;
}

export async function requestCode(_state: LoginState, form: FormData): Promise<LoginState> {
  const raw = String(form.get("phone") ?? "");
  const phone = normalizePhone(raw);
  if (!phone) return { step: "phone", phone: raw, error: "أدخل رقم جوال صحيح، مثال: 0912345678" };

  const result = await publicPost("/api/auth/phone/request", { phoneNumber: phone });
  if (!result.ok) return { step: "phone", phone: raw, error: errorMessage(result.code, result.status) };

  return { step: "code", phone };
}

export async function verifyCode(state: LoginState, form: FormData): Promise<LoginState> {
  const code = String(form.get("code") ?? "").replace(/\s/g, "");
  const result = await publicPost<{ accessToken: string }>("/api/auth/phone/verify", {
    phoneNumber: state.phone,
    code,
  });

  if (!result.ok) return { ...state, error: errorMessage(result.code, result.status) };

  await setSessionToken(result.value.accessToken);
  redirect(safeNextPath(String(form.get("next") ?? "")));
}
