import type { MetadataRoute } from "next";
import { searchListings } from "@/lib/api";
import { governorateLabels } from "@/lib/labels";
import { siteUrl } from "@/lib/site";

export const revalidate = 3600;

/** Upper bound on listing URLs per sitemap; the public API caps offset pagination at 10,000. */
const maximumListings = 10000;

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const base = siteUrl();
  const entries: MetadataRoute.Sitemap = [
    { url: `${base}/`, changeFrequency: "daily", priority: 1 },
    { url: `${base}/search`, changeFrequency: "hourly", priority: 0.8 },
  ];

  for (const governorate of Object.keys(governorateLabels)) {
    for (const intent of ["Sell", "Rent"]) {
      entries.push({
        url: `${base}/search?transactionIntent=${intent}&governorate=${governorate}`,
        changeFrequency: "daily",
        priority: 0.6,
      });
    }
  }

  try {
    for (let page = 1; (page - 1) * 100 < maximumListings; page++) {
      const result = await searchListings(new URLSearchParams({ page: String(page), pageSize: "100" }));
      if (!result) break;
      for (const listing of result.items)
        entries.push({ url: `${base}/listings/${listing.listingId}`, changeFrequency: "weekly", priority: 0.7 });
      if (!result.hasMore) break;
    }
  } catch {
    // A sitemap without listings is better than a failing one; the next revalidation retries.
  }

  return entries;
}
