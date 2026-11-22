import type { Metadata } from "next";
import Link from "next/link";
import { breadcrumbStructuredData } from "@/lib/seo";
import { jsonForScript } from "@/lib/site";

export const metadata: Metadata = {
  title: "أدوات عقارية",
  description: "حاسبة الأقساط الشهرية وتقدير قيمة العقار — أدوات مجانية من ديارك.",
};

export default function ToolsPage() {
  return (
    <div className="container section">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: jsonForScript(breadcrumbStructuredData([
        { name: "الرئيسية", href: "/" },
        { name: "أدوات عقارية" },
      ])) }} />
      <nav className="breadcrumbs" aria-label="مسار التنقل">
        <Link href="/">الرئيسية</Link>
        <span>›</span>
        <span>أدوات عقارية</span>
      </nav>
      <h1>أدوات عقارية</h1>
      <p className="notice">أدوات مجانية تساعدك في اتخاذ قرارات عقارية أفضل.</p>
      <div className="tools-grid">
        <Link href="/tools/mortgage" className="panel tool-card">
          <strong>حاسبة الأقساط</strong>
          <span>احسب القسط الشهري لقرض عقاري بناءً على المبلغ ومدة السداد ونسبة الفائدة.</span>
        </Link>
        <Link href="/tools/estimator" className="panel tool-card">
          <strong>تقدير قيمة العقار</strong>
          <span>تقدير تقريبي لقيمة عقارك بناءً على المحافظة ونوع العقار والمساحة.</span>
        </Link>
      </div>
    </div>
  );
}
