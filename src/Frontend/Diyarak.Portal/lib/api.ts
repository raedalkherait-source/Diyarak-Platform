import "server-only";
import { headers } from "next/headers";
import { nearestForwardedFor } from "./contact";
import type { PublicListing, PublicListingPage } from "./types";

const apiUrl = (process.env.DIYARAK_API_URL ?? "http://localhost:5080").replace(/\/$/, "");

/** Cache tag of every public listing response; invalidated when listings change or a saved search opens. */
export const publicListingsTag = "public-listings";

export class ApiUnavailableError extends Error {}

async function getJson<T>(path: string): Promise<T | null> {
  let response: Response;

  // Forward the visitor address so the API's per-client rate limit does not treat every
  // server-rendered page as coming from the portal itself (requires ReverseProxy:KnownProxies).
  const requestHeaders: Record<string, string> = { Accept: "application/json" };
  const client = nearestForwardedFor((await headers()).get("x-forwarded-for"));
  if (client) requestHeaders["X-Forwarded-For"] = client;

  try {
    response = await fetch(`${apiUrl}${path}`, {
      headers: requestHeaders,
      next: { revalidate: 30, tags: [publicListingsTag] },
    });
  } catch (error) {
    throw new ApiUnavailableError(`Diyarak API is unreachable: ${(error as Error).message}`);
  }

  if (response.status === 404 || response.status === 400) return null;
  if (!response.ok) throw new ApiUnavailableError(`Diyarak API returned ${response.status} for ${path}`);
  return (await response.json()) as T;
}

export function searchListings(query: URLSearchParams): Promise<PublicListingPage | null> {
  return getJson<PublicListingPage>(`/api/market/public/listings?${query.toString()}`);
}

export function getListing(listingId: string): Promise<PublicListing | null> {
  if (!/^[0-9a-fA-F-]{36}$/.test(listingId)) return Promise.resolve(null);
  return getJson<PublicListing>(`/api/market/public/listings/${listingId}`);
}
