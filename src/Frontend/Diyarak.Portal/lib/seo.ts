import { siteUrl } from "./site";
import type { PublicListing } from "./types";
import { photoSrc } from "./format";
import { governorateLabels, label } from "./labels";

/** Search pages with only these filters are useful landing pages; others are not indexed. */
const indexableSearchKeys = new Set(["transactionIntent", "propertyCategory", "governorate"]);

export function isIndexableSearch(params: Record<string, string | string[] | undefined>): boolean {
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === "") continue;
    if (key === "page") {
      if (value !== "1") return false;
      continue;
    }
    if (!indexableSearchKeys.has(key) || Array.isArray(value)) return false;
  }
  return true;
}

/** schema.org BreadcrumbList for structured navigation. */
export function breadcrumbStructuredData(items: Array<{ name: string; href?: string }>): Record<string, unknown> {
  return {
    "@context": "https://schema.org",
    "@type": "BreadcrumbList",
    itemListElement: items.map((item, i) => ({
      "@type": "ListItem",
      position: i + 1,
      name: item.name,
      ...(item.href ? { item: `${siteUrl()}${item.href}` } : {}),
    })),
  };
}

/** schema.org WebSite with SearchAction for sitelinks search box. */
export function websiteStructuredData(): Record<string, unknown> {
  const base = siteUrl();
  return {
    "@context": "https://schema.org",
    "@type": "WebSite",
    name: "ديارك",
    url: base,
    potentialAction: {
      "@type": "SearchAction",
      target: { "@type": "EntryPoint", urlTemplate: `${base}/search?district={search_term_string}` },
      "query-input": "required name=search_term_string",
    },
  };
}

/** schema.org Organization for the site. */
export function organizationStructuredData(): Record<string, unknown> {
  const base = siteUrl();
  return {
    "@context": "https://schema.org",
    "@type": "Organization",
    name: "ديارك",
    url: base,
    logo: `${base}/icon-192.png`,
    description: "منصة العقارات في سوريا — شقق وبيوت ومحلات للبيع والإيجار.",
    areaServed: { "@type": "Country", name: "سوريا" },
  };
}

/** schema.org FAQPage for the FAQ page — enables Google rich results. */
export function faqStructuredData(items: Array<{ question: string; answer: string }>): Record<string, unknown> {
  return {
    "@context": "https://schema.org",
    "@type": "FAQPage",
    mainEntity: items.map((item) => ({
      "@type": "Question",
      name: item.question,
      acceptedAnswer: { "@type": "Answer", text: item.answer },
    })),
  };
}

/** schema.org RealEstateListing for a public listing page. */
export function listingStructuredData(listing: PublicListing): Record<string, unknown> {
  const url = `${siteUrl()}/listings/${listing.listingId}`;
  const location = listing.property.location;

  const data: Record<string, unknown> = {
    "@context": "https://schema.org",
    "@type": "RealEstateListing",
    name: listing.headline,
    url,
    image: listing.photos.map((photo) => `${siteUrl()}${photoSrc(listing.listingId, photo.photoId)}`),
    contentLocation: {
      "@type": "Place",
      address: {
        "@type": "PostalAddress",
        addressCountry: "SY",
        addressLocality: location.city,
        ...(location.governorate ? { addressRegion: label(governorateLabels, location.governorate) } : {}),
        ...(location.district ? { streetAddress: location.district } : {}),
      },
    },
  };

  if (!listing.price.isOnRequest && listing.price.amount !== null && listing.price.currency) {
    data.offers = {
      "@type": "Offer",
      price: listing.price.amount,
      priceCurrency: listing.price.currency,
      availability: "https://schema.org/InStock",
      url,
    };
  }

  return data;
}
