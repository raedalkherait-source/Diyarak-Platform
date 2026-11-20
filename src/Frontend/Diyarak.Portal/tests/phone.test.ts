import { describe, expect, it } from "vitest";
import { normalizePhone } from "@/lib/phone";

describe("normalizePhone", () => {
  it.each([
    ["0912345678", "+963912345678"],
    ["912345678", "+963912345678"],
    ["0912 345 678", "+963912345678"],
    ["+963 912-345-678", "+963912345678"],
    ["00963912345678", "+963912345678"],
    ["963912345678", "+963912345678"],
    ["٠٩١٢٣٤٥٦٧٨", "+963912345678"],
    ["+4915112345678", "+4915112345678"],
    ["00905321234567", "+905321234567"],
  ])("normalises %s", (input, expected) => {
    expect(normalizePhone(input)).toBe(expected);
  });

  it.each(["", "12345", "0112345678", "+963112345678", "abc", "+0123456789", "09123456789"])("rejects %s", (input) => {
    expect(normalizePhone(input)).toBeNull();
  });
});
