import { describe, expect, it } from "vitest";
import { isIndexableSearch, listingStructuredData, breadcrumbStructuredData, faqStructuredData, websiteStructuredData, organizationStructuredData } from "@/lib/seo";
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
  agency: null,
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

  it("builds BreadcrumbList with positions and URLs", () => {
    const data = breadcrumbStructuredData([
      { name: "الرئيسية", href: "/" },
      { name: "الأسئلة الشائعة" },
    ]);
    expect(data["@type"]).toBe("BreadcrumbList");
    const items = data.itemListElement as Array<{ position: number; name: string; item?: string }>;
    expect(items).toHaveLength(2);
    expect(items[0].position).toBe(1);
    expect(items[0].item).toContain("/");
    expect(items[1].position).toBe(2);
    expect(items[1].item).toBeUndefined();
  });

  it("builds WebSite data with SearchAction", () => {
    const data = websiteStructuredData();
    expect(data["@type"]).toBe("WebSite");
    expect(data.name).toBe("ديارك");
    expect(data.url).toBeTruthy();
    const action = data.potentialAction as Record<string, unknown>;
    expect(action["@type"]).toBe("SearchAction");
    const target = action.target as Record<string, unknown>;
    expect(target.urlTemplate).toContain("/search?district=");
  });

  it("builds Organization data with logo and area served", () => {
    const data = organizationStructuredData();
    expect(data["@type"]).toBe("Organization");
    expect(data.name).toBe("ديارك");
    expect(data.logo).toContain("/icon-192.png");
    const area = data.areaServed as Record<string, unknown>;
    expect(area["@type"]).toBe("Country");
    expect(area.name).toBe("سوريا");
  });

  it("builds FAQPage with questions and answers", () => {
    const faqs = [
      { question: "ما هي ديارك؟", answer: "منصة عقارية سورية." },
      { question: "هل النشر مجاني؟", answer: "نعم." },
    ];
    const data = faqStructuredData(faqs);
    expect(data["@type"]).toBe("FAQPage");
    const entities = data.mainEntity as Array<{ "@type": string; name: string; acceptedAnswer: { text: string } }>;
    expect(entities).toHaveLength(2);
    expect(entities[0]["@type"]).toBe("Question");
    expect(entities[0].name).toBe("ما هي ديارك؟");
    expect(entities[0].acceptedAnswer.text).toBe("منصة عقارية سورية.");
    expect(entities[1].acceptedAnswer["@type"]).toBe("Answer");
  });
});
