import { describe, expect, it } from "vitest";
import { errorMessage } from "@/lib/errors";

describe("errorMessage", () => {
  it("returns the mapped message for a known error code", () => {
    expect(errorMessage("identity.phone.invalid_phone_number")).toBe("رقم الهاتف غير صالح.");
    expect(errorMessage("market.listing_photo.too_large")).toBe("حجم الصورة أكبر من 10 ميغابايت.");
  });

  it("returns rate-limit message for 429 status", () => {
    expect(errorMessage(null, 429)).toBe("طلبات كثيرة. انتظر قليلاً ثم حاول مجدداً.");
    expect(errorMessage(undefined, 429)).toBe("طلبات كثيرة. انتظر قليلاً ثم حاول مجدداً.");
  });

  it("returns not-found message for 404 status", () => {
    expect(errorMessage(null, 404)).toBe("العنصر غير موجود.");
  });

  it("prefers a known code over status fallback", () => {
    expect(errorMessage("identity.phone.too_many_requests", 429)).toBe(
      "طلبت رموزاً كثيرة. انتظر 15 دقيقة ثم حاول مجدداً.",
    );
  });

  it("returns a generic fallback for unknown codes and statuses", () => {
    expect(errorMessage("unknown.code")).toBe("حدث خطأ غير متوقع. حاول مرة أخرى.");
    expect(errorMessage(null)).toBe("حدث خطأ غير متوقع. حاول مرة أخرى.");
    expect(errorMessage(null, 500)).toBe("حدث خطأ غير متوقع. حاول مرة أخرى.");
  });
});
