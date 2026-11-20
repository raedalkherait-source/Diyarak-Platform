import { NextResponse } from "next/server";
import { isListingId } from "@/lib/contact";
import { apiBaseUrl } from "@/lib/managementApi";
import { getSessionToken } from "@/lib/session";

/** Streams a Draft photo to its owner; the API enforces ownership with the session token. */
export async function GET(_request: Request, context: { params: Promise<{ listingId: string; photoId: string }> }) {
  const { listingId, photoId } = await context.params;
  const token = await getSessionToken();

  if (!token || !isListingId(listingId) || !isListingId(photoId))
    return new NextResponse(null, { status: 404, headers: { "Cache-Control": "no-store" } });

  const response = await fetch(`${apiBaseUrl()}/api/market/listings/${listingId}/photos/${photoId}/content`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  if (!response.ok || !response.body)
    return new NextResponse(null, { status: 404, headers: { "Cache-Control": "no-store" } });

  return new NextResponse(response.body, {
    headers: {
      "Content-Type": response.headers.get("Content-Type") ?? "application/octet-stream",
      "Cache-Control": "private, no-store",
    },
  });
}
