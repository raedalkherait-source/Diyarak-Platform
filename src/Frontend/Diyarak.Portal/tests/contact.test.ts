import { describe, expect, it } from "vitest";
import { isListingId, nearestForwardedFor, whatsAppLink, displayPhone } from "@/lib/contact";

describe("isListingId", () => {
  it("accepts a valid UUID", () => {
    expect(isListingId("3faf53de-b49c-477c-8e5b-bf80760c45d5")).toBe(true);
  });

  it("accepts uppercase hex", () => {
    expect(isListingId("3FAF53DE-B49C-477C-8E5B-BF80760C45D5")).toBe(true);
  });

  it("rejects empty string", () => {
    expect(isListingId("")).toBe(false);
  });

  it("rejects a short string", () => {
    expect(isListingId("3faf53de-b49c")).toBe(false);
  });

  it("rejects non-hex characters", () => {
    expect(isListingId("zfaf53de-b49c-477c-8e5b-bf80760c45d5")).toBe(false);
  });
});

describe("nearestForwardedFor", () => {
  it("returns the last entry from a multi-entry header", () => {
    expect(nearestForwardedFor("1.2.3.4, 10.0.0.1, 192.168.1.1")).toBe("192.168.1.1");
  });

  it("returns the only entry for a single-value header", () => {
    expect(nearestForwardedFor("203.0.113.50")).toBe("203.0.113.50");
  });

  it("handles IPv6 addresses", () => {
    expect(nearestForwardedFor("1.2.3.4, ::1")).toBe("::1");
  });

  it("returns null for null header", () => {
    expect(nearestForwardedFor(null)).toBeNull();
  });

  it("returns null for empty header", () => {
    expect(nearestForwardedFor("")).toBeNull();
  });

  it("rejects entries with invalid characters", () => {
    expect(nearestForwardedFor("1.2.3.4, not-an-ip!")).toBeNull();
  });
});

describe("whatsAppLink", () => {
  it("builds a wa.me link with encoded Arabic text", () => {
    const result = whatsAppLink("https://wa.me/963912345678", "شقة في دمشق", "https://diyarak.app/listings/abc");
    expect(result).toContain("https://wa.me/963912345678?text=");
    expect(result).toContain(encodeURIComponent("شقة في دمشق"));
    expect(result).toContain(encodeURIComponent("https://diyarak.app/listings/abc"));
  });

  it("throws for non-wa.me URLs", () => {
    expect(() => whatsAppLink("https://evil.com/963912345678", "test", "url")).toThrow();
  });

  it("throws for non-https URLs", () => {
    expect(() => whatsAppLink("http://wa.me/963912345678", "test", "url")).toThrow();
  });
});

describe("displayPhone", () => {
  it("formats a Syrian E.164 number with spaces", () => {
    expect(displayPhone("+963912345678")).toBe("+963 912 345 678");
  });

  it("returns non-Syrian numbers unchanged", () => {
    expect(displayPhone("+1234567890")).toBe("+1234567890");
  });

  it("returns short numbers unchanged", () => {
    expect(displayPhone("+96391234")).toBe("+96391234");
  });
});
