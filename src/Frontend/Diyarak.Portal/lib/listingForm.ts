import {
  categoryLabels,
  commercialSubtypeLabels,
  featureLabels,
  finishingLevelLabels,
  governorateLabels,
  legalTitleLabels,
  publishingRoleLabels,
  transactionIntentLabels,
} from "./labels";

export type FormValues = Record<string, string | string[]>;

export interface ListingDraft {
  property: Record<string, unknown>;
  listing: {
    context: { publishingRole: string; transactionIntent: string };
    headline: string;
    price: { isOnRequest: true } | { isOnRequest: false; amount: number; currency: string };
    contact: { allowCalls: boolean; allowWhatsApp: boolean } | null;
  };
}

export type ParseResult =
  | { ok: true; draft: ListingDraft; values: FormValues }
  | { ok: false; errors: Record<string, string>; values: FormValues };

const singleFields = [
  "category", "commercialSubtype", "governorate", "city", "district", "livingArea", "totalRooms",
  "bedroomCount", "bathroomCount", "floorNumber", "buildingFloorCount", "legalTitleType",
  "finishingLevel", "transactionIntent", "publishingRole", "headline", "priceMode", "priceAmount",
  "priceCurrency", "allowCalls", "allowWhatsApp",
] as const;

function readValues(form: FormData): FormValues {
  const values: FormValues = {};
  for (const name of singleFields) {
    const value = form.get(name);
    values[name] = typeof value === "string" ? value.trim() : "";
  }
  values.features = form.getAll("features").filter((value): value is string => typeof value === "string");
  return values;
}

function oneOf(value: string, allowed: Record<string, string>): boolean {
  return Object.prototype.hasOwnProperty.call(allowed, value);
}

function integer(value: string, min: number, max: number): number | null | "invalid" {
  if (value === "") return null;
  if (!/^-?\d+$/.test(value)) return "invalid";
  const parsed = Number(value);
  return parsed >= min && parsed <= max ? parsed : "invalid";
}

function decimal(value: string, min: number, max: number): number | null | "invalid" {
  if (value === "") return null;
  if (!/^\d+(\.\d{1,2})?$/.test(value)) return "invalid";
  const parsed = Number(value);
  return parsed >= min && parsed <= max ? parsed : "invalid";
}

