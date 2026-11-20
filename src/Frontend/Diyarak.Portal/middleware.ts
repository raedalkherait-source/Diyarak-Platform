import { NextResponse, type NextRequest } from "next/server";

/** Sends visitors without a session cookie to sign-in before any dashboard page renders. */
export function middleware(request: NextRequest) {
  if (request.cookies.has("diyarak_session")) return NextResponse.next();

  const login = new URL("/login", request.url);
  login.searchParams.set("next", request.nextUrl.pathname);
  return NextResponse.redirect(login);
}

export const config = { matcher: ["/my/:path*"] };
