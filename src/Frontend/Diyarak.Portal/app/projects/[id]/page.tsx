import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getOffPlanProject } from "@/lib/api";
import { governorateLabels, constructionStatusLabels } from "@/lib/labels";

type Props = { params: Promise<{ id: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  const project = await getOffPlanProject(id);
  if (!project) return { title: "مشروع غير موجود" };
  return {
    title: project.name,
    description: `${project.name} — ${constructionStatusLabels[project.constructionStatus] ?? project.constructionStatus} في ${governorateLabels[project.governorate] ?? project.governorate}`,
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

  return (
    <div className="container section">
      <Link href="/projects" style={{ display: "inline-block", marginBottom: "1rem" }}>
        &rarr; جميع المشاريع
      </Link>

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
        <div style={{ marginTop: "2rem", whiteSpace: "pre-wrap", lineHeight: 1.8 }}>
          {project.description}
        </div>
      )}
    </div>
  );
}