/** Validates the "new listing" form and maps it to API payloads. */
export function parseListingForm(form: FormData): ParseResult {
  const values = readValues(form);
  const v = (name: string) => values[name] as string;
  const errors: Record<string, string> = {};

  if (!oneOf(v("category"), categoryLabels)) errors.category = "اختر نوع العقار.";
  if (v("category") === "CommercialProperty" && v("commercialSubtype") && !oneOf(v("commercialSubtype"), commercialSubtypeLabels))
    errors.commercialSubtype = "النوع التجاري غير صالح.";
  if (!oneOf(v("governorate"), governorateLabels)) errors.governorate = "اختر المحافظة.";
  if (!v("city") || v("city").length > 100) errors.city = "أدخل المدينة.";
  if (v("district").length > 100) errors.district = "اسم الحي طويل جداً.";

  const livingArea = decimal(v("livingArea"), 1, 100000);
  if (livingArea === "invalid") errors.livingArea = "أدخل مساحة صحيحة بالمتر المربع.";
  const totalRooms = integer(v("totalRooms"), 0, 100);
  if (totalRooms === "invalid") errors.totalRooms = "عدد غير صالح.";
  const bedroomCount = integer(v("bedroomCount"), 0, 100);
  if (bedroomCount === "invalid") errors.bedroomCount = "عدد غير صالح.";
  const bathroomCount = integer(v("bathroomCount"), 0, 100);
  if (bathroomCount === "invalid") errors.bathroomCount = "عدد غير صالح.";
  const floorNumber = integer(v("floorNumber"), -5, 200);
  if (floorNumber === "invalid") errors.floorNumber = "الطابق بين -5 و 200 (0 للأرضي).";
  const buildingFloorCount = integer(v("buildingFloorCount"), 1, 200);
  if (buildingFloorCount === "invalid") errors.buildingFloorCount = "عدد غير صالح.";
  if (typeof floorNumber === "number" && typeof buildingFloorCount === "number" && floorNumber > buildingFloorCount)
    errors.floorNumber = "الطابق أعلى من عدد طوابق البناء.";

  if (v("legalTitleType") && !oneOf(v("legalTitleType"), legalTitleLabels)) errors.legalTitleType = "قيمة غير صالحة.";
  if (v("finishingLevel") && !oneOf(v("finishingLevel"), finishingLevelLabels)) errors.finishingLevel = "قيمة غير صالحة.";
  const features = values.features as string[];
  if (features.some((feature) => !oneOf(feature, featureLabels))) errors.features = "ميزة غير معروفة.";

  if (!oneOf(v("transactionIntent"), transactionIntentLabels)) errors.transactionIntent = "اختر نوع العرض.";
  if (!oneOf(v("publishingRole"), publishingRoleLabels)) errors.publishingRole = "اختر صفة المعلن.";
  if (v("headline").length < 5 || v("headline").length > 120) errors.headline = "العنوان بين 5 و 120 حرفاً.";

  let price: ListingDraft["listing"]["price"] = { isOnRequest: true };
  if (v("priceMode") !== "onRequest") {
    const amount = decimal(v("priceAmount"), 1, 1e13);
    if (amount === null || amount === "invalid") errors.priceAmount = "أدخل السعر بالأرقام.";
    if (!["USD", "SYP"].includes(v("priceCurrency"))) errors.priceCurrency = "اختر العملة.";
    if (typeof amount === "number") price = { isOnRequest: false, amount, currency: v("priceCurrency") };
  }

  const allowCalls = v("allowCalls") === "on";
  const allowWhatsApp = v("allowWhatsApp") === "on";

  if (Object.keys(errors).length > 0) return { ok: false, errors, values };

  const property: Record<string, unknown> = {
    category: v("category"),
    address: {
      governorate: v("governorate"),
      city: v("city"),
      ...(v("district") ? { district: v("district") } : {}),
    },
  };
  const optional: Record<string, unknown> = {
    livingArea: livingArea === null ? undefined : { value: livingArea, unit: "SquareMeter" },
    totalRooms, bedroomCount, bathroomCount, floorNumber, buildingFloorCount,
    legalTitleType: v("legalTitleType") || undefined,
    finishingLevel: v("finishingLevel") || undefined,
    commercialSubtype: v("category") === "CommercialProperty" ? v("commercialSubtype") || undefined : undefined,
    features: features.length > 0 ? features : undefined,
  };
  for (const [key, value] of Object.entries(optional)) if (value !== null && value !== undefined) property[key] = value;

  return {
    ok: true,
    values,
    draft: {
      property,
      listing: {
        context: { publishingRole: v("publishingRole"), transactionIntent: v("transactionIntent") },
        headline: v("headline"),
        price,
        contact: allowCalls || allowWhatsApp ? { allowCalls, allowWhatsApp } : null,
      },
    },
  };
}

export interface ListingEdit {
  context: { publishingRole: string; transactionIntent: string };
  headline: string;
  price: ListingDraft["listing"]["price"];
  contact: { allowCalls: boolean; allowWhatsApp: boolean } | null;
}

export type EditParseResult =
  | { ok: true; edit: ListingEdit; values: FormValues }
  | { ok: false; errors: Record<string, string>; values: FormValues };

/** Validates the edit form for an existing listing (offer, headline, price, contact). */
export function parseListingEditForm(form: FormData): EditParseResult {
  const values = readValues(form);
  const v = (name: string) => values[name] as string;
  const errors: Record<string, string> = {};

  if (!oneOf(v("transactionIntent"), transactionIntentLabels)) errors.transactionIntent = "اختر نوع العرض.";
  if (!oneOf(v("publishingRole"), publishingRoleLabels)) errors.publishingRole = "اختر صفة المعلن.";
  if (v("headline").length < 5 || v("headline").length > 120) errors.headline = "العنوان بين 5 و 120 حرفاً.";

  let price: ListingEdit["price"] = { isOnRequest: true };
  if (v("priceMode") !== "onRequest") {
    const amount = decimal(v("priceAmount"), 1, 1e13);
    if (amount === null || amount === "invalid") errors.priceAmount = "أدخل السعر بالأرقام.";
    if (!["USD", "SYP"].includes(v("priceCurrency"))) errors.priceCurrency = "اختر العملة.";
    if (typeof amount === "number") price = { isOnRequest: false, amount, currency: v("priceCurrency") };
  }

  const allowCalls = v("allowCalls") === "on";
  const allowWhatsApp = v("allowWhatsApp") === "on";

  if (Object.keys(errors).length > 0) return { ok: false, errors, values };

  return {
    ok: true,
    values,
    edit: {
      context: { publishingRole: v("publishingRole"), transactionIntent: v("transactionIntent") },
      headline: v("headline"),
      price,
      contact: allowCalls || allowWhatsApp ? { allowCalls, allowWhatsApp } : null,
    },
  };
}
