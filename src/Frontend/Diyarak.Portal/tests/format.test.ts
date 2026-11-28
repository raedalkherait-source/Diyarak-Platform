import { describe, it, expect } from "vitest";
import { formatNumber, formatPrice, formatArea, formatFloor, formatLocation, photoSrc } from "../lib/format";

describe("formatNumber", () => {
  it("formats integers with group separators", () => {
    expect(formatNumber(1000)).toBe("1,000");
  });

  it("formats decimals up to 2 places", () => {
    expect(formatNumber(1234.567)).toBe("1,234.57");
  });

  it("formats zero", () => {
    expect(formatNumber(0)).toBe("0");
  });

  it("formats negative numbers", () => {
    expect(formatNumber(-500)).toContain("500");
  });
});

describe("formatPrice", () => {
  it("shows on-request label when isOnRequest is true", () => {
    expect(formatPrice({ isOnRequest: true, amount: null, currency: null })).toBe("السعر عند الاتصال");
  });

  it("shows on-request label when amount is null", () => {
    expect(formatPrice({ isOnRequest: false, amount: null, currency: "USD" })).toBe("السعر عند الاتصال");
  });

  it("formats USD price", () => {
    expect(formatPrice({ isOnRequest: false, amount: 50000, currency: "USD" })).toBe("50,000 $");
  });

  it("formats SYP price", () => {
    expect(formatPrice({ isOnRequest: false, amount: 1000000, currency: "SYP" })).toBe("1,000,000 ل.س");
  });

  it("formats EUR price", () => {
    expect(formatPrice({ isOnRequest: false, amount: 200000, currency: "EUR" })).toBe("200,000 €");
  });

  it("falls back to currency code for unknown currency", () => {
    expect(formatPrice({ isOnRequest: false, amount: 100, currency: "GBP" })).toBe("100 GBP");
  });

  it("omits symbol when currency is null", () => {
    expect(formatPrice({ isOnRequest: false, amount: 999, currency: null })).toBe("999");
  });
});

describe("formatArea", () => {
  it("returns empty string for null area", () => {
    expect(formatArea(null)).toBe("");
  });

  it("formats area with SquareMeter unit", () => {
    const result = formatArea({ value: 150, unit: "SquareMeter" });
    expect(result).toContain("150");
    expect(result).toContain("م²");
  });

  it("formats area with Dunam unit", () => {
    const result = formatArea({ value: 3, unit: "Dunam" });
    expect(result).toContain("3");
  });
});

describe("formatFloor", () => {
  it("returns empty string for null floor", () => {
    expect(formatFloor(null, null)).toBe("");
  });

  it("formats ground floor", () => {
    expect(formatFloor(0, null)).toBe("الطابق الأرضي");
  });

  it("formats positive floor", () => {
    expect(formatFloor(3, null)).toContain("الطابق");
    expect(formatFloor(3, null)).toContain("3");
  });

  it("formats basement floor", () => {
    const result = formatFloor(-1, null);
    expect(result).toContain("القبو");
    expect(result).toContain("1");
  });

  it("includes building total when provided", () => {
    const result = formatFloor(2, 10);
    expect(result).toContain("من");
    expect(result).toContain("10");
  });

  it("omits building total when null", () => {
    expect(formatFloor(5, null)).not.toContain("من");
  });
});

describe("formatLocation", () => {
  it("joins district and city", () => {
    expect(formatLocation({ governorate: "Damascus", city: "دمشق", district: "المزة" })).toBe("المزة، دمشق");
  });

  it("returns city only when no district", () => {
    expect(formatLocation({ governorate: "Aleppo", city: "حلب", district: null })).toBe("حلب");
  });

  it("returns district only when no city", () => {
    expect(formatLocation({ governorate: "Homs", city: null, district: "الوعر" })).toBe("الوعر");
  });

  it("returns empty string when both null", () => {
    expect(formatLocation({ governorate: "Latakia", city: null, district: null })).toBe("");
  });
});

describe("photoSrc", () => {
  it("builds media path", () => {
    expect(photoSrc("abc-123", "photo-1")).toBe("/media/listings/abc-123/photos/photo-1");
  });

  it("encodes special characters", () => {
    expect(photoSrc("a/b", "c/d")).toBe("/media/listings/a%2Fb/photos/c%2Fd");
  });
});
