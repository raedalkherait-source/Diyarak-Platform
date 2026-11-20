import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ContactButton } from "@/components/ContactButton";
import { Gallery } from "@/components/Gallery";
import { ReportListing } from "@/components/ReportListing";
import { getListing } from "@/lib/api";
import { listingStructuredData } from "@/lib/seo";
import { jsonForScript } from "@/lib/site";
import type { PublicListing } from "@/lib/types";
import { formatArea, formatFloor, formatLocation, formatNumber, formatPrice, photoSrc } from "@/lib/format";
import {
  categoryLabels,
  commercialSubtypeLabels,
  featureLabels,
  finishingLevelLabels,
  governorateLabels,
  label,
  legalTitleLabels,
  publishingRoleLabels,
  transactionIntentLabels,
} from "@/lib/labels";

type Props = { params: Promise<{ id: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const listing = await getListing((await params).id);
  if (!listing) return { title: "الإعلان غير موجود" };

  const cover = listing.photos[0];
  const description = `${formatPrice(listing.price)} — ${formatLocation(listing.property.location)}`;

  return {
    title: listing.headline,
    description,
    alternates: { canonical: `/listings/${listing.listingId}` },
    openGraph: {
      title: listing.headline,
      description,
      images: cover ? [photoSrc(listing.listingId, cover.photoId)] : [],
    },
  };
}

export default async function ListingPage({ params }: Props) {
  const listing = await getListing((await params).id);
  if (!listing) notFound();

  const property = listing.property;
  const governorate = label(governorateLabels, property.location.governorate);

  const facts: Array<[string, string]> = [
    ["نوع العقار", label(categoryLabels, property.category)],
    ["النوع التجاري", label(commercialSubtypeLabels, property.commercialSubtype)],
    ["المساحة", formatArea(property.livingArea)],
    ["عدد الغرف", property.totalRooms !== null ? formatNumber(property.totalRooms) : ""],
    ["غرف النوم", property.bedroomCount !== null ? formatNumber(property.bedroomCount) : ""],
    ["الحمامات", property.bathroomCount !== null ? formatNumber(property.bathroomCount) : ""],
    ["الطابق", formatFloor(property.floorNumber, property.buildingFloorCount)],
    ["الوضع القانوني", label(legalTitleLabels, property.legalTitleType)],
    ["الإكساء", label(finishingLevelLabels, property.finishingLevel)],
    ["سنة البناء", property.constructionYear !== null ? String(property.constructionYear) : ""],
    ["مواقف السيارات", property.parkingSpaceCount !== null ? formatNumber(property.parkingSpaceCount) : ""],
    ["متاح من", listing.availableFromDate ?? ""],
  ].filter((fact): fact is [string, string] => Boolean(fact[1]));

  return (
    <div className="container">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: jsonForScript(listingStructuredData(listing)) }} />
      <nav className="breadcrumbs" aria-label="مسار التنقل">
        <Link href="/">الرئيسية</Link>
        <span>›</span>
        {property.location.governorate ? (
          <>
            <Link href={`/search?governorate=${encodeURIComponent(property.location.governorate)}`}>{governorate}</Link>
            <span>›</span>
          </>
        ) : null}
        <span>{listing.headline}</span>
      </nav>

      <div className="detail">
        <div>
          <Gallery listingId={listing.listingId} photos={listing.photos} title={listing.headline} />
          <h1>{listing.headline}</h1>
          <div className="location">
            {[formatLocation(property.location), governorate !== property.location.city ? governorate : ""]
              .filter(Boolean)
              .join(" — ")}
          </div>

          <div className="mobile-only" style={{ marginBlockStart: 16 }}>
            <ContactPanel listing={listing} />
          </div>

          <section className="panel" style={{ marginBlockStart: 20 }}>
            <h2>تفاصيل العقار</h2>
            <dl className="facts-table">
              {facts.map(([name, value]) => (
                <div key={name}>
                  <dt>{name}</dt>
                  <dd>{value}</dd>
                </div>
              ))}
            </dl>
          </section>

          <ReportListing listingId={listing.listingId} />

          {property.features.length > 0 ? (
            <section className="panel">
              <h2>المزايا</h2>
              <ul className="chips">
                {property.features.map((feature) => (
                  <li key={feature}>{label(featureLabels, feature)}</li>
                ))}
              </ul>
            </section>
          ) : null}
        </div>

        <aside className="desktop-only">
          <ContactPanel listing={listing} />
        </aside>
      </div>
    </div>
  );
}

function ContactPanel({ listing }: { listing: PublicListing }) {
  return (
    <div className="panel">
      <div className="notice">{label(transactionIntentLabels, listing.context.transactionIntent)}</div>
      <div className="price" style={{ fontSize: "1.7rem" }}>{formatPrice(listing.price)}</div>
      <p className="notice">المعلن: {label(publishingRoleLabels, listing.context.publishingRole)}</p>
      {listing.contact ? (
        <ContactButton listingId={listing.listingId} headline={listing.headline} />
      ) : (
        <p className="notice">لم يحدد المعلن طريقة تواصل بعد.</p>
      )}
    </div>
  );
}
