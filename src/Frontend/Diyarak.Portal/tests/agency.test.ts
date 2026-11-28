import { describe, expect, it } from "vitest";
import { isAgencyId, verificationStatusLabels } from "@/lib/agency";

describe("isAgencyId", () => {
  it("accepts a valid lowercase UUID", () => {
    expect(isAgencyId("3faf53de-b49c-477c-8e5b-bf80760c45d5")).toBe(true);
  });

  it("accepts uppercase UUID", () => {
    expect(isAgencyId("3FAF53DE-B49C-477C-8E5B-BF80760C45D5")).toBe(true);
  });

  it("rejects empty string", () => {
    expect(isAgencyId("")).toBe(false);
  });

  it("rejects non-UUID string", () => {
    expect(isAgencyId("not-a-uuid")).toBe(false);
  });
});

describe("verificationStatusLabels", () => {
  it("covers all four statuses", () => {
    expect(Object.keys(verificationStatusLabels)).toHaveLength(4);
    expect(verificationStatusLabels.None).toBeTruthy();
    expect(verificationStatusLabels.Pending).toBeTruthy();
    expect(verificationStatusLabels.Verified).toBeTruthy();
    expect(verificationStatusLabels.Rejected).toBeTruthy();
  });
});
