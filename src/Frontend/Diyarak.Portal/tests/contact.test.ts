import { describe, expect, it } from "vitest";
import { displayPhone, isListingId, nearestForwardedFor, whatsAppLink } from "@/lib/contact";

describe("contact helpers", () => {
  it("uses the right-most forwarded address and rejects junk", () => {
    expect(nearestForwardedFor("1.2.3.4, 10.0.0.9")).toBe("10.0.0.9");
    expect(nearestForwardedFor("2001:db8::1")).toBe("2001:db8::1");
    expect(nearestForwardedFor("evil<script>")).toBeNull();
    expect(nearestForwardedFor(null)).toBeNull();
    expect(nearestForwardedFor(" , ")).toBeNull();
  });

  it("builds a WhatsApp link with a pre-filled Arabic message", () => {
    const link = new URL(whatsAppLink("https://wa.me/963912345678", "شقة في المزة", "https://diyarak.sy/listings/1"));
    expect(link.hostname).toBe("wa.me");
    expect(link.pathname).toBe("/963912345678");
    expect(link.searchParams.get("text")).toBe("مرحباً، أنا مهتم بإعلانك على ديارك: شقة في المزة\nhttps://diyarak.sy/listings/1");
    expect(link.search).not.toContain("+");
    expect(link.search).toContain("%20");
  });

  it("refuses non-WhatsApp URLs", () => {
    expect(() => whatsAppLink("https://evil.example/963", "x", "y")).toThrow();
    expect(() => whatsAppLink("http://wa.me/963", "x", "y")).toThrow();
  });

  it("validates listing ids and formats Syrian numbers", () => {
    expect(isListingId("3faf53de-b49c-477c-8e5b-bf80760c45d5")).toBe(true);
    expect(isListingId("../../etc")).toBe(false);
    expect(displayPhone("+963912345678")).toBe("+963 912 345 678");
    expect(displayPhone("+4915112345678")).toBe("+4915112345678");
  });
});
