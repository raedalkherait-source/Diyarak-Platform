import type { Metadata } from "next";
import Link from "next/link";
import { listContractors, getSubjectRating } from "@/lib/api";
import { governorateLabels, contractorSpecialtyLabels, label } from "@/lib/labels";
import { breadcrumbStructuredData } from "@/lib/seo";
import { jsonForScript } from "@/lib/site";

export const metadata: Metadata = {
  title: "دليل المقاولين في سوريا",
  description: "ابحث عن مقاولين ومتعهدين في جميع المحافظات السورية — بناء، كهرباء، سباكة، تصميم داخلي وأكثر.",
  alternates: { canonical: "/contractors" },
  openGraph: {
    title: "دليل المقاولين في سوريا — ديارك",
    description: "ابحث عن مقاولين ومتعهدين في جميع المحافظات السورية.",
  },
};

const specialties = Object.keys(contractorSpecialtyLabels);
const governorates = Object.keys(governorateLabels);

export default async function ContractorsPage({
  searchParams,
}: {
  searchParams: Promise<{ page?: string; governorate?: string; specialty?: string }>;
}) {
  const { page: pageStr, governorate, specialty } = await searchParams;
  const page = Math.max(1, parseInt(pageStr ?? "1", 10) || 1);
  const data = await listContractors(page, governorate, specialty);
  const items = data?.items ?? [];
  const hasMore = data?.hasMore ?? false;

  const ratings = await Promise.all(
    items.map((c) => getSubjectRating(c.id).then((r) => [c.id, r] as const)),
  );
  const ratingMap = Object.fromEntries(ratings);

  return (
    <div className="container section">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: jsonForScript(breadcrumbStructuredData([
        { name: "الرئيسية", href: "/" },
        { name: "دليل المقاولين" },
      ])) }} />
      <nav className="breadcrumbs" aria-label="مسار التنقل">
        <Link href="/">الرئيسية</Link>
        <span>›</span>
        <span>دليل المقاولين</span>
      </nav>

      <h1>دليل المقاولين</h1>
      <p className="notice">
        تصفح مقاولين ومتعهدين في جميع المحافظات السورية.
      </p>

      <div className="filter-chips">
        <Link href="/contractors" className={`chip ${!governorate && !specialty ? "active" : ""}`}>الكل</Link>
        {governorates.map((g) => (
          <Link key={g} href={`/contractors?governorate=${g}${specialty ? `&specialty=${specialty}` : ""}`} className={`chip ${governorate === g ? "active" : ""}`}>
            {label(governorateLabels, g)}
          </Link>
        ))}
      </div>

      <div className="filter-chips">
        {specialties.map((s) => (
          <Link key={s} href={`/contractors?specialty=${s}${governorate ? `&governorate=${governorate}` : ""}`} className={`chip ${specialty === s ? "active" : ""}`}>
            {label(contractorSpecialtyLabels, s)}
          </Link>
        ))}
      </div>

      {items.length === 0 ? (
        <div className="empty">لا يوجد مقاولون مسجلون حالياً بهذا التصنيف.</div>
      ) : (
        <div className="contractor-grid">
          {items.map((c) => (
            <div key={c.id} className="panel contractor-card">
              <h2 className="contractor-name">{c.name}</h2>
              {ratingMap[c.id] != null && ratingMap[c.id]!.count > 0 && (
                <p className="agency-rating">
                  <span className="stars">
                    {Array.from({ length: 5 }, (_, i) => <span key={i} className={i < Math.round(ratingMap[c.id]!.average) ? "star filled" : "star"}>★</span>)}
                  </span>
                  <span className="rating-text">{ratingMap[c.id]!.average.toFixed(1)} ({ratingMap[c.id]!.count})</span>
                </p>
              )}
              <p className="contractor-meta">
                {label(contractorSpecialtyLabels, c.specialty)} · {label(governorateLabels, c.governorate)}
                {c.district ? ` — ${c.district}` : ""}
              </p>
              {c.description && <p className="contractor-desc">{c.description}</p>}
              <p className="contractor-phone">
                <a href={`tel:${c.phone}`} dir="ltr" translate="no">{c.phone}</a>
              </p>
            </div>
          ))}
        </div>
      )}

      {(page > 1 || hasMore) && (
        <nav className="pagination" aria-label="الصفحات">
          {page > 1 && (
            <Link href={`/contractors?page=${page - 1}${governorate ? `&governorate=${governorate}` : ""}${specialty ? `&specialty=${specialty}` : ""}`} className="button secondary">
              → السابق
            </Link>
          )}
          {hasMore && (
            <Link href={`/contractors?page=${page + 1}${governorate ? `&governorate=${governorate}` : ""}${specialty ? `&specialty=${specialty}` : ""}`} className="button secondary">
              التالي ←
            </Link>
          )}
        </nav>
      )}
    </div>
  );
}
