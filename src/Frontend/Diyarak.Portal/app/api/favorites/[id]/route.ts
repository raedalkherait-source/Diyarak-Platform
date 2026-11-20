import { NextResponse, type NextRequest } from "next/server";
import { isListingId, nearestForwardedFor } from "@/lib/contact";
import { getSessionToken } from "@/lib/session";

const apiUrl = (process.env.DIYARAK_API_URL ?? "http://localhost:5080").replace(/\/$/, "");
const noStore = { "Cache-Control": "no-store", "X-Robots-Tag": "noindex, nofollow" };

type Context = { params: Promise<{ id: string }> };

/** Same-origin proxy for the visitor's saved state of one listing; the session cookie never reaches the browser's JS. */
async function forward(request: NextRequest, context: Context, method: "GET" | "PUT" | "DELETE") {
  const { id } = await context.params;
  if (!isListingId(id)) return NextResponse.json({ error: "not_found" }, { status: 404, headers: noStore });

  const token = await getSessionToken();
  if (!token) return NextResponse.json({ signedIn: false, isFavorite: false }, { headers: noStore });

  const headers: Record<string, string> = { Accept: "application/json", Authorization: `Bearer ${token}` };
  const client = nearestForwardedFor(request.headers.get("x-forwarded-for"));
  if (client) headers["X-Forwarded-For"] = client;

  let response: Response;
  try {
    response = await fetch(`${apiUrl}/api/market/favorites/${id}`, { method, headers, cache: "no-store" });
  } catch {
    return NextResponse.json({ error: "unavailable" }, { status: 503, headers: noStore });
  }

  if (response.status === 401 || response.status === 403)
    return NextResponse.json({ signedIn: false, isFavorite: false }, { headers: noStore });

  if (!response.ok) {
    let code: string | null = null;
    try {
      code = ((await response.json()) as { code?: string }).code ?? null;
    } catch {
      code = null;
    }
    return NextResponse.json({ error: code ?? "failed" }, { status: response.status, headers: noStore });
  }

  if (method === "GET") {
    const body = (await response.json()) as { isFavorite: boolean };
    return NextResponse.json({ signedIn: true, isFavorite: body.isFavorite }, { headers: noStore });
  }

  return NextResponse.json({ signedIn: true, isFavorite: method === "PUT" }, { headers: noStore });
}

export function GET(request: NextRequest, context: Context) {
  return forward(request, context, "GET");
}

export function PUT(request: NextRequest, context: Context) {
  return forward(request, context, "PUT");
}

export function DELETE(request: NextRequest, context: Context) {
  return forward(request, context, "DELETE");
}
