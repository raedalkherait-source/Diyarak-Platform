import { describe, expect, it } from "vitest";
import { firstValue, parseBbox, formatBbox, toApiQuery, pageHref, savedSearchHref } from "@/lib/search";

describe("firstValue", () => {
  it("returns the string when given a string", () => {
    expect(firstValue("hello")).toBe("hello");
  });

  it("returns the first element of an array", () => {
    expect(firstValue(["a", "b"])).toBe("a");
  });

  it("returns empty string for undefined", () => {
    expect(firstValue(undefined)).toBe("");
  });
});

describe("parseBbox", () => {
  it("parses a valid bbox", () => {
    expect(parseBbox("33.5,36.2,33.6,36.3")).toEqual({ south: 33.5, west: 36.2, north: 33.6, east: 36.3 });
  });

  it("rejects too few parts", () => {
    expect(parseBbox("33.5,36.2")).toBeNull();
  });

  it("rejects non-numeric parts", () => {
    expect(parseBbox("abc,36.2,33.6,36.3")).toBeNull();
  });

  it("rejects south >= north", () => {
    expect(parseBbox("34,36,33,37")).toBeNull();
  });

  it("rejects oversized boxes (>8 degrees)", () => {
    expect(parseBbox("30,36,39,37")).toBeNull();
  });

  it("rejects out-of-range coordinates", () => {
    expect(parseBbox("-91,0,0,1")).toBeNull();
    expect(parseBbox("0,0,91,1")).toBeNull();
  });
});

describe("formatBbox", () => {
  it("rounds to 4 decimal places", () => {
    expect(formatBbox({ south: 33.12345, west: 36.6789, north: 33.2, east: 36.8 })).toBe("33.1235,36.6789,33.2,36.8");
  });
});

describe("toApiQuery", () => {
  it("drops unknown keys and sets defaults", () => {
    const q = toApiQuery({ governorate: "Damascus", unknown: "x" });
    expect(q.get("governorate")).toBe("Damascus");
    expect(q.has("unknown")).toBe(false);
    expect(q.get("page")).toBe("1");
    expect(q.get("pageSize")).toBe("12");
  });

  it("adds priceCurrency when price bounds are present", () => {
    const q = toApiQuery({ priceMin: "100" });
    expect(q.get("priceCurrency")).toBe("USD");
  });

  it("drops priceCurrency when no price bounds", () => {
    const q = toApiQuery({ priceCurrency: "SYP" });
    expect(q.has("priceCurrency")).toBe(false);
  });

  it("rejects non-numeric values for numeric keys", () => {
    const q = toApiQuery({ priceMin: "abc", page: "2" });
    expect(q.has("priceMin")).toBe(false);
    expect(q.get("page")).toBe("2");
  });
});

describe("pageHref", () => {
  it("builds a search URL for a given page", () => {
    const href = pageHref({ governorate: "Damascus" }, 3);
    expect(href).toContain("/search?");
    expect(href).toContain("page=3");
    expect(href).toContain("governorate=Damascus");
    expect(href).not.toContain("pageSize");
  });
});

describe("savedSearchHref", () => {
  it("strips livingAreaUnit from the query", () => {
    expect(savedSearchHref("governorate=Damascus&livingAreaUnit=SquareMeter")).toBe("/search?governorate=Damascus");
  });

  it("returns /search for empty query", () => {
    expect(savedSearchHref("")).toBe("/search");
  });
});
