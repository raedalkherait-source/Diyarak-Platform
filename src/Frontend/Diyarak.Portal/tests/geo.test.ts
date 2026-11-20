import { describe, expect, it } from "vitest";
import { insideSyria, parseCoordinates } from "@/lib/geo";

describe("parseCoordinates", () => {
  it("treats two empty fields as no location", () => {
    expect(parseCoordinates("", "")).toBeNull();
  });

  it("accepts points inside Syria and rounds to 6 decimals", () => {
    expect(parseCoordinates("33.51234567", "36.27654321")).toEqual({ latitude: 33.512346, longitude: 36.276543 });
  });

  it.each([
    ["33.5", ""],
    ["", "36.2"],
    ["abc", "36.2"],
    ["33.5;", "36.2"],
    ["1e2", "36.2"],
    ["48.85", "2.35"],
    ["31.9", "35.9"],
  ])("rejects %s, %s", (latitude, longitude) => {
    expect(parseCoordinates(latitude, longitude)).toBe("invalid");
  });
});

describe("insideSyria", () => {
  it("covers the governorate capitals at the edges", () => {
    expect(insideSyria({ latitude: 37.05, longitude: 42.15 })).toBe(true); // Al-Malikiyah area
    expect(insideSyria({ latitude: 32.62, longitude: 36.1 })).toBe(true); // Daraa
    expect(insideSyria({ latitude: 35.52, longitude: 35.79 })).toBe(true); // Latakia
  });
});
