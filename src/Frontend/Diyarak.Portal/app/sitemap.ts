import type { MetadataRoute } from "next";
import { searchListings, listOffPlanProjects, listContractors } from "@/lib/api";
import { governorateLabels, contractorSpecialtyLabels } from "@/lib/labels";
import { siteUrl } from "@/lib/site";

export const revalidate = 3600;

/** Upper bound on listing URLs per sitemap; the public API caps offset pagination at 10,000. */
const maximumListings = 10000;

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const base = siteUrl();
  const governorates = Object.keys(governorateLabels);
  const entries: MetadataRoute.Sitemap = [
    { url: `${base}/`, changeFrequency: "daily", priority: 1 },
    { url: `${base}/search`, changeFrequency: "hourly", priority: 0.8 },
    { url: `${base}/contractors`, changeFrequency: "daily", priority: 0.7 },
    { url: `${base}/projects`, changeFrequency: "daily", priority: 0.7 },
    { url: `${base}/prices`, changeFrequency: "weekly", priority: 0.6 },
    { url: `${base}/faq`, changeFrequency: "monthly", priority: 0.4 },
    { url: `${base}/contact`, changeFrequency: "monthly", priority: 0.4 },
  ];

  for (const governorate of governorates) {
    for (const intent of ["Sell", "Rent"]) {
      entries.push({
        url: `${base}/search?transactionIntent=${intent}&governorate=${governorate}`,
        changeFrequency: "daily",
        priority: 0.6,
      });
    }
    entries.push({
      url: `${base}/prices?governorate=${governorate}`,
      changeFrequency: "weekly",
      priority: 0.5,
    });
  }

  for (const specialty of Object.keys(contractorSpecialtyLabels)) {
    entries.push({
      url: `${base}/contractors?specialty=${specialty}`,
      changeFrequency: "daily",
      priority: 0.5,
    });
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

  try {
    for (let page = 1; page <= 50; page++) {
      const result = await listOffPlanProjects(page);
      if (!result) break;
      for (const project of result.items)
        entries.push({ url: `${base}/projects/${project.id}`, changeFrequency: "weekly", priority: 0.6 });
      if (!result.hasMore) break;
    }
  } catch {
    // Best-effort; the next revalidation retries.
  }

  return entries;
}
