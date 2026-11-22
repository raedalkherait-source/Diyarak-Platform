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
      <h1>مشاريع على المخطط</h1>
      <p style={{ marginBottom: "1.5rem", color: "var(--muted)" }}>
        مشاريع عقارية قيد الإنشاء — احجز وحدتك مبكراً
      </p>

      <nav style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap", marginBottom: "1.5rem" }}>
        <Link
          href="/projects"
          className={`button small ${!params.governorate ? "" : "secondary"}`}
        >
          الكل
        </Link>
        {Object.entries(governorateLabels).map(([key, label]) => (
          <Link
            key={key}
            href={`/projects?governorate=${key}`}
            className={`button small ${params.governorate === key ? "" : "secondary"}`}
          >
            {label}
          </Link>
        ))}
      </nav>

      {!data || data.items.length === 0 ? (
        <p>لا توجد مشاريع حالياً.</p>
      ) : (
        <>
          <div className="card-grid">
            {data.items.map((project) => (
              <Link
                key={project.id}
                href={`/projects/${project.id}`}
                className="card"
              >
                <h3>{project.name}</h3>
                <p className="card-meta">
                  {governorateLabels[project.governorate] ?? project.governorate}
                  {" — "}
                  {project.district}
                </p>
                <p className="card-meta">
                  {constructionStatusLabels[project.constructionStatus] ?? project.constructionStatus}
                  {" · "}
                  {project.totalUnits} وحدة
                </p>
                <p className="card-price">{formatPrice(project.minPriceUsd, project.maxPriceUsd)}</p>
                <p className="card-agency">
                  {project.agency.name}
                  {project.agency.isVerified && " ✓"}
                </p>
              </Link>
            ))}
          </div>

          <div className="pagination">
            {page > 1 && (
              <Link
                className="button secondary"
                href={`/projects?page=${page - 1}${params.governorate ? `&governorate=${params.governorate}` : ""}`}
              >
                الصفحة السابقة
              </Link>
            )}
            {data.hasMore && (
              <Link
                className="button secondary"
                href={`/projects?page=${page + 1}${params.governorate ? `&governorate=${params.governorate}` : ""}`}
              >
                الصفحة التالية
              </Link>
            )}
          </div>
        </>
      )}
    </div>
  );
}
