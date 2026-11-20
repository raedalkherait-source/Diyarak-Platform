/** Accepts only same-site relative paths to prevent open redirects after sign-in. */
export function safeNextPath(value: string | null | undefined, fallback = "/my"): string {
  if (!value || !value.startsWith("/") || value.startsWith("//") || value.includes("\\")) return fallback;
  return value;
}
