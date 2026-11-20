/** Public origin of the portal, used for canonical URLs, the sitemap and structured data. */
export function siteUrl(): string {
  return (process.env.PORTAL_PUBLIC_URL ?? "http://localhost:3000").replace(/\/$/, "");
}

/** Escapes JSON for safe embedding inside a <script> element. */
export function jsonForScript(value: unknown): string {
  return JSON.stringify(value)
    .replace(/</g, "\\u003c")
    .replace(/>/g, "\\u003e")
    .replace(/&/g, "\\u0026")
    .replace(/[\u2028\u2029]/g, (character) => (character === "\u2028" ? "\\u2028" : "\\u2029"));
}
