"use server";

import { revalidatePath, revalidateTag } from "next/cache";
import { publicListingsTag } from "@/lib/api";
import { redirect } from "next/navigation";
import { errorMessage } from "@/lib/errors";
import {
  parseListingEditForm,
  parseListingForm,
  parsePropertyEditForm,
  type FormValues,
  type ManagementProperty,
} from "@/lib/listingForm";
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
  revalidateTag(publicListingsTag);
  return { ok: true };
}

export async function deletePhoto(listingId: string, photoId: string): Promise<void> {
  if (!isListingId(listingId) || !isListingId(photoId)) return;
  await managementFetch(`/api/market/listings/${listingId}/photos/${photoId}`, { method: "DELETE" });
  revalidatePath(`/my/listings/${listingId}`);
  revalidatePath(`/listings/${listingId}`);
  revalidateTag(publicListingsTag);
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
  revalidateTag(publicListingsTag);
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
  revalidateTag(publicListingsTag);
  redirect(`/my/listings/${listingId}?notice=updated`);
}

export interface EditPropertyState {
  errors?: Record<string, string>;
  formError?: string;
  values?: FormValues;
}

const propertyIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/**
 * Updates the property behind a listing. The stored property is re-read so fields the portal does
 * not show are preserved; {@link version} is the version the form was rendered from, so edits made
 * elsewhere in the meantime are reported instead of overwritten.
 */
export async function updateProperty(
  listingId: string,
  propertyId: string,
  version: number,
  _state: EditPropertyState,
  form: FormData,
): Promise<EditPropertyState> {
  if (!isListingId(listingId) || !propertyIdPattern.test(propertyId)) return { formError: errorMessage(null, 404) };

  const current = await managementFetch<ManagementProperty>(`/api/market/properties/${propertyId}`);
  if (!current.ok) return { formError: errorMessage(current.code, current.status) };

  const parsed = parsePropertyEditForm(form, current.value);
  if (!parsed.ok) return { errors: parsed.errors, values: parsed.values };
  if (current.value.version !== version)
    return { formError: errorMessage("market.property.concurrent_modification"), values: parsed.values };

  const result = await managementFetch(`/api/market/properties/${propertyId}`, {
    method: "PUT",
    json: parsed.replacement,
  });
  if (!result.ok) return { formError: errorMessage(result.code, result.status), values: parsed.values };

  revalidatePath("/my");
  revalidatePath(`/my/listings/${listingId}`);
  revalidatePath(`/listings/${listingId}`);
  revalidateTag(publicListingsTag);
  redirect(`/my/listings/${listingId}?notice=property-updated`);
}

/** Permanently deletes a draft listing (with its photos and, if unused, its property). */
export async function deleteDraft(listingId: string, version: number): Promise<void> {
  if (!isListingId(listingId) || !Number.isSafeInteger(version) || version <= 0) return;

  const result = await managementFetch(`/api/market/listings/${listingId}?version=${version}`, { method: "DELETE" });

  revalidatePath("/my");
  if (result.ok) redirect("/my?notice=deleted");
  redirect(`/my/listings/${listingId}?notice=delete-failed`);
}

export interface SaveSearchState {
  status?: "saved" | "error";
  message?: string;
}

const saveSearchMessages: Record<string, string> = {
  "market.saved_search.duplicate": "هذا البحث محفوظ مسبقاً.",
  "market.saved_search.limit_reached": "وصلت إلى الحد الأقصى (20 بحثاً محفوظاً). احذف بعضها أولاً.",
};

/** Saves the current search filters under a name. */
export async function saveSearch(query: string, name: string, _state: SaveSearchState): Promise<SaveSearchState> {
  const result = await managementFetch("/api/market/saved-searches", { method: "POST", json: { name, query } });
  if (!result.ok) return { status: "error", message: saveSearchMessages[result.code ?? ""] ?? errorMessage(result.code, result.status) };

  revalidatePath("/my/searches");
  return { status: "saved", message: "تم حفظ البحث. ستجد عدد الإعلانات الجديدة في «عمليات البحث المحفوظة»." };
}

const savedSearchIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** Marks a saved search as seen (resetting its "new" count) and opens its results. */
export async function openSavedSearch(searchId: string, href: string): Promise<void> {
  if (!savedSearchIdPattern.test(searchId) || !href.startsWith("/search")) return;
  await managementFetch(`/api/market/saved-searches/${searchId}/seen`, { method: "POST" });
  revalidatePath("/my/searches");
  // The "new" count came from live data; show the same listings rather than a cached page.
  revalidateTag(publicListingsTag);
  redirect(href);
}

/** Turns SMS alerts for a saved search on or off. */
export async function setSavedSearchAlerts(searchId: string, enabled: boolean): Promise<void> {
  if (!savedSearchIdPattern.test(searchId)) return;
  await managementFetch(`/api/market/saved-searches/${searchId}/alerts`, { method: "PUT", json: { enabled } });
  revalidatePath("/my/searches");
  redirect(`/my/searches?notice=${enabled ? "alerts-on" : "alerts-off"}`);
}

export async function deleteSavedSearch(searchId: string): Promise<void> {
  if (!savedSearchIdPattern.test(searchId)) return;
  await managementFetch(`/api/market/saved-searches/${searchId}`, { method: "DELETE" });
  revalidatePath("/my/searches");
  redirect("/my/searches");
}

export interface AgencyFormState {
  status?: "saved" | "error";
  message?: string;
  values?: Record<string, string>;
}

const agencyMessages: Record<string, string> = {
  "market.agency.invalid_request": "تحقق من البيانات: الاسم بين 2 و 80 حرفاً والمدينة إلزامية.",
  "market.agency.concurrent_modification": "تغيّرت بيانات المكتب في مكان آخر. حدّث الصفحة وحاول مجدداً.",
  "market.agency.invalid_transition": "لا يمكن تقديم طلب توثيق الآن.",
};

/** Creates or updates the signed-in user's agency profile. */
export async function saveAgency(version: number | null, _state: AgencyFormState, form: FormData): Promise<AgencyFormState> {
  const values = {
    name: String(form.get("name") ?? "").trim(),
    description: String(form.get("description") ?? "").trim(),
    governorate: String(form.get("governorate") ?? ""),
    city: String(form.get("city") ?? "").trim(),
  };
  const result = await managementFetch("/api/market/agency", {
    method: "PUT",
    json: { ...values, description: values.description || null, governorate: values.governorate || null, version },
  });
  if (!result.ok) return { status: "error", message: agencyMessages[result.code ?? ""] ?? errorMessage(result.code, result.status), values };

  revalidatePath("/my/agency");
  revalidateTag(publicListingsTag);
  redirect("/my/agency?notice=saved");
}

export async function requestAgencyVerification(version: number, _state: AgencyFormState, form: FormData): Promise<AgencyFormState> {
  const registrationNumber = String(form.get("registrationNumber") ?? "").trim();
  if (!registrationNumber || registrationNumber.length > 60) return { status: "error", message: "أدخل رقم السجل التجاري أو الترخيص." };

  const result = await managementFetch("/api/market/agency/verification", { method: "POST", json: { registrationNumber, version } });
  if (!result.ok) return { status: "error", message: agencyMessages[result.code ?? ""] ?? errorMessage(result.code, result.status) };

  revalidatePath("/my/agency");
  redirect("/my/agency?notice=requested");
}

export interface PaymentFormState {
  status?: "error";
  message?: string;
}

const paymentMessages: Record<string, string> = {
  "market.payment.invalid_request": "تحقق من رقم العملية (بين 3 و 60 حرفاً).",
  "market.payment.not_offered": "طريقة الدفع أو الباقة غير متاحة حالياً.",
  "market.payment.listing_not_published": "يمكن تمييز الإعلانات المنشورة فقط.",
  "market.payment.pending_exists": "لديك طلب دفع قيد المراجعة لهذا الإعلان.",
};

/** Submits a manual payment (bank transfer or mobile wallet) for a featured period. */
export async function createPaymentOrder(listingId: string, _state: PaymentFormState, form: FormData): Promise<PaymentFormState> {
  if (!isListingId(listingId)) return { status: "error", message: errorMessage(null, 404) };
  const product = String(form.get("product") ?? "");
  const method = String(form.get("method") ?? "");
  const reference = String(form.get("reference") ?? "").trim();

  const result = await managementFetch("/api/market/payments/orders", { method: "POST", json: { listingId, product, method, reference } });
  if (!result.ok) return { status: "error", message: paymentMessages[result.code ?? ""] ?? errorMessage(result.code, result.status) };

  revalidatePath(`/my/listings/${listingId}`);
  redirect(`/my/listings/${listingId}?notice=payment-submitted`);
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
  revalidateTag(publicListingsTag);
  redirect(`/my/listings/${listingId}?notice=${result.ok ? `status-${action}` : "status-failed"}`);
}

