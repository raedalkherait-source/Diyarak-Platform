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
