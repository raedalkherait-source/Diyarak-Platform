import type { Metadata } from "next";
import Link from "next/link";
import { ListingCard } from "@/components/ListingCard";
import { managementFetch } from "@/lib/managementApi";
import type { PublicListingPage } from "@/lib/types";

export const metadata: Metadata = { title: "المفضلة", robots: { index: false } };

export default async function FavoritesPage() {
  const result = await managementFetch<PublicListingPage>("/api/market/favorites?pageSize=100");
  const items = result.ok ? result.value.items : [];

  return (
    <div className="container section">
      <div className="section-title">
        <h1>المفضلة</h1>
        <Link className="button secondary" href="/my">إعلاناتي</Link>
      </div>
      {!result.ok ? (
        <div className="empty">تعذر تحميل المفضلة. حاول مجدداً.</div>
      ) : items.length === 0 ? (
        <div className="empty">
          <p>لم تحفظ أي إعلان بعد. اضغط «احفظ الإعلان» في صفحة أي عقار يعجبك.</p>
          <Link className="button" href="/search">تصفح العقارات</Link>
        </div>
      ) : (
        <>
          <p className="notice">الإعلانات التي أوقفها أصحابها أو أنهوها لا تظهر هنا، وتعود إن أُعيد نشرها.</p>
          <div className="grid">
            {items.map((listing) => (
              <ListingCard key={listing.listingId} listing={listing} />
            ))}
          </div>
        </>
      )}
    </div>
  );
}
