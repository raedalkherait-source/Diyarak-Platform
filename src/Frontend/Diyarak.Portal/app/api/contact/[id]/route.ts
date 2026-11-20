import { NextResponse, type NextRequest } from "next/server";
import { isListingId, nearestForwardedFor } from "@/lib/contact";

const apiUrl = (process.env.DIYARAK_API_URL ?? "http://localhost:5080").replace(/\/$/, "");

const noStore = { "Cache-Control": "no-store", "X-Robots-Tag": "noindex, nofollow" };

/** Same-origin proxy for revealing a publisher's number; forwards the client address for rate limiting. */
export async function GET(request: NextRequest, context: { params: Promise<{ id: string }> }) {
  const { id } = await context.params;

  if (!isListingId(id)) return NextResponse.json({ error: "not_found" }, { status: 404, headers: noStore });

  const headers: Record<string, string> = { Accept: "application/json" };
  const client = nearestForwardedFor(request.headers.get("x-forwarded-for"));
  if (client) headers["X-Forwarded-For"] = client;

  let response: Response;
  try {
    response = await fetch(`${apiUrl}/api/market/public/listings/${id}/contact`, { headers, cache: "no-store" });
  } catch {
    return NextResponse.json({ error: "unavailable" }, { status: 503, headers: noStore });
  }

  if (response.status === 429) return NextResponse.json({ error: "rate_limited" }, { status: 429, headers: noStore });
  if (!response.ok) return NextResponse.json({ error: "not_found" }, { status: 404, headers: noStore });

  return NextResponse.json(await response.json(), { headers: noStore });
}
