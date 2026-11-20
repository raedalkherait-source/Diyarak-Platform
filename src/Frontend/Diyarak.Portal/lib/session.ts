import "server-only";
import { cookies } from "next/headers";

export const sessionCookie = "diyarak_session";

/** Reads the `exp` claim without verifying the signature; the API verifies every request. */
export function tokenExpiry(token: string): Date | null {
  const payload = token.split(".")[1];
  if (!payload) return null;
  try {
    const json = JSON.parse(Buffer.from(payload, "base64url").toString("utf8")) as { exp?: unknown };
    return typeof json.exp === "number" ? new Date(json.exp * 1000) : null;
  } catch {
    return null;
  }
}

export async function getSessionToken(): Promise<string | null> {
  const token = (await cookies()).get(sessionCookie)?.value;
  if (!token) return null;
  const expiry = tokenExpiry(token);
  return expiry && expiry.getTime() > Date.now() ? token : null;
}

export async function setSessionToken(token: string): Promise<void> {
  const expires = tokenExpiry(token) ?? new Date(Date.now() + 60 * 60 * 1000);
  (await cookies()).set(sessionCookie, token, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    expires,
  });
}

export async function clearSession(): Promise<void> {
  (await cookies()).delete(sessionCookie);
}
