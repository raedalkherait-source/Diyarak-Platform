import { describe, expect, it } from "vitest";
import { isAgencyId, verificationStatusLabels } from "@/lib/agency";

describe("isAgencyId", () => {
  it("accepts a valid lowercase UUID", () => {
    expect(isAgencyId("01234567-89ab-cdef-0123-456789abcdef")).toBe(true);
  });
  it("accepts uppercase UUID", () => {
    expect(isAgencyId("01234567-89AB-CDEF-0123-456789ABCDEF")).toBe(true);
  });
  it("rejects short strings", () => {
    expect(isAgencyId("abc")).toBe(false);
  });
  it("rejects UUIDs without hyphens", () => {
    expect(isAgencyId("0123456789abcdef0123456789abcdef")).toBe(false);
  });
  it("rejects strings with invalid characters", () => {
    expect(isAgencyId("0123456g-89ab-cdef-0123-456789abcdef")).toBe(false);
  });
});

describe("verificationStatusLabels", () => {
  it("has labels for all statuses", () => {
    expect(verificationStatusLabels.None).toBeTruthy();
    expect(verificationStatusLabels.Pending).toBeTruthy();
    expect(verificationStatusLabels.Verified).toBeTruthy();
    expect(verificationStatusLabels.Rejected).toBeTruthy();
  });
});