export interface BulkImportState {
  status?: "success" | "error";
  message?: string;
  created?: number;
  failed?: number;
  errors?: string[];
}

export async function bulkImportCsv(_state: BulkImportState, form: FormData): Promise<BulkImportState> {
  const file = form.get("csv");
  if (!(file instanceof File) || file.size === 0) return { status: "error", message: "اختر ملف CSV." };
  if (file.size > 2 * 1024 * 1024) return { status: "error", message: "حجم الملف يجب ألا يتجاوز 2 ميغابايت." };

  const body = new FormData();
  body.append("file", file);

  const result = await managementFetch<{ created: number; failed: number; errors: string[] }>("/api/market/import/csv", {
    method: "POST",
    body,
  });

  if (!result.ok) return { status: "error", message: errorMessage(result.code, result.status) };

  revalidatePath("/my");
  return {
    status: "success",
    message: `تم استيراد ${result.value.created} إعلان بنجاح.`,
    created: result.value.created,
    failed: result.value.failed,
    errors: result.value.errors,
  };
}

// --- Off-plan projects ---

export interface OffPlanProjectFormState {
  status?: "saved" | "error";
  message?: string;
}

interface OffPlanProjectPayload {
  name: string;
  description: string | null;
  governorate: string;
  district: string;
  constructionStatus: string;
  totalUnits: number;
  minPriceUsd: number | null;
  maxPriceUsd: number | null;
  expectedCompletion: string | null;
}

function parseOffPlanForm(form: FormData): OffPlanProjectPayload {
  return {
    name: String(form.get("name") ?? "").trim(),
    description: String(form.get("description") ?? "").trim() || null,
    governorate: String(form.get("governorate") ?? ""),
    district: String(form.get("district") ?? "").trim(),
    constructionStatus: String(form.get("constructionStatus") ?? "Planned"),
    totalUnits: parseInt(String(form.get("totalUnits") ?? "0"), 10) || 0,
    minPriceUsd: form.get("minPriceUsd") ? parseFloat(String(form.get("minPriceUsd"))) || null : null,
    maxPriceUsd: form.get("maxPriceUsd") ? parseFloat(String(form.get("maxPriceUsd"))) || null : null,
    expectedCompletion: String(form.get("expectedCompletion") ?? "").trim() || null,
  };
}

export async function createOffPlanProject(_state: OffPlanProjectFormState, form: FormData): Promise<OffPlanProjectFormState> {
  const payload = parseOffPlanForm(form);
  const result = await managementFetch("/api/market/offplan", { method: "POST", json: payload });
  if (!result.ok) return { status: "error", message: errorMessage(result.code, result.status) };
  revalidatePath("/my/projects");
  redirect("/my/projects?notice=created");
}

export async function updateOffPlanProject(projectId: string, expectedVersion: number, _state: OffPlanProjectFormState, form: FormData): Promise<OffPlanProjectFormState> {
  const payload = { ...parseOffPlanForm(form), expectedVersion };
  const result = await managementFetch(`/api/market/offplan/${projectId}`, { method: "PUT", json: payload });
  if (!result.ok) return { status: "error", message: errorMessage(result.code, result.status) };
  revalidatePath("/my/projects");
  return { status: "saved", message: "تم حفظ التعديلات." };
}

export async function toggleOffPlanPublish(projectId: string, expectedVersion: number, publish: boolean): Promise<void> {
  await managementFetch(`/api/market/offplan/${projectId}/publish`, { method: "PUT", json: { expectedVersion, publish } });
  revalidatePath("/my/projects");
  redirect(`/my/projects?notice=${publish ? "published" : "unpublished"}`);
}

export async function deleteOffPlanProject(projectId: string): Promise<void> {
  await managementFetch(`/api/market/offplan/${projectId}`, { method: "DELETE" });
  revalidatePath("/my/projects");
  redirect("/my/projects?notice=deleted");
}
