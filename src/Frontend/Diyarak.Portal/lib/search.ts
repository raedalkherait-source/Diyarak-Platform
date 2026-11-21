/** Query parameters the portal forwards to the public listing API. Anything else is dropped. */
export const forwardedSearchKeys = [
  "transactionIntent",
  "propertyCategory",
  "governorate",
  "district",
  "city",
  "priceMin",
  "priceMax",
  "priceCurrency",
  "livingAreaMin",
  "livingAreaMax",
  "bedroomCountMin",
  "legalTitleType",
  "finishingLevel",
  "propertyFeature",
  "bbox",
  "page",
] as const;

const repeatableKeys = new Set(["transactionIntent", "propertyCategory", "governorate", "legalTitleType", "finishingLevel", "propertyFeature"]);
const numericKeys = new Set(["priceMin", "priceMax", "livingAreaMin", "livingAreaMax", "bedroomCountMin", "page"]);

export const defaultPageSize = 12;

export type RawSearchParams = Record<string, string | string[] | undefined>;

/** Normalises user-supplied search parameters into a clean API query. */
export function toApiQuery(params: RawSearchParams, pageSize: number = defaultPageSize): URLSearchParams {
  const query = new URLSearchParams();

  for (const key of forwardedSearchKeys) {
    const raw = params[key];
    const values = (Array.isArray(raw) ? raw : raw === undefined ? [] : [raw])
      .map((value) => value.trim())
      .filter((value) => value.length > 0 && value.length <= 100);

    const accepted = repeatableKeys.has(key) ? values : values.slice(0, 1);

    for (const value of accepted) {
      if (numericKeys.has(key) && !/^\d+(\.\d+)?$/.test(value)) continue;
      if (key === "bbox" && !parseBbox(value)) continue;
      query.append(key, value);
    }
  }

  // A currency only makes sense together with a price bound.
  if (!query.has("priceMin") && !query.has("priceMax")) query.delete("priceCurrency");
  else if (!query.has("priceCurrency")) query.set("priceCurrency", "USD");

  if (query.has("livingAreaMin") || query.has("livingAreaMax")) query.set("livingAreaUnit", "SquareMeter");

  const page = Number(query.get("page") ?? "1");
  query.set("page", String(Number.isInteger(page) && page >= 1 ? page : 1));
  query.set("pageSize", String(pageSize));

  return query;
}

/** Builds a portal search URL for another page while keeping the current filters. */
export function pageHref(params: RawSearchParams, page: number): string {
  const query = toApiQuery(params);
  query.delete("pageSize");
  query.delete("livingAreaUnit");
  query.set("page", String(page));
  return `/search?${query.toString()}`;
}

export function firstValue(value: string | string[] | undefined): string {
  return (Array.isArray(value) ? value[0] : value) ?? "";
}

/** The filters of a search, without paging, as stored for a saved search. */
export function savedSearchQuery(params: RawSearchParams): string {
  const query = toApiQuery(params);
  query.delete("page");
  query.delete("pageSize");
  return query.toString();
}

/** Portal URL that reopens a saved search (the portal re-adds derived parameters itself). */
export function savedSearchHref(query: string): string {
  const params = new URLSearchParams(query);
  params.delete("livingAreaUnit");
  const text = params.toString();
  return text ? `/search?${text}` : "/search";
}

export interface MapBounds {
  south: number;
  west: number;
  north: number;
  east: number;
}

/** Parses "south,west,north,east" (the API's bbox format); invalid or oversized boxes are rejected. */
export function parseBbox(value: string): MapBounds | null {
  const parts = value.split(",");
  if (parts.length !== 4 || parts.some((part) => !/^-?\d{1,3}(\.\d{1,6})?$/.test(part))) return null;
  const [south = NaN, west = NaN, north = NaN, east = NaN] = parts.map(Number);
  const valid =
    south >= -90 && north <= 90 && west >= -180 && east <= 180 &&
    south < north && west < east && north - south <= 8 && east - west <= 8;
  return valid ? { south, west, north, east } : null;
}

export function formatBbox(bounds: MapBounds): string {
  const fixed = (value: number) => (Math.round(value * 1e4) / 1e4).toString();
  return [bounds.south, bounds.west, bounds.north, bounds.east].map(fixed).join(",");
}

/** The current search with a map area (and back to page 1), or without one when bounds is null. */
export function searchWithArea(query: string, bounds: MapBounds | null): string {
  const params = new URLSearchParams(query);
  params.delete("page");
  params.delete("pageSize");
  params.delete("livingAreaUnit");
  if (bounds) params.set("bbox", formatBbox(bounds));
  else params.delete("bbox");
  const text = params.toString();
  return text ? `/search?${text}` : "/search";
}
