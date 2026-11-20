import { describe, expect, it } from "vitest";
import { pageHref, toApiQuery } from "@/lib/search";

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
