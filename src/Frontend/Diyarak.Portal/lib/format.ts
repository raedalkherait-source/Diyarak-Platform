import { areaUnitLabels, label } from "./labels";
import type { PublicListing, PublicListingArea } from "./types";

const currencyLabels: Record<string, string> = {
  USD: "$",
  SYP: "ل.س",
  EUR: "€",
};

/** Formats with Western digits, which Syrian users read most reliably online. */
const numberFormat = new Intl.NumberFormat("ar-SY-u-nu-latn", { maximumFractionDigits: 2 });

export function formatNumber(value: number): string {
  return numberFormat.format(value);
}

export function formatPrice(price: PublicListing["price"]): string {
  if (price.isOnRequest || price.amount === null) return "السعر عند الاتصال";
  const symbol = price.currency ? (currencyLabels[price.currency] ?? price.currency) : "";
  return `${formatNumber(price.amount)} ${symbol}`.trim();
}

export function formatArea(area: PublicListingArea | null): string {
  if (!area) return "";
  return `${formatNumber(area.value)} ${label(areaUnitLabels, area.unit)}`;
}

export function formatFloor(floor: number | null, buildingFloors: number | null): string {
  if (floor === null) return "";
  const name = floor === 0 ? "الطابق الأرضي" : floor < 0 ? `القبو ${formatNumber(-floor)}` : `الطابق ${formatNumber(floor)}`;
  return buildingFloors ? `${name} من ${formatNumber(buildingFloors)}` : name;
}

export function formatLocation(location: PublicListing["property"]["location"]): string {
  return [location.district, location.city].filter(Boolean).join("، ");
}

/** Maps an API photo URL to the portal's same-origin media path. */
export function photoSrc(listingId: string, photoId: string): string {
  return `/media/listings/${encodeURIComponent(listingId)}/photos/${encodeURIComponent(photoId)}`;
}
