import { describe, expect, it } from "vitest";
import { isListingId, nearestForwardedFor, whatsAppLink, displayPhone } from "@/lib/contact";

describe("isListingId", () => {
  it("accepts a valid UUID", () => {
    expect(isListingId("01234567-89ab-cdef-0123-456789abcdef")).toBe(true);
  });
  it("rejects short strings", () => {
    expect(isListingId("abc")).toBe(false);
  });
  it("rejects UUIDs without hyphens", () => {
    expect(isListingId("0123456789abcdef0123456789abcdef")).toBe(false);
  });
});

describe("nearestForwardedFor", () => {
  it("returns the last entry", () => {
    expect(nearestForwardedFor("1.2.3.4, 5.6.7.8")).toBe("5.6.7.8");
  });
  it("returns null for null header", () => {
    expect(nearestForwardedFor(null)).toBeNull();
  });
  it("returns null for empty header", () => {
    expect(nearestForwardedFor("")).toBeNull();
  });
  it("returns null for invalid IP format", () => {
    expect(nearestForwardedFor("not-an-ip")).toBeNull();
  });
  it("accepts IPv6 addresses", () => {
    expect(nearestForwardedFor("::1")).toBe("::1");
  });
});

describe("whatsAppLink", () => {
  it("builds a prefilled WhatsApp link", () => {
    const result = whatsAppLink("https://wa.me/963912345678", "شقة للبيع", "https://diyarak.app/listings/1");
    expect(result).toContain("https://wa.me/963912345678?text=");
    expect(result).toContain(encodeURIComponent("شقة للبيع"));
    expect(result).toContain(encodeURIComponent("https://diyarak.app/listings/1"));
  });
  it("throws for non-WhatsApp URLs", () => {
    expect(() => whatsAppLink("https://evil.com/123", "test", "https://x.com")).toThrow();
  });
  it("throws for non-HTTPS URLs", () => {
    expect(() => whatsAppLink("http://wa.me/123", "test", "https://x.com")).toThrow();
  });
});

describe("displayPhone", () => {
  it("formats a Syrian mobile number", () => {
    expect(displayPhone("+963912345678")).toBe("+963 912 345 678");
  });
  it("returns non-Syrian numbers as-is", () => {
    expect(displayPhone("+1234567890")).toBe("+1234567890");
  });
  it("returns short strings as-is", () => {
    expect(displayPhone("123")).toBe("123");
  });
});
