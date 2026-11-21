import { describe, expect, it } from "vitest";
import { isIndexableSearch, listingStructuredData } from "@/lib/seo";
import { jsonForScript } from "@/lib/site";
import type { PublicListing } from "@/lib/types";

const listing: PublicListing = {
  listingId: "3faf53de-b49c-477c-8e5b-bf80760c45d5",
  context: { publishingRole: "Owner", transactionIntent: "Sell" },
  headline: "شقة </script><script>alert(1)</script>",
  price: { isOnRequest: false, amount: 145000, currency: "USD" },
  property: {
    category: "Apartment",
    location: { city: "دمشق", postalCode: null, governorate: "Damascus", district: "المزة", approximate: null },
    livingArea: null, totalRooms: null, bedroomCount: null, bathroomCount: null, furnishingQuality: null,
    features: [], constructionYear: null, commercialSubtype: null, parkingSpaceCount: null,
    legalTitleType: null, finishingLevel: null, floorNumber: null, buildingFloorCount: null,
  },
  availableFromDate: null,
  photos: [{ photoId: "p1", url: "/api/market/public/listings/x/photos/p1" }],
  contact: null,
};

describe("seo", () => {
  it("indexes only simple landing searches", () => {
    expect(isIndexableSearch({})).toBe(true);
    expect(isIndexableSearch({ governorate: "Damascus", transactionIntent: "Rent" })).toBe(true);
    expect(isIndexableSearch({ governorate: "Damascus", page: "1" })).toBe(true);
    expect(isIndexableSearch({ governorate: "Damascus", page: "2" })).toBe(false);
    expect(isIndexableSearch({ priceMax: "1000" })).toBe(false);
    expect(isIndexableSearch({ governorate: ["Damascus", "Aleppo"] })).toBe(false);
  });

  it("builds RealEstateListing data with offer and address", () => {
    const data = listingStructuredData(listing);
    expect(data["@type"]).toBe("RealEstateListing");
    expect(data.offers).toMatchObject({ price: 145000, priceCurrency: "USD" });
    expect(JSON.stringify(data.contentLocation)).toContain("دمشق");
    expect((data.image as string[])[0]).toMatch(/\/media\/listings\/3faf53de-b49c-477c-8e5b-bf80760c45d5\/photos\/p1$/);
  });

  it("omits the offer when the price is on request", () => {
    expect(listingStructuredData({ ...listing, price: { isOnRequest: true, amount: null, currency: null } }).offers).toBeUndefined();
  });

  it("escapes script-breaking characters when embedding JSON", () => {
    const embedded = jsonForScript(listingStructuredData(listing));
    expect(embedded).not.toContain("</script>");
    expect(embedded).not.toContain("<");
    expect(JSON.parse(embedded).name).toBe(listing.headline);
  });
});
