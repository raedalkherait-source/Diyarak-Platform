/** Crawlers, link previews and headless tools that should not count as listing views. */
const botPattern =
  /bot|crawl|spider|slurp|facebookexternalhit|whatsapp|telegram|preview|headless|lighthouse|curl|wget|python|httpclient|axios|node-fetch/i;

export function isLikelyBot(userAgent: string | null): boolean {
  return !userAgent || botPattern.test(userAgent);
}

export interface DailyStatistics {
  day: string;
  views: number;
  contactReveals: number;
}

export interface ListingStatistics {
  listingId: string;
  from: string;
  to: string;
  views: number;
  contactReveals: number;
  favorites: number;
  days: DailyStatistics[];
}

/** A "nice" axis maximum (1, 2, 5 × 10ⁿ) at or above the largest value, never below 4. */
export function niceMax(values: number[]): number {
  const max = Math.max(4, ...values);
  const magnitude = 10 ** Math.floor(Math.log10(max));
  return [1, 2, 5, 10].map((step) => step * magnitude).find((candidate) => candidate >= max) ?? max;
}
