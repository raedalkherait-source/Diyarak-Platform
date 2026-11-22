import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getOffPlanProject } from "@/lib/api";
import { governorateLabels, constructionStatusLabels } from "@/lib/labels";
import { jsonForScript, siteUrl } from "@/lib/site";

type Props = { params: Promise<{ id: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  const project = await getOffPlanProject(id);
  if (!project) return { title: "مشروع غير موجود" };
  const desc = `${project.name} — ${constructionStatusLabels[project.constructionStatus] ?? project.constructionStatus} في ${governorateLabels[project.governorate] ?? project.governorate}`;
  return {
    title: project.name,
    description: desc,
    openGraph: { title: project.name, description: desc },
  };
}

function formatPrice(min: number | null, max: number | null): string {
  const fmt = new Intl.NumberFormat("ar-SY-u-nu-latn");
  if (min !== null && max !== null) return `$${fmt.format(min)} – $${fmt.format(max)}`;
  if (min !== null) return `من $${fmt.format(min)}`;
  if (max !== null) return `حتى $${fmt.format(max)}`;
  return "السعر عند الطلب";
}

export default async function ProjectDetailPage({ params }: Props) {
  const { id } = await params;
  const project = await getOffPlanProject(id);
  if (!project) notFound();

  const structured = {
    "@context": "https://schema.org",
    "@type": "RealEstateProject" as string,
    name: project.name,
    url: `${siteUrl()}/projects/${project.id}`,
    description: project.description ?? undefined,
    address: {
      "@type": "PostalAddress",
      addressLocality: project.district,
      addressRegion: governorateLabels[project.governorate] ?? project.governorate,
      addressCountry: "SY",
    },
    numberOfUnits: project.totalUnits,
  };

  return (
    <div className="container section">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: jsonForScript(structured) }} />
      <nav className="breadcrumbs" aria-label="مسار التنقل">
        <Link href="/">الرئيسية</Link>
        <span>›</span>
        <Link href="/projects">مشاريع على المخطط</Link>
        <span>›</span>
        <span>{project.name}</span>
      </nav>

      <h1>{project.name}</h1>

      <dl className="detail-grid">
        <dt>الموقع</dt>
        <dd>
          {governorateLabels[project.governorate] ?? project.governorate}
          {" — "}
          {project.district}
        </dd>

        <dt>حالة البناء</dt>
        <dd>{constructionStatusLabels[project.constructionStatus] ?? project.constructionStatus}</dd>

        <dt>عدد الوحدات</dt>
        <dd>{project.totalUnits}</dd>

        <dt>نطاق الأسعار</dt>
        <dd>{formatPrice(project.minPriceUsd, project.maxPriceUsd)}</dd>

        {project.expectedCompletion && (
          <>
            <dt>التسليم المتوقع</dt>
            <dd>{project.expectedCompletion}</dd>
          </>
        )}

        <dt>المطور</dt>
        <dd>
          <Link href={`/agencies/${project.agency.id}`}>
            {project.agency.name}
            {project.agency.isVerified && " ✓"}
          </Link>
        </dd>
      </dl>

      {project.description && (
        <div className="project-description">
          {project.description}
        </div>
      )}
    </div>
  );
}
