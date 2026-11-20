/** Cookie names and helpers shared by middleware and server code (no server-only imports). */
export const accessCookie = "diyarak_session";
export const refreshCookie = "diyarak_refresh";

/** Renew this long before the access token expires so in-flight requests stay authenticated. */
export const renewBeforeExpiryMs = 2 * 60 * 1000;

/** Reads the `exp` claim without verifying the signature; the API verifies every request. */
export function tokenExpiryMs(token: string): number | null {
  const payload = token.split(".")[1];
  if (!payload) return null;
  try {
    const normalized = payload.replace(/-/g, "+").replace(/_/g, "/");
    const json = JSON.parse(atob(normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), "="))) as { exp?: unknown };
    return typeof json.exp === "number" ? json.exp * 1000 : null;
  } catch {
    return null;
  }
}

export function needsRenewal(accessToken: string | undefined, now: number): boolean {
  if (!accessToken) return true;
  const expiry = tokenExpiryMs(accessToken);
  return expiry === null || expiry - now < renewBeforeExpiryMs;
}

export interface SessionTokens {
  accessToken: string;
  expiresAt: string;
  refreshToken?: string | null;
  refreshTokenExpiresAt?: string | null;
}

export function cookieOptions(expires: Date) {
  return {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax" as const,
    path: "/",
    expires,
  };
}
