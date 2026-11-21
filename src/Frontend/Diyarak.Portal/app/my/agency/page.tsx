import type { Metadata } from "next";
import Link from "next/link";
import { AgencyProfileForm, VerificationRequestForm } from "@/components/AgencyForms";
import { verificationStatusLabels, type OwnerAgency } from "@/lib/agency";
import { managementFetch } from "@/lib/managementApi";

export const metadata: Metadata = { title: "مكتبي العقاري", robots: { index: false } };

type Props = { searchParams: Promise<{ notice?: string }> };

const notices: Record<string, string> = {
  saved: "تم حفظ بيانات المكتب.",
  requested: "تم إرسال طلب التوثيق. سنراجعه قريباً.",
};

export default async function MyAgencyPage({ searchParams }: Props) {
  const { notice } = await searchParams;
  const result = await managementFetch<OwnerAgency>("/api/market/agency");
  const agency = result.ok ? result.value : null;

  return (
    <div className="container section narrow">
      <Link href="/my" className="notice">→ إعلاناتي</Link>
      <h1>مكتبي العقاري</h1>
      {notice && notices[notice] ? <p className="success" role="status">{notices[notice]}</p> : null}
      {!agency ? (
        <p className="notice">إن كنت مكتباً عقارياً، أنشئ صفحة لمكتبك تظهر عليها كل إعلاناتك، ويظهر اسم المكتب على كل إعلان. يمكنك بعدها طلب التوثيق.</p>
      ) : (
        <p>
          <span className={`verification verification-${agency.verificationStatus.toLowerCase()}`}>
            {agency.verificationStatus === "Verified" ? "✓ " : ""}{verificationStatusLabels[agency.verificationStatus]}
          </span>{" "}
          <Link href={`/agencies/${agency.agencyId}`}>عرض صفحة المكتب العامة</Link>
        </p>
      )}

      <section className="panel">
        <h2>بيانات المكتب</h2>
        <AgencyProfileForm agency={agency} />
      </section>

      {agency && (agency.verificationStatus === "None" || agency.verificationStatus === "Rejected") ? (
        <section className="panel">
          <h2>التوثيق</h2>
          {agency.verificationStatus === "Rejected" && agency.verificationNote ? (
            <p className="form-error">سبب الرفض: {agency.verificationNote}</p>
          ) : null}
          <p className="notice">المكاتب الموثّقة تظهر بعلامة ✓ على صفحتها وعلى كل إعلاناتها، ما يزيد ثقة الزوار.</p>
          <VerificationRequestForm agency={agency} />
        </section>
      ) : null}
    </div>
  );
}
