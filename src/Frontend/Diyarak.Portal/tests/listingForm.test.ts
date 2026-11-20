import { describe, expect, it } from "vitest";
import { parseListingForm } from "@/lib/listingForm";

function form(entries: Record<string, string | string[]>): FormData {
  const data = new FormData();
  for (const [key, value] of Object.entries(entries))
    for (const item of Array.isArray(value) ? value : [value]) data.append(key, item);
  return data;
}

const valid = {
  category: "Apartment",
  governorate: "Damascus",
  city: "دمشق",
  district: "المزة",
  livingArea: "150",
  bedroomCount: "3",
  floorNumber: "0",
  buildingFloorCount: "5",
  legalTitleType: "GreenTitleDeed",
  finishingLevel: "SuperDeluxe",
  features: ["Elevator", "SolarPowerSystem"],
  transactionIntent: "Sell",
  publishingRole: "Owner",
  headline: "شقة سوبر ديلوكس في المزة",
  priceMode: "known",
  priceAmount: "120000",
  priceCurrency: "USD",
  allowWhatsApp: "on",
};

describe("parseListingForm", () => {
  it("maps a valid form to property and listing payloads", () => {
    const result = parseListingForm(form(valid));
    expect(result.ok).toBe(true);
    if (!result.ok) return;

    expect(result.draft.property).toEqual({
      category: "Apartment",
      address: { governorate: "Damascus", city: "دمشق", district: "المزة" },
      livingArea: { value: 150, unit: "SquareMeter" },
      bedroomCount: 3,
      floorNumber: 0,
      buildingFloorCount: 5,
      legalTitleType: "GreenTitleDeed",
      finishingLevel: "SuperDeluxe",
      features: ["Elevator", "SolarPowerSystem"],
    });
    expect(result.draft.listing).toEqual({
      context: { publishingRole: "Owner", transactionIntent: "Sell" },
      headline: "شقة سوبر ديلوكس في المزة",
      price: { isOnRequest: false, amount: 120000, currency: "USD" },
      contact: { allowCalls: false, allowWhatsApp: true },
    });
  });

  it("supports price on request and no contact", () => {
    const result = parseListingForm(form({ ...valid, priceMode: "onRequest", priceAmount: "", allowWhatsApp: "" }));
    expect(result.ok && result.draft.listing.price).toEqual({ isOnRequest: true });
    expect(result.ok && result.draft.listing.contact).toBeNull();
  });

  it("returns Arabic field errors and keeps entered values", () => {
    const result = parseListingForm(
      form({ ...valid, category: "Castle", governorate: "", headline: "قصير", priceAmount: "abc", floorNumber: "9", buildingFloorCount: "4" }),
    );
    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(Object.keys(result.errors).sort()).toEqual(["category", "floorNumber", "governorate", "headline", "priceAmount"]);
    expect(result.values.city).toBe("دمشق");
    expect(result.values.features).toEqual(["Elevator", "SolarPowerSystem"]);
  });

  it("rejects unknown features and commercial subtype outside commercial category", () => {
    const unknown = parseListingForm(form({ ...valid, features: ["Helipad"] }));
    expect(!unknown.ok && unknown.errors.features).toBeTruthy();

    const notCommercial = parseListingForm(form({ ...valid, commercialSubtype: "Retail" }));
    expect(notCommercial.ok && "commercialSubtype" in notCommercial.draft.property).toBe(false);
  });
});

describe("parseListingEditForm", () => {
  it("maps offer, headline, price and cleared contact", async () => {
    const { parseListingEditForm } = await import("@/lib/listingForm");
    const result = parseListingEditForm(
      form({ transactionIntent: "Sell", publishingRole: "Owner", headline: "سعر مخفّض للبيع", priceMode: "known", priceAmount: "99000", priceCurrency: "USD" }),
    );
    expect(result.ok && result.edit).toEqual({
      context: { publishingRole: "Owner", transactionIntent: "Sell" },
      headline: "سعر مخفّض للبيع",
      price: { isOnRequest: false, amount: 99000, currency: "USD" },
      contact: null,
    });
  });

  it("rejects an invalid edit", async () => {
    const { parseListingEditForm } = await import("@/lib/listingForm");
    const result = parseListingEditForm(form({ transactionIntent: "Barter", publishingRole: "Owner", headline: "x", priceMode: "known", priceAmount: "" }));
    expect(!result.ok && Object.keys(result.errors).sort()).toEqual(["headline", "priceAmount", "priceCurrency", "transactionIntent"]);
  });
});

describe("parseListingForm location", () => {
  it("adds the map pin to the address when both coordinates are set", () => {
    const result = parseListingForm(form({ ...valid, latitude: "33.5138", longitude: "36.2765" }));
    expect(result.ok && (result.draft.property.address as Record<string, unknown>).location).toEqual({
      latitude: 33.5138,
      longitude: 36.2765,
    });
  });

  it("reports an invalid pin", () => {
    const result = parseListingForm(form({ ...valid, latitude: "33.5", longitude: "" }));
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.errors.location).toBeDefined();
  });
});
