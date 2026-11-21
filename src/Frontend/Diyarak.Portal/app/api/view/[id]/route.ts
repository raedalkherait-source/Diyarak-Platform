import { NextResponse, type NextRequest } from "next/server";
import { isListingId, nearestForwardedFor } from "@/lib/contact";
import { getSessionToken } from "@/lib/session";
import { isLikelyBot } from "@/lib/statistics";

const apiUrl = (process.env.DIYARAK_API_URL ?? "http://localhost:5080").replace(/\/$/, "");

/**
 * View beacon. Bots are dropped here; signed-in viewers are forwarded with their session so the
 * API can ignore publishers viewing their own listing. Always 204: nothing is revealed.
 */
export async function POST(request: NextRequest, context: { params: Promise<{ id: string }> }) {
  const { id } = await context.params;
  const done = new NextResponse(null, { status: 204, headers: { "Cache-Control": "no-store" } });

  if (!isListingId(id) || isLikelyBot(request.headers.get("user-agent"))) return done;

  const headers: Record<string, string> = {};
  const client = nearestForwardedFor(request.headers.get("x-forwarded-for"));
  if (client) headers["X-Forwarded-For"] = client;

  const token = await getSessionToken();
  const path = token ? `/api/market/listings/${id}/views` : `/api/market/public/listings/${id}/views`;
  if (token) headers.Authorization = `Bearer ${token}`;

  try {
    await fetch(`${apiUrl}${path}`, { method: "POST", headers, cache: "no-store" });
  } catch {
    // Statistics are best-effort; a lost view must never affect the visitor.
  }

  return done;
}
