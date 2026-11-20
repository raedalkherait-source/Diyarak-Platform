/** Arabic messages for API problem codes shown in the publisher dashboard. */
const messages: Record<string, string> = {
  "identity.phone.invalid_phone_number": "رقم الهاتف غير صالح.",
  "identity.phone.invalid_code_format": "الرمز يجب أن يتكون من 6 أرقام.",
  "identity.phone.too_many_requests": "طلبت رموزاً كثيرة. انتظر 15 دقيقة ثم حاول مجدداً.",
  "identity.phone.verification_failed": "الرمز غير صحيح أو منتهي الصلاحية. اطلب رمزاً جديداً.",
  "identity.phone.concurrent_modification": "حدث تعارض. حاول مرة أخرى.",
  "identity.phone.delivery_failed": "تعذر إرسال رسالة الرمز حالياً. حاول بعد دقائق.",
  "market.property.invalid_create_request": "بعض بيانات العقار غير صالحة. راجع الحقول.",
  "market.listing.invalid_patch": "بعض بيانات الإعلان غير صالحة.",
  "market.listing.concurrent_modification": "تغيّر الإعلان في مكان آخر. حدّث الصفحة وحاول مجدداً.",
  "market.listing.cannot_edit": "لا يمكن تعديل إعلان منتهٍ.",
  "market.listing_status.invalid_transition": "لا يمكن تنفيذ هذا التغيير على حالة الإعلان الحالية.",
  "market.listing_status.concurrent_modification": "تغيّر الإعلان في مكان آخر. حدّث الصفحة وحاول مجدداً.",
  "market.listing.cannot_publish": "أكمل بيانات الإعلان (نوع العرض، العنوان، السعر) قبل النشر.",
  "market.listing.contact_phone_not_verified": "يجب تسجيل الدخول برقم هاتف موثّق لتفعيل التواصل.",
  "market.listing_photo.too_large": "حجم الصورة أكبر من 10 ميغابايت.",
  "market.listing_photo.unsupported_format": "الصيغ المدعومة: JPEG و PNG و WebP.",
  "market.listing_photo.invalid_image": "الملف ليس صورة صالحة.",
  "market.listing_photo.limit_reached": "وصلت إلى الحد الأقصى (30 صورة).",
  "market.listing_photo.listing_not_editable": "لا يمكن تعديل صور إعلان منتهٍ.",
  "market.listing_photo.concurrent_modification": "حدث تعارض أثناء الرفع. حاول مجدداً.",
};

export function errorMessage(code: string | null | undefined, status?: number): string {
  if (code && messages[code]) return messages[code];
  if (status === 429) return "طلبات كثيرة. انتظر قليلاً ثم حاول مجدداً.";
  if (status === 404) return "العنصر غير موجود.";
  return "حدث خطأ غير متوقع. حاول مرة أخرى.";
}
