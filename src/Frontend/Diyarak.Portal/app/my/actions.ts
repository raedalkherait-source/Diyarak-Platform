"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { errorMessage } from "@/lib/errors";
import { parseListingEditForm, parseListingForm, type FormValues } from "@/lib/listingForm";
import { managementFetch, publicPost } from "@/lib/managementApi";
import { clearSession, getRefreshToken } from "@/lib/session";
import { isListingId } from "@/lib/contact";

export interface CreateListingState {
  errors?: Record<string, string>;
  formError?: string;
  values?: FormValues;
}

export async function createListing(_state: CreateListingState, form: FormData): Promise<CreateListingState> {
  const parsed = parseListingForm(form);
  if (!parsed.ok) return { errors: parsed.errors, values: parsed.values };

  const { property, listing } = parsed.draft;

  const createdProperty = await managementFetch<{ propertyId: string }>("/api/market/properties", {
    method: "POST",
    json: property,
  });
  if (!createdProperty.ok) return { formError: errorMessage(createdProperty.code, createdProperty.status), values: parsed.values };

  const createdListing = await managementFetch<{ listingId: string; version: number }>("/api/market/listings", {
    method: "POST",
    json: { propertyId: createdProperty.value.propertyId },
  });
  if (!createdListing.ok) return { formError: errorMessage(createdListing.code, createdListing.status), values: parsed.values };

  const { listingId, version } = createdListing.value;
  const patch = { version, context: listing.context, headline: listing.headline, price: listing.price };

  let notice = "created";
  let updated = await managementFetch(`/api/market/listings/${listingId}`, {
    method: "PATCH",
    json: listing.contact ? { ...patch, contact: listing.contact } : patch,
  });

  // Keep the draft even when contact cannot be enabled; tell the publisher why.
  if (!updated.ok && updated.code === "market.listing.contact_phone_not_verified") {
    notice = "contact-unverified";
    updated = await managementFetch(`/api/market/listings/${listingId}`, { method: "PATCH", json: patch });
  }

  if (!updated.ok) return { formError: errorMessage(updated.code, updated.status), values: parsed.values };

  revalidatePath("/my");
  redirect(`/my/listings/${listingId}?notice=${notice}`);
}

export interface ActionResult {
  ok: boolean;
  error?: string;
}

export async function uploadPhoto(listingId: string, form: FormData): Promise<ActionResult> {
  if (!isListingId(listingId)) return { ok: false, error: errorMessage(null, 404) };

  const file = form.get("photo");
  if (!(file instanceof File) || file.size === 0) return { ok: false, error: "اختر صورة." };
  if (file.size > 10 * 1024 * 1024) return { ok: false, error: errorMessage("market.listing_photo.too_large") };

  const result = await managementFetch(`/api/market/listings/${listingId}/photos`, {
    method: "POST",
    body: Buffer.from(await file.arrayBuffer()),
    contentType: "application/octet-stream",
  });

  if (!result.ok) return { ok: false, error: errorMessage(result.code, result.status) };
  revalidatePath(`/my/listings/${listingId}`);
  revalidatePath(`/listings/${listingId}`);
  return { ok: true };
}

export async function deletePhoto(listingId: string, photoId: string): Promise<void> {
  if (!isListingId(listingId) || !isListingId(photoId)) return;
  await managementFetch(`/api/market/listings/${listingId}/photos/${photoId}`, { method: "DELETE" });
  revalidatePath(`/my/listings/${listingId}`);
  revalidatePath(`/listings/${listingId}`);
}

export async function publishListing(listingId: string, version: number, _state: ActionResult): Promise<ActionResult> {
  if (!isListingId(listingId)) return { ok: false, error: errorMessage(null, 404) };

  const result = await managementFetch(`/api/market/listings/${listingId}/publish`, {
    method: "POST",
    json: { version },
  });

  if (!result.ok) return { ok: false, error: errorMessage(result.code, result.status) };
  revalidatePath("/my");
  revalidatePath(`/my/listings/${listingId}`);
  return { ok: true };
}

export async function logout(): Promise<void> {
  const refreshToken = await getRefreshToken();
  if (refreshToken) await publicPost("/api/auth/session/logout", { refreshToken }).catch(() => undefined);
  await clearSession();
  redirect("/");
}

export async function logoutEverywhere(): Promise<void> {
  await managementFetch("/api/auth/session/logout-all", { method: "POST", json: {} });
  await clearSession();
  redirect("/login?signedOut=all");
}

export interface EditListingState {
  ok?: boolean;
  errors?: Record<string, string>;
  formError?: string;
  values?: FormValues;
}

export async function updateListing(
  listingId: string,
  version: number,
  _state: EditListingState,
  form: FormData,
): Promise<EditListingState> {
  if (!isListingId(listingId)) return { formError: errorMessage(null, 404) };

  const parsed = parseListingEditForm(form);
  if (!parsed.ok) return { errors: parsed.errors, values: parsed.values };

  // contact: null clears the options when both boxes are unticked.
  const result = await managementFetch(`/api/market/listings/${listingId}`, {
    method: "PATCH",
    json: { version, ...parsed.edit },
  });

  if (!result.ok) return { formError: errorMessage(result.code, result.status), values: parsed.values };

  revalidatePath("/my");
  revalidatePath(`/my/listings/${listingId}`);
  revalidatePath(`/listings/${listingId}`);
  redirect(`/my/listings/${listingId}?notice=updated`);
}

const statusActions = new Set(["pause", "resume", "close"]);
const closureReasons = new Set(["Sold", "Rented", "Withdrawn"]);

export async function changeListingStatus(listingId: string, form: FormData): Promise<void> {
  const action = String(form.get("action") ?? "");
  const reason = String(form.get("reason") ?? "");
  if (!isListingId(listingId) || !statusActions.has(action)) return;
  if (action === "close" && !closureReasons.has(reason)) return;

  const result = await managementFetch(`/api/market/listings/${listingId}/${action}`, {
    method: "POST",
    json: action === "close" ? { reason } : {},
  });

  revalidatePath("/my");
  revalidatePath(`/my/listings/${listingId}`);
  revalidatePath(`/listings/${listingId}`);
  redirect(`/my/listings/${listingId}?notice=${result.ok ? `status-${action}` : "status-failed"}`);
}
