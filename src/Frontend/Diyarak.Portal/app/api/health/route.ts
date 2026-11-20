import { NextResponse } from "next/server";

/** Liveness probe for containers and load balancers; does not call the API. */
export function GET() {
  return NextResponse.json({ status: "ok" }, { headers: { "Cache-Control": "no-store" } });
}
