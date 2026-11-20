"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { errorMessage } from "@/lib/errors";
import { parseListingForm, type FormValues } from "@/lib/listingForm";
import { managementFetch } from "@/lib/managementApi";
import { clearSession } from "@/lib/session";
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
  return { ok: true };
}

export async function deletePhoto(listingId: string, photoId: string): Promise<void> {
  if (!isListingId(listingId) || !isListingId(photoId)) return;
  await managementFetch(`/api/market/listings/${listingId}/photos/${photoId}`, { method: "DELETE" });
  revalidatePath(`/my/listings/${listingId}`);
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
  await clearSession();
  redirect("/");
}
