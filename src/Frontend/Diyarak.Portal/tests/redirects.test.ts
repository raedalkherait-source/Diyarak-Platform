import { describe, expect, it } from "vitest";
import { safeNextPath } from "@/lib/redirects";

describe("safeNextPath", () => {
  it("accepts a valid relative path", () => {
    expect(safeNextPath("/my/listings")).toBe("/my/listings");
  });
  it("rejects null", () => {
    expect(safeNextPath(null)).toBe("/my");
  });
  it("rejects undefined", () => {
    expect(safeNextPath(undefined)).toBe("/my");
  });
  it("rejects empty string", () => {
    expect(safeNextPath("")).toBe("/my");
  });
  it("rejects protocol-relative URLs (open redirect)", () => {
    expect(safeNextPath("//evil.com")).toBe("/my");
  });
  it("rejects absolute URLs", () => {
    expect(safeNextPath("https://evil.com")).toBe("/my");
  });
  it("rejects backslash paths", () => {
    expect(safeNextPath("/foo\\bar")).toBe("/my");
  });
  it("uses custom fallback", () => {
    expect(safeNextPath(null, "/home")).toBe("/home");
  });
});
