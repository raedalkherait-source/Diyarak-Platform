import type { Metadata } from "next";
import Link from "next/link";
import { listOffPlanProjects } from "@/lib/api";
import { governorateLabels, constructionStatusLabels } from "@/lib/labels";
import { breadcrumbStructuredData } from "@/lib/seo";
import { jsonForScript } from "@/lib/site";

export const metadata: Metadata = {
  title: "مشاريع على المخطط",
  description: "تصفح المشاريع العقارية قيد الإنشاء في سوريا — احجز وحدتك قبل الانتهاء.",
  openGraph: {
    title: "مشاريع على المخطط — ديارك",
    description: "تصفح المشاريع العقارية قيد الإنشاء في سوريا.",
  },
};

function formatPrice(min: number | null, max: number | null): string {
  const fmt = new Intl.NumberFormat("ar-SY-u-nu-latn");
  if (min !== null && max !== null) return `$${fmt.format(min)} – $${fmt.format(max)}`;
  if (min !== null) return `من $${fmt.format(min)}`;
  if (max !== null) return `حتى $${fmt.format(max)}`;
  return "السعر عند الطلب";
}

export default async function ProjectsPage({
  searchParams,
}: {
  searchParams: Promise<{ page?: string; governorate?: string }>;
}) {
  const params = await searchParams;
  const page = Math.max(1, parseInt(params.page ?? "1", 10) || 1);
  const data = await listOffPlanProjects(page, params.governorate);

  return (
    <div className="container section">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: jsonForScript(breadcrumbStructuredData([
        { name: "الرئيسية", href: "/" },
        { name: "مشاريع على المخطط" },
      ])) }} />
      <nav className="breadcrumbs" aria-label="مسار التنقل">
        <Link href="/">الرئيسية</Link>
        <span>›</span>
        <span>مشاريع على المخطط</span>
      </nav>

      <h1>مشاريع على المخطط</h1>
      <p className="notice">
        مشاريع عقارية قيد الإنشاء — احجز وحدتك مبكراً
      </p>

      <div className="filter-chips">
        <Link href="/projects" className={`chip ${!params.governorate ? "active" : ""}`}>الكل</Link>
        {Object.entries(governorateLabels).map(([key, lbl]) => (
          <Link key={key} href={`/projects?governorate=${key}`} className={`chip ${params.governorate === key ? "active" : ""}`}>
            {lbl}
          </Link>
        ))}
      </div>

      {!data || data.items.length === 0 ? (
        <div className="empty">لا توجد مشاريع حالياً.</div>
      ) : (
        <>
          <div className="project-grid">
            {data.items.map((project) => (
              <Link key={project.id} href={`/projects/${project.id}`} className="panel project-card">
                <div className="project-header">
                  <h3 className="project-name">{project.name}</h3>
                  <span className={`status-badge status-${project.constructionStatus.toLowerCase()}`}>
                    {constructionStatusLabels[project.constructionStatus] ?? project.constructionStatus}
                  </span>
                </div>
                <p className="project-location">
                  {governorateLabels[project.governorate] ?? project.governorate} — {project.district}
                </p>
                <p className="project-units">{project.totalUnits} وحدة</p>
                <p className="project-price">{formatPrice(project.minPriceUsd, project.maxPriceUsd)}</p>
                <p className="project-agency">
                  {project.agency.name}
                  {project.agency.isVerified && <span className="verified-badge"> ✓</span>}
                </p>
                {project.expectedCompletion ? (
                  <p className="project-completion">التسليم المتوقع: {project.expectedCompletion}</p>
                ) : null}
              </Link>
            ))}
          </div>

          <nav className="pagination" aria-label="الصفحات">
            {page > 1 && (
              <Link className="button secondary" href={`/projects?page=${page - 1}${params.governorate ? `&governorate=${params.governorate}` : ""}`}>
                → السابق
              </Link>
            )}
            {data.hasMore && (
              <Link className="button secondary" href={`/projects?page=${page + 1}${params.governorate ? `&governorate=${params.governorate}` : ""}`}>
                التالي ←
              </Link>
            )}
          </nav>
        </>
      )}
    </div>
  );
}
