import "server-only";
import { headers } from "next/headers";
import { redirect } from "next/navigation";
import { nearestForwardedFor } from "./contact";
import { getSessionToken } from "./session";

const apiUrl = (process.env.DIYARAK_API_URL ?? "http://localhost:5080").replace(/\/$/, "");

export interface ManagementListing {
  listingId: string;
  version: number;
  status: "Draft" | "Published" | "Paused" | "Closed";
  subject: { subjectId: string; subjectType: string };
  context: { publishingRole: string; transactionIntent: string } | null;
  headline: string | null;
  price: { isOnRequest: boolean; amount: number | null; currency: string | null } | null;
  availableFromDate: string | null;
  contact: { allowCalls: boolean; allowWhatsApp: boolean } | null;
  closureReason: "Sold" | "Rented" | "Withdrawn" | null;
}

export interface ManagementPhoto {
  photoId: string;
  position: number;
  contentType: string;
  sizeBytes: number;
  uploadedAt: string;
  url: string;
}

export type ApiResult<T> = { ok: true; value: T } | { ok: false; status: number; code: string | null };

async function forwardedHeaders(): Promise<Record<string, string>> {
  const client = nearestForwardedFor((await headers()).get("x-forwarded-for"));
  return client ? { "X-Forwarded-For": client } : {};
}

/** Calls an authenticated management route. Missing or expired sessions redirect to login. */
export async function managementFetch<T>(
  path: string,
  init: { method?: string; json?: unknown; body?: BodyInit; contentType?: string } = {},
): Promise<ApiResult<T>> {
  const token = await getSessionToken();
  if (!token) redirect("/login");

  const requestHeaders: Record<string, string> = {
    Accept: "application/json",
    Authorization: `Bearer ${token}`,
    ...(await forwardedHeaders()),
  };

  let body = init.body;
  if (init.json !== undefined) {
    body = JSON.stringify(init.json);
    requestHeaders["Content-Type"] = "application/json";
  } else if (init.contentType) {
    requestHeaders["Content-Type"] = init.contentType;
  }

  const response = await fetch(`${apiUrl}${path}`, {
    method: init.method ?? "GET",
    headers: requestHeaders,
    body,
    cache: "no-store",
  });

  if (response.status === 401 || response.status === 403) redirect("/login?expired=1");

  if (!response.ok) {
    let code: string | null = null;
    try {
      code = ((await response.json()) as { code?: string }).code ?? null;
    } catch {
      code = null;
    }
    return { ok: false, status: response.status, code };
  }

  const text = await response.text();
  return { ok: true, value: (text ? JSON.parse(text) : null) as T };
}

/** Anonymous call used by the sign-in flow. */
export async function publicPost<T>(path: string, json: unknown): Promise<ApiResult<T>> {
  const response = await fetch(`${apiUrl}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json", ...(await forwardedHeaders()) },
    body: JSON.stringify(json),
    cache: "no-store",
  });

  if (!response.ok) {
    let code: string | null = null;
    try {
      code = ((await response.json()) as { code?: string }).code ?? null;
    } catch {
      code = null;
    }
    return { ok: false, status: response.status, code };
  }

  return { ok: true, value: (await response.json()) as T };
}

export function apiBaseUrl(): string {
  return apiUrl;
}
