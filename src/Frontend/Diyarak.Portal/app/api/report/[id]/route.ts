import { NextResponse, type NextRequest } from "next/server";
import { isListingId, nearestForwardedFor } from "@/lib/contact";
import { reportReasonLabels } from "@/lib/moderation";

const apiUrl = (process.env.DIYARAK_API_URL ?? "http://localhost:5080").replace(/\/$/, "");
const noStore = { "Cache-Control": "no-store" };

/** Same-origin proxy for anonymous listing reports; forwards the client address for rate limiting. */
export async function POST(request: NextRequest, context: { params: Promise<{ id: string }> }) {
  const { id } = await context.params;
  if (request.headers.get("origin") && request.headers.get("origin") !== request.nextUrl.origin)
    return NextResponse.json({ error: "forbidden" }, { status: 403, headers: noStore });

  let body: { reason?: unknown; comment?: unknown };
  try {
    body = (await request.json()) as typeof body;
  } catch {
    return NextResponse.json({ error: "invalid" }, { status: 400, headers: noStore });
  }

  const reason = typeof body.reason === "string" ? body.reason : "";
  const comment = typeof body.comment === "string" ? body.comment.slice(0, 500) : undefined;
  if (!isListingId(id) || !Object.hasOwn(reportReasonLabels, reason))
    return NextResponse.json({ error: "invalid" }, { status: 400, headers: noStore });

  const headers: Record<string, string> = { "Content-Type": "application/json", Accept: "application/json" };
  const client = nearestForwardedFor(request.headers.get("x-forwarded-for"));
  if (client) headers["X-Forwarded-For"] = client;

  try {
    const response = await fetch(`${apiUrl}/api/market/public/listings/${id}/reports`, {
      method: "POST",
      headers,
      body: JSON.stringify({ reason, comment }),
      cache: "no-store",
    });
    return NextResponse.json({ ok: response.ok }, { status: response.ok ? 202 : response.status, headers: noStore });
  } catch {
    return NextResponse.json({ error: "unavailable" }, { status: 503, headers: noStore });
  }
}
