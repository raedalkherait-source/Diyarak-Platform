import { describe, expect, it } from "vitest";
import { isLikelyBot, niceMax } from "@/lib/statistics";

describe("isLikelyBot", () => {
  it.each([
    [null, true],
    ["Mozilla/5.0 (compatible; Googlebot/2.1)", true],
    ["WhatsApp/2.23.20", true],
    ["facebookexternalhit/1.1", true],
    ["curl/8.5.0", true],
    ["Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 Mobile/15E148 Safari/604.1", false],
    ["Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 Chrome/130.0 Mobile Safari/537.36", false],
  ])("%s → %s", (agent, expected) => {
    expect(isLikelyBot(agent)).toBe(expected);
  });
});

describe("niceMax", () => {
  it("rounds up to 1, 2 or 5 times a power of ten and never below 4", () => {
    expect(niceMax([0, 0])).toBe(5);
    expect(niceMax([3])).toBe(5);
    expect(niceMax([7])).toBe(10);
    expect(niceMax([13])).toBe(20);
    expect(niceMax([480])).toBe(500);
  });
});
