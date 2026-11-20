import { describe, expect, it } from "vitest";
import { needsRenewal, renewBeforeExpiryMs, tokenExpiryMs } from "@/lib/sessionCookies";

function token(exp: number): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString("base64url");
  return `${encode({ alg: "HS256" })}.${encode({ sub: "u", exp })}.sig`;
}

describe("session cookies", () => {
  const now = Date.UTC(2026, 10, 20, 12, 0, 0);

  it("reads the exp claim from base64url payloads", () => {
    expect(tokenExpiryMs(token(now / 1000 + 600))).toBe(now + 600_000);
    expect(tokenExpiryMs("garbage")).toBeNull();
    expect(tokenExpiryMs("a.%%%.c")).toBeNull();
  });

  it("renews when the token is missing, unreadable or close to expiry", () => {
    expect(needsRenewal(undefined, now)).toBe(true);
    expect(needsRenewal("garbage", now)).toBe(true);
    expect(needsRenewal(token((now + renewBeforeExpiryMs - 1000) / 1000), now)).toBe(true);
    expect(needsRenewal(token((now + 10 * 60 * 1000) / 1000), now)).toBe(false);
  });
});
