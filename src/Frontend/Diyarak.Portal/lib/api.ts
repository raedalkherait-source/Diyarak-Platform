import "server-only";
import type { PublicListing, PublicListingPage } from "./types";

const apiUrl = (process.env.DIYARAK_API_URL ?? "http://localhost:5080").replace(/\/$/, "");

export class ApiUnavailableError extends Error {}

async function getJson<T>(path: string): Promise<T | null> {
  let response: Response;

  try {
    response = await fetch(`${apiUrl}${path}`, {
      headers: { Accept: "application/json" },
      next: { revalidate: 30 },
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
