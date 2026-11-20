import "server-only";
import { cookies } from "next/headers";
import { accessCookie, cookieOptions, refreshCookie, tokenExpiryMs, type SessionTokens } from "./sessionCookies";

export const sessionCookie = accessCookie;

export async function getSessionToken(): Promise<string | null> {
  const token = (await cookies()).get(accessCookie)?.value;
  if (!token) return null;
  const expiry = tokenExpiryMs(token);
  return expiry && expiry > Date.now() ? token : null;
}

export async function getRefreshToken(): Promise<string | null> {
  return (await cookies()).get(refreshCookie)?.value ?? null;
}

/** Stores the access token and, when present, the long-lived refresh token as httpOnly cookies. */
export async function setSessionTokens(tokens: SessionTokens): Promise<void> {
  const jar = await cookies();
  jar.set(accessCookie, tokens.accessToken, cookieOptions(new Date(tokens.expiresAt)));
  if (tokens.refreshToken && tokens.refreshTokenExpiresAt)
    jar.set(refreshCookie, tokens.refreshToken, cookieOptions(new Date(tokens.refreshTokenExpiresAt)));
}

export async function clearSession(): Promise<void> {
  const jar = await cookies();
  jar.delete(accessCookie);
  jar.delete(refreshCookie);
}
