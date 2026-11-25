import { describe, expect, it } from "vitest";
import { convertPrice } from "@/lib/currency";

describe("convertPrice", () => {
  const rate = 14500;

  it("converts USD to SYP", () => {
    const result = convertPrice(100, "USD", "SYP", rate);
    expect(result).toEqual({ amount: 1450000, currency: "SYP" });
  });

  it("converts SYP to USD with two decimal places", () => {
    const result = convertPrice(1450000, "SYP", "USD", rate);
    expect(result).toEqual({ amount: 100, currency: "USD" });
  });

  it("rounds SYP to USD correctly", () => {
    const result = convertPrice(100000, "SYP", "USD", rate);
    expect(result).toEqual({ amount: 6.9, currency: "USD" });
  });

  it("returns same amount when currencies match", () => {
    const result = convertPrice(500, "USD", "USD", rate);
    expect(result).toEqual({ amount: 500, currency: "USD" });
  });

  it("returns same amount when fromCurrency is null", () => {
    const result = convertPrice(500, null, "USD", rate);
    expect(result).toEqual({ amount: 500, currency: "USD" });
  });

  it("handles null amount", () => {
    const result = convertPrice(null, "USD", "SYP", rate);
    expect(result).toEqual({ amount: null, currency: "SYP" });
  });

  it("passes through unknown currency pairs unchanged", () => {
    const result = convertPrice(100, "EUR", "USD", rate);
    expect(result).toEqual({ amount: 100, currency: "EUR" });
  });
});
