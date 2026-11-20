import type { Metadata } from "next";
import { ListingForm } from "@/components/ListingForm";

export const metadata: Metadata = { title: "إعلان جديد", robots: { index: false } };

export default function NewListingPage() {
  return (
    <div className="container section narrow">
      <h1>إعلان جديد</h1>
      <p className="notice">املأ البيانات، ثم أضف الصور وانشر الإعلان في الخطوة التالية. الحقول المعلّمة بـ * إلزامية.</p>
      <ListingForm />
    </div>
  );
}
