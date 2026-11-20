import type { Metadata } from "next";
import { LoginForm } from "@/components/LoginForm";
import { safeNextPath } from "@/lib/redirects";

export const metadata: Metadata = { title: "تسجيل الدخول", robots: { index: false } };

type Props = { searchParams: Promise<{ next?: string; expired?: string }> };

export default async function LoginPage({ searchParams }: Props) {
  const params = await searchParams;
  return (
    <div className="container section">
      {params.expired ? <p className="notice center">انتهت الجلسة. سجّل الدخول مجدداً.</p> : null}
      <LoginForm next={safeNextPath(params.next)} />
    </div>
  );
}
