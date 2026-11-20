"use server";

import { revalidatePath } from "next/cache";
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
