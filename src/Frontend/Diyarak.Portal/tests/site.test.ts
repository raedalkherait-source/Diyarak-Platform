import { describe, expect, it } from "vitest";
import { jsonForScript } from "@/lib/site";

describe("jsonForScript", () => {
  it("escapes angle brackets so embedded JSON cannot break out of a script tag", () => {
    const result = jsonForScript({ text: "</script><img onerror=alert(1)>" });
    expect(result).not.toContain("<");
    expect(result).not.toContain(">");
    expect(result).toContain("\\u003c");
    expect(result).toContain("\\u003e");
    expect(JSON.parse(result).text).toBe("</script><img onerror=alert(1)>");
  });

  it("escapes ampersands", () => {
    const result = jsonForScript({ q: "a&b" });
    expect(result).not.toContain("&");
    expect(JSON.parse(result).q).toBe("a&b");
  });

  it("escapes line and paragraph separators", () => {
    const result = jsonForScript({ t: "a b c" });
    expect(result).not.toContain(" ");
    expect(result).not.toContain(" ");
    expect(JSON.parse(result).t).toBe("a b c");
  });

  it("round-trips nested objects", () => {
    const original = { a: [1, 2], b: { c: "d<e>" } };
    expect(JSON.parse(jsonForScript(original))).toEqual(original);
  });
});
