import { NextResponse, type NextRequest } from "next/server";
import { accessCookie, cookieOptions, needsRenewal, refreshCookie, type SessionTokens } from "@/lib/sessionCookies";

const apiUrl = (process.env.DIYARAK_API_URL ?? "http://localhost:5080").replace(/\/$/, "");
const protectedPrefixes = ["/my", "/admin"];
const retryCookie = "diyarak_renew_retry";

type Renewal =
  | { kind: "renewed"; tokens: SessionTokens }
  | { kind: "invalid" }
  | { kind: "stale" }
  | { kind: "unavailable" }
  | { kind: "none" };

async function renew(refreshToken: string): Promise<Renewal> {
  try {
    const response = await fetch(`${apiUrl}/api/auth/session/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify({ refreshToken }),
      cache: "no-store",
    });
    if (response.ok) return { kind: "renewed", tokens: (await response.json()) as SessionTokens };
    if (response.status === 401) return { kind: "invalid" };
    if (response.status === 409) return { kind: "stale" };
    return { kind: "unavailable" };
  } catch {
    return { kind: "unavailable" };
  }
}

function isPrefetch(request: NextRequest): boolean {
  return (
    request.headers.get("next-router-prefetch") === "1" ||
    request.headers.get("purpose") === "prefetch" ||
    request.headers.has("x-middleware-prefetch")
  );
}

/**
 * Keeps sessions alive: renews the short-lived access token from the refresh token shortly before
 * it expires, and guards dashboard routes. Renewed cookies are applied to both the response and
 * the current request so server components see the new token immediately.
 */
export async function middleware(request: NextRequest) {
  // Background prefetches never rotate tokens: parallel rotations would race the real navigation.
  if (isPrefetch(request)) return NextResponse.next();

  const path = request.nextUrl.pathname;
  const isProtected = protectedPrefixes.some((prefix) => path === prefix || path.startsWith(`${prefix}/`));
  const access = request.cookies.get(accessCookie)?.value;
  const refresh = request.cookies.get(refreshCookie)?.value;
  const now = Date.now();

  let hasAccess = Boolean(access) && !needsRenewal(access, now - 2 * 60 * 1000);
  let renewal: Renewal = { kind: "none" };

  if (refresh && needsRenewal(access, now)) {
    renewal = await renew(refresh);
    if (renewal.kind === "renewed") {
      request.cookies.set(accessCookie, renewal.tokens.accessToken);
      if (renewal.tokens.refreshToken) request.cookies.set(refreshCookie, renewal.tokens.refreshToken);
      hasAccess = true;
    } else if (renewal.kind === "invalid") {
      request.cookies.delete(accessCookie);
      request.cookies.delete(refreshCookie);
      hasAccess = false;
    }
  }

  let response: NextResponse;
  if (isProtected && !hasAccess) {
    if (renewal.kind === "stale" && !request.cookies.has(retryCookie)) {
      // Another request rotated the token a moment ago and is delivering the new cookies.
      // Retry once with whatever the browser holds then, instead of signing the user out.
      response = NextResponse.redirect(request.nextUrl);
      response.cookies.set(retryCookie, "1", { httpOnly: true, sameSite: "lax", path: "/", maxAge: 10 });
      return response;
    }

    const login = new URL("/login", request.url);
    login.searchParams.set("next", path);
    response = NextResponse.redirect(login);
  } else {
    response = NextResponse.next({ request: { headers: request.headers } });
  }

  if (request.cookies.has(retryCookie)) response.cookies.delete(retryCookie);

  if (renewal.kind === "renewed") {
    const { tokens } = renewal;
    response.cookies.set(accessCookie, tokens.accessToken, cookieOptions(new Date(tokens.expiresAt)));
    if (tokens.refreshToken && tokens.refreshTokenExpiresAt)
      response.cookies.set(refreshCookie, tokens.refreshToken, cookieOptions(new Date(tokens.refreshTokenExpiresAt)));
  } else if (renewal.kind === "invalid") {
    response.cookies.delete(accessCookie);
    response.cookies.delete(refreshCookie);
  }

  return response;
}

export const config = {
  // Pages only: skip static assets, optimized images, proxied photos and machine endpoints.
  matcher: ["/((?!_next/|media/|api/health|robots.txt|sitemap.xml|favicon.ico).*)"],
};
