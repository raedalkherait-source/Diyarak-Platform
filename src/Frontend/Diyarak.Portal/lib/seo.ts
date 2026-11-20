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
