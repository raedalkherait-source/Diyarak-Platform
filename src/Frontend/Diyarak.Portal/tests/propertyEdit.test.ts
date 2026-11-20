import { describe, expect, it } from "vitest";
import { parsePropertyEditForm, propertyFormValues, type ManagementProperty } from "@/lib/listingForm";

function form(entries: Record<string, string | string[]>): FormData {
  const data = new FormData();
  for (const [key, value] of Object.entries(entries))
    for (const item of Array.isArray(value) ? value : [value]) data.append(key, item);
  return data;
}

const stored: ManagementProperty = {
  propertyId: "0b9e5a0e-7a43-4c43-9a55-5d3c1f0e8f11",
  version: 4,
  category: "Apartment",
  address: {
    street: "شارع المزة",
    houseNumber: "12",
    postalCode: null,
    city: "دمشق",
    location: { latitude: 33.5, longitude: 36.25 },
    governorate: "Damascus",
    district: "المزة",
  },
  livingArea: { value: 150, unit: "SquareMeter" },
  usableArea: { value: 140, unit: "SquareMeter" },
  totalRooms: 4,
  bedroomCount: 3,
  bathroomCount: 2,
  furnishingQuality: "Normal",
  features: ["Elevator", "UnlabelledFutureFeature"],
  constructionYear: 2005,
  lastModernizationYear: null,
  commercialSubtype: null,
  salesArea: null,
  totalArea: null,
  parkingSpaceCount: null,
  legalTitleType: "GreenTitleDeed",
  finishingLevel: "Deluxe",
  floorNumber: 2,
  buildingFloorCount: 5,
};

const edited = {
  governorate: "Damascus",
  city: "دمشق",
  district: "",
  livingArea: "160",
  totalRooms: "",
  bedroomCount: "3",
  bathroomCount: "2",
  floorNumber: "3",
  buildingFloorCount: "5",
  legalTitleType: "GreenTitleDeed",
  finishingLevel: "SuperDeluxe",
  features: ["SolarPowerSystem"],
};

describe("propertyFormValues", () => {
  it("prefills portal fields and hides features without a portal label", () => {
    const values = propertyFormValues(stored);
    expect(values.livingArea).toBe("150");
    expect(values.floorNumber).toBe("2");
    expect(values.district).toBe("المزة");
    expect(values.features).toEqual(["Elevator"]);
  });
});

describe("parsePropertyEditForm", () => {
  it("replaces portal fields and preserves everything the portal does not show", () => {
    const result = parsePropertyEditForm(form(edited), stored);
    expect(result.ok).toBe(true);
    if (!result.ok) return;

    expect(result.replacement).toEqual({
      version: 4,
      category: "Apartment",
      address: {
        street: "شارع المزة",
        houseNumber: "12",
        location: { latitude: 33.5, longitude: 36.25 },
        governorate: "Damascus",
        city: "دمشق",
      },
      livingArea: { value: 160, unit: "SquareMeter" },
      usableArea: { value: 140, unit: "SquareMeter" },
      bedroomCount: 3,
      bathroomCount: 2,
      furnishingQuality: "Normal",
      features: ["SolarPowerSystem", "UnlabelledFutureFeature"],
      constructionYear: 2005,
      legalTitleType: "GreenTitleDeed",
      finishingLevel: "SuperDeluxe",
      floorNumber: 3,
      buildingFloorCount: 5,
    });
  });

  it("keeps the stored category even if the form tries to change it", () => {
    const result = parsePropertyEditForm(form({ ...edited, category: "House" }), stored);
    expect(result.ok && result.replacement.category).toBe("Apartment");
  });

  it("omits features entirely when none remain", () => {
    const result = parsePropertyEditForm(form({ ...edited, features: [] }), { ...stored, features: ["Elevator"] });
    expect(result.ok && "features" in result.replacement).toBe(false);
  });

  it("reports validation errors without building a replacement", () => {
    const result = parsePropertyEditForm(form({ ...edited, city: "", floorNumber: "9" }), stored);
    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(Object.keys(result.errors).sort()).toEqual(["city", "floorNumber"]);
  });
});
