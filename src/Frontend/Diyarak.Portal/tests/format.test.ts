import { describe, expect, it } from "vitest";
import { formatArea, formatFloor, formatLocation, formatPrice, photoSrc } from "@/lib/format";

describe("format", () => {
  it("formats prices with Western digits and currency symbols", () => {
    expect(formatPrice({ isOnRequest: false, amount: 145000, currency: "USD" })).toBe("145,000 $");
    expect(formatPrice({ isOnRequest: false, amount: 350000000, currency: "SYP" })).toBe("350,000,000 ل.س");
    expect(formatPrice({ isOnRequest: true, amount: null, currency: null })).toBe("السعر عند الاتصال");
  });

  it("formats floors including ground floor and basements", () => {
    expect(formatFloor(0, null)).toBe("الطابق الأرضي");
    expect(formatFloor(-1, null)).toBe("القبو 1");
    expect(formatFloor(4, 8)).toBe("الطابق 4 من 8");
    expect(formatFloor(null, 8)).toBe("");
  });

  it("formats areas and locations", () => {
    expect(formatArea({ value: 160, unit: "SquareMeter" })).toBe("160 م²");
    expect(formatArea(null)).toBe("");
    expect(formatLocation({ city: "دمشق", district: "المزة", governorate: "Damascus", postalCode: null })).toBe("المزة، دمشق");
    expect(formatLocation({ city: "حلب", district: null, governorate: null, postalCode: null })).toBe("حلب");
  });

  it("builds same-origin photo paths with encoded segments", () => {
    expect(photoSrc("a/b", "c")).toBe("/media/listings/a%2Fb/photos/c");
  });
});
