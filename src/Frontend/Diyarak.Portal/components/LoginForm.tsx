"use client";

import { useActionState } from "react";
import { requestCode, verifyCode, type LoginState } from "@/app/login/actions";

const initial: LoginState = { step: "phone", phone: "" };

export function LoginForm({ next }: { next: string }) {
  const [state, setState] = useActionState(
    async (previous: LoginState, form: FormData) =>
      form.get("intent") === "verify" ? verifyCode(previous, form) : requestCode(previous, form),
    initial,
  );

  return (
    <form action={setState} className="panel auth-card">
      <h1>تسجيل الدخول</h1>
      <input type="hidden" name="next" value={next} />
      {state.step === "phone" ? (
        <>
          <p className="notice">أدخل رقم جوالك وسنرسل لك رمز تحقق. لا حاجة لكلمة مرور.</p>
          <label className="field">
            رقم الجوال
            <input
              name="phone"
              type="tel"
              inputMode="tel"
              autoComplete="tel"
              dir="ltr"
              placeholder="09xx xxx xxx"
              defaultValue={state.phone}
              required
            />
          </label>
          <button className="button" type="submit" name="intent" value="request">إرسال الرمز</button>
        </>
      ) : (
        <>
          <p className="notice">
            أرسلنا رمزاً من 6 أرقام إلى <span dir="ltr">{state.phone}</span>. الرمز صالح لمدة 5 دقائق.
          </p>
          <label className="field">
            رمز التحقق
            <input
              name="code"
              inputMode="numeric"
              autoComplete="one-time-code"
              pattern="[0-9٠-٩]{6}"
              maxLength={6}
              dir="ltr"
              required
              autoFocus
            />
          </label>
          <button className="button" type="submit" name="intent" value="verify">دخول</button>
          <button className="button secondary" type="submit" name="intent" value="request" formNoValidate>
            إعادة إرسال الرمز
          </button>
          <input type="hidden" name="phone" value={state.phone} />
        </>
      )}
      {state.error ? <p className="form-error" role="alert">{state.error}</p> : null}
    </form>
  );
}
