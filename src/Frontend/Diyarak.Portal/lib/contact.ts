const listingIdPattern = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function isListingId(value: string): boolean {
  return listingIdPattern.test(value);
}

/**
 * Returns the client address appended by the nearest proxy (the right-most X-Forwarded-For
 * entry). Left-most entries are client-controlled and must not be trusted.
 */
export function nearestForwardedFor(header: string | null): string | null {
  if (!header) return null;
  const entries = header.split(",").map((entry) => entry.trim()).filter(Boolean);
  const last = entries.at(-1);
  return last && /^[0-9a-fA-F:.]{2,45}$/.test(last) ? last : null;
}

/** Adds a pre-filled Arabic message referencing the listing to a wa.me link. */
export function whatsAppLink(baseUrl: string, headline: string, listingUrl: string): string {
  const url = new URL(baseUrl);
  if (url.protocol !== "https:" || url.hostname !== "wa.me") throw new Error("Unexpected WhatsApp URL");
  // encodeURIComponent uses %20 for spaces; WhatsApp does not decode "+" as a space.
  const text = encodeURIComponent(`مرحباً، أنا مهتم بإعلانك على ديارك: ${headline}\n${listingUrl}`);
  return `https://wa.me${url.pathname}?text=${text}`;
}

/** Formats an E.164 Syrian mobile number for display, e.g. +963 912 345 678. */
export function displayPhone(e164: string): string {
  const match = /^\+963(\d{3})(\d{3})(\d{3})$/.exec(e164);
  return match ? `+963 ${match[1]} ${match[2]} ${match[3]}` : e164;
}
