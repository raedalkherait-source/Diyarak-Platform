import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ListingCard } from "@/components/ListingCard";
import { ReviewSection } from "@/components/ReviewSection";
import { getAgency, listReviews, getSubjectRating } from "@/lib/api";
import { governorateLabels, label } from "@/lib/labels";
import { getSessionToken } from "@/lib/session";
import { jsonForScript, siteUrl } from "@/lib/site";

type Props = { params: Promise<{ id: string }>; searchParams: Promise<{ page?: string }> };

function pageNumber(value: string | undefined): number {
  const page = Number(value ?? "1");
  return Number.isInteger(page) && page >= 1 && page <= 100 ? page : 1;
}

export async function generateMetadata({ params, searchParams }: Props): Promise<Metadata> {
  const [{ id }, { page }] = await Promise.all([params, searchParams]);
  const data = await getAgency(id, pageNumber(page));
  if (!data) return { title: "مكتب غير موجود", robots: { index: false } };

  const place = [data.agency.city, label(governorateLabels, data.agency.governorate)].filter(Boolean).join("، ");
  const title = `${data.agency.name} — مكتب عقاري في ${place}`;
  const desc = data.agency.description?.slice(0, 160) ?? `عقارات ${data.agency.name} للبيع والإيجار في ${place} على ديارك.`;
  return {
    title,
    description: desc,
    alternates: { canonical: `/agencies/${data.agency.agencyId}` },
    openGraph: { title, description: desc },
    robots: data.agency.verified ? undefined : { index: false, follow: true },
  };
}

export default async function AgencyPage({ params, searchParams }: Props) {
  const [{ id }, { page }] = await Promise.all([params, searchParams]);
  const current = pageNumber(page);
  const data = await getAgency(id, current);
  if (!data) notFound();

  const { agency, items, hasMore } = data;

  const [reviewsData, ratingData, token] = await Promise.all([
    listReviews(agency.agencyId),
    getSubjectRating(agency.agencyId),
    getSessionToken(),
  ]);

  const structured = {
    "@context": "https://schema.org",
    "@type": "RealEstateAgent",
    name: agency.name,
    url: `${siteUrl()}/agencies/${agency.agencyId}`,
    address: { "@type": "PostalAddress", addressLocality: agency.city, addressRegion: label(governorateLabels, agency.governorate) || undefined, addressCountry: "SY" },
    description: agency.description ?? undefined,
  };

  return (
    <div className="container section">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: jsonForScript(structured) }} />
      <header className="panel agency-header">
        <div className="agency-avatar" aria-hidden="true">{agency.name.trim().charAt(0)}</div>
        <div>
          <h1>
            {agency.name}
            {agency.verified ? <span className="verified-badge" title="مكتب موثّق من ديارك"> ✓ موثّق</span> : null}
          </h1>
          <p className="notice">مكتب عقاري · {[agency.city, label(governorateLabels, agency.governorate)].filter(Boolean).join("، ")}</p>
          {agency.description ? <p className="agency-description">{agency.description}</p> : null}
        </div>
      </header>

      <h2 className="section-subtitle">إعلانات المكتب</h2>
      {items.length === 0 ? (
        <div className="empty">لا توجد إعلانات منشورة لهذا المكتب حالياً.</div>
      ) : (
        <>
          <div className="grid">
            {items.map((listing) => (
              <ListingCard key={listing.listingId} listing={listing} />
            ))}
          </div>
          <nav className="pagination" aria-label="الصفحات">
            {current > 1 ? <Link className="button secondary" href={`/agencies/${agency.agencyId}?page=${current - 1}`}>→ السابق</Link> : null}
            {hasMore ? <Link className="button secondary" href={`/agencies/${agency.agencyId}?page=${current + 1}`}>التالي ←</Link> : null}
          </nav>
        </>
      )}

      <ReviewSection
        subjectId={agency.agencyId}
        subjectType="Agency"
        reviews={reviewsData?.items ?? []}
        average={ratingData?.average ?? 0}
        count={ratingData?.count ?? 0}
        hasMore={reviewsData?.hasMore ?? false}
        signedIn={token !== null}
      />
    </div>
  );
}
