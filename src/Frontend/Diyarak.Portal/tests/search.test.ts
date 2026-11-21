import { describe, expect, it } from "vitest";
import { pageHref, parseBbox, savedSearchHref, savedSearchQuery, searchWithArea, toApiQuery } from "@/lib/search";

describe("toApiQuery", () => {
  it("forwards known filters, keeps repeatable values and adds paging", () => {
    const query = toApiQuery({
      transactionIntent: "Sell",
      governorate: ["Damascus", "RifDimashq"],
      district: "  المزة  ",
      unknown: "dropped",
      page: "2",
    });

    expect(query.getAll("governorate")).toEqual(["Damascus", "RifDimashq"]);
    expect(query.get("district")).toBe("المزة");
    expect(query.has("unknown")).toBe(false);
    expect(query.get("page")).toBe("2");
    expect(query.get("pageSize")).toBe("12");
  });

  it("drops empty and non-numeric values and keeps single-valued keys single", () => {
    const query = toApiQuery({ governorate: "", priceMin: "abc", district: ["a", "b"], page: "-3" });

    expect(query.has("governorate")).toBe(false);
    expect(query.has("priceMin")).toBe(false);
    expect(query.getAll("district")).toEqual(["a"]);
    expect(query.get("page")).toBe("1");
  });

  it("only sends a currency together with a price bound", () => {
    expect(toApiQuery({ priceCurrency: "SYP" }).has("priceCurrency")).toBe(false);
    expect(toApiQuery({ priceMax: "1000" }).get("priceCurrency")).toBe("USD");
    expect(toApiQuery({ priceMax: "1000", priceCurrency: "SYP" }).get("priceCurrency")).toBe("SYP");
  });

  it("sends square metres as unit for area bounds", () => {
    expect(toApiQuery({ livingAreaMin: "80" }).get("livingAreaUnit")).toBe("SquareMeter");
    expect(toApiQuery({}).has("livingAreaUnit")).toBe(false);
  });

  it("rejects overly long values", () => {
    expect(toApiQuery({ district: "x".repeat(101) }).has("district")).toBe(false);
  });
});

describe("pageHref", () => {
  it("keeps filters and replaces the page", () => {
    const href = pageHref({ governorate: "Aleppo", page: "1" }, 3);
    expect(href.startsWith("/search?")).toBe(true);
    const query = new URLSearchParams(href.split("?")[1]);
    expect(query.get("governorate")).toBe("Aleppo");
    expect(query.get("page")).toBe("3");
    expect(query.has("pageSize")).toBe(false);
  });
});

describe("saved search helpers", () => {
  it("stores filters without paging and reopens them as a portal URL", () => {
    const query = savedSearchQuery({ governorate: "Damascus", livingAreaMin: "100", page: "3", utm: "x" });
    expect(query).toBe("governorate=Damascus&livingAreaMin=100&livingAreaUnit=SquareMeter");
    expect(savedSearchHref(query)).toBe("/search?governorate=Damascus&livingAreaMin=100");
  });

  it("opens an unfiltered search at /search", () => {
    expect(savedSearchHref(savedSearchQuery({}))).toBe("/search");
  });
});

describe("map area", () => {
  it("parses valid boxes and rejects malformed or oversized ones", () => {
    expect(parseBbox("33.45,36.2,33.55,36.35")).toEqual({ south: 33.45, west: 36.2, north: 33.55, east: 36.35 });
    expect(parseBbox("33.55,36.2,33.45,36.35")).toBeNull(); // south above north
    expect(parseBbox("30,30,40,40")).toBeNull(); // more than 8 degrees
    expect(parseBbox("33,36,34")).toBeNull();
    expect(parseBbox("1e1,36,34,37")).toBeNull();
  });

  it("forwards only a valid bbox to the API", () => {
    expect(toApiQuery({ bbox: "33.45,36.2,33.55,36.35" }).get("bbox")).toBe("33.45,36.2,33.55,36.35");
    expect(toApiQuery({ bbox: "nonsense" }).has("bbox")).toBe(false);
  });

  it("replaces the area, resets paging and can remove it", () => {
    const query = "governorate=Damascus&page=3&bbox=1,1,2,2";
    expect(searchWithArea(query, { south: 33.451234, west: 36.2, north: 33.55, east: 36.35 })).toBe(
      "/search?governorate=Damascus&bbox=33.4512%2C36.2%2C33.55%2C36.35",
    );
    expect(searchWithArea(query, null)).toBe("/search?governorate=Damascus");
  });
});
