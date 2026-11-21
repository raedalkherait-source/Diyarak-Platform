"use server";

import { revalidatePath, revalidateTag } from "next/cache";
import { publicListingsTag } from "@/lib/api";
import { isListingId } from "@/lib/contact";
import { managementFetch } from "@/lib/managementApi";

const actions = new Set(["suspend", "reinstate", "dismiss-reports"]);

export async function moderate(listingId: string, form: FormData): Promise<void> {
  const action = String(form.get("action") ?? "");
  const note = String(form.get("note") ?? "").trim();
  if (!isListingId(listingId) || !actions.has(action)) return;
  if (action === "suspend" && (note.length === 0 || note.length > 500)) return;

  await managementFetch(`/api/moderation/listings/${listingId}/${action}`, {
    method: "POST",
    json: action === "suspend" ? { note } : {},
    allowForbidden: true,
  });

  revalidatePath("/admin");
  revalidatePath(`/listings/${listingId}`);
}

const agencyDecisions = new Set(["verify", "reject"]);

export async function decideAgency(agencyId: string, form: FormData): Promise<void> {
  const decision = String(form.get("decision") ?? "");
  const note = String(form.get("note") ?? "").trim();
  if (!/^[0-9a-f-]{36}$/i.test(agencyId) || !agencyDecisions.has(decision)) return;
  if (decision === "reject" && (note.length === 0 || note.length > 500)) return;

  await managementFetch(`/api/moderation/agencies/${agencyId}/${decision}`, {
    method: "POST",
    json: decision === "reject" ? { note } : {},
    allowForbidden: true,
  });

  revalidatePath("/admin");
  revalidatePath(`/agencies/${agencyId}`);
  // The badge appears on every listing of the agency.
  revalidateTag(publicListingsTag);
}

const paymentDecisions = new Set(["confirm", "reject"]);

export async function decidePayment(orderId: string, form: FormData): Promise<void> {
  const decision = String(form.get("decision") ?? "");
  const note = String(form.get("note") ?? "").trim();
  if (!/^[0-9a-f-]{36}$/i.test(orderId) || !paymentDecisions.has(decision)) return;
  if (decision === "reject" && (note.length === 0 || note.length > 300)) return;

  await managementFetch(`/api/moderation/payments/${orderId}/${decision}`, {
    method: "POST",
    json: decision === "reject" ? { note } : {},
    allowForbidden: true,
  });

  revalidatePath("/admin");
  // A confirmed payment changes search order and badges.
  revalidateTag(publicListingsTag);
}

export async function assignPackage(agencyId: string, form: FormData): Promise<void> {
  const listingQuota = Number(form.get("listingQuota") ?? "0");
  const durationDays = Number(form.get("durationDays") ?? "0");
  const note = String(form.get("note") ?? "").trim() || null;
  if (!/^[0-9a-f-]{36}$/i.test(agencyId) || listingQuota <= 0 || durationDays <= 0) return;

  await managementFetch(`/api/moderation/agencies/${agencyId}/packages`, {
    method: "POST",
    json: { listingQuota, durationDays, note },
    allowForbidden: true,
  });

  revalidatePath("/admin");
}
