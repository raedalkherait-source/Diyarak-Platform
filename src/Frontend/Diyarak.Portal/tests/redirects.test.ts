import { describe, expect, it } from "vitest";
import { safeNextPath } from "@/lib/redirects";

describe("safeNextPath", () => {
  it("accepts a simple relative path", () => {
    expect(safeNextPath("/my/listings")).toBe("/my/listings");
  });

  it("falls back for null, undefined and empty string", () => {
    expect(safeNextPath(null)).toBe("/my");
    expect(safeNextPath(undefined)).toBe("/my");
    expect(safeNextPath("")).toBe("/my");
  });

  it("rejects protocol-relative URLs (open redirect)", () => {
    expect(safeNextPath("//evil.com")).toBe("/my");
  });

  it("rejects paths with backslashes (open redirect on some browsers)", () => {
    expect(safeNextPath("/\\evil.com")).toBe("/my");
  });

  it("rejects absolute URLs", () => {
    expect(safeNextPath("https://evil.com")).toBe("/my");
    expect(safeNextPath("http://evil.com/my")).toBe("/my");
  });

  it("uses a custom fallback", () => {
    expect(safeNextPath(null, "/")).toBe("/");
    expect(safeNextPath("//evil.com", "/search")).toBe("/search");
  });
});
