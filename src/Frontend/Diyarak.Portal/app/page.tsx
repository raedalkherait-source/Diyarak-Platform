import Link from "next/link";
import { ListingCard } from "@/components/ListingCard";
import { Options } from "@/components/Options";
import { SearchHistory } from "@/components/SearchHistory";
import { ApiUnavailableError, searchListings } from "@/lib/api";
import { categoryLabels, governorateLabels } from "@/lib/labels";
import { toApiQuery } from "@/lib/search";
import type { PublicListing } from "@/lib/types";

export const dynamic = "force-dynamic";

async function latestListings(): Promise<PublicListing[] | null> {
  try {
    return (await searchListings(toApiQuery({}, 8)))?.items ?? [];
  } catch (error) {
    if (error instanceof ApiUnavailableError) return null;
    throw error;
  }
}

export default async function HomePage() {
  const listings = await latestListings();

  return (
    <>
      <section className="hero">
        <div className="container">
          <h1>ابحث عن بيتك في سوريا</h1>
          <p>شقق، بيوت، أراضٍ ومحلات للبيع والإيجار في جميع المحافظات.</p>
          <form className="search-card" action="/search" method="get" role="search">
            <div className="intent-tabs" role="radiogroup" aria-label="نوع العرض">
              <label>
                <input type="radio" name="transactionIntent" value="Sell" defaultChecked />
                <span>شراء</span>
              </label>
              <label>
                <input type="radio" name="transactionIntent" value="Rent" />
                <span>إيجار</span>
              </label>
            </div>
            <label className="field">
              المحافظة
              <select name="governorate" defaultValue="">
                <Options labels={governorateLabels} placeholder="كل المحافظات" />
              </select>
            </label>
            <label className="field">
              الحي / المنطقة
              <input name="district" placeholder="مثال: المزة" maxLength={100} />
            </label>
            <label className="field">
              نوع العقار
              <select name="propertyCategory" defaultValue="">
                <Options labels={categoryLabels} placeholder="الكل" />
              </select>
            </label>
            <button className="button" type="submit">ابحث</button>
          </form>
          <SearchHistory />
        </div>
      </section>

      <section className="section">
        <div className="container">
          <div className="quick-links">
            <Link href="/contractors" className="quick-link-card">
              <strong>دليل المقاولين</strong>
              <span>ابحث عن مقاولين ومتعهدين في سوريا</span>
            </Link>
            <Link href="/projects" className="quick-link-card">
              <strong>مشاريع على المخطط</strong>
              <span>مشاريع قيد الإنشاء — احجز وحدتك</span>
            </Link>
            <Link href="/prices" className="quick-link-card">
              <strong>أسعار العقارات</strong>
              <span>متوسط الأسعار حسب المحافظة والمنطقة</span>
            </Link>
          </div>
        </div>
      </section>

      <section className="section">
        <div className="container">
          <div className="section-title">
            <h2>أحدث الإعلانات</h2>
            <Link href="/search">عرض الكل ←</Link>
          </div>
          {listings === null ? (
            <div className="empty">تعذر تحميل الإعلانات حالياً. حاول مرة أخرى بعد قليل.</div>
          ) : listings.length === 0 ? (
            <div className="empty">لا توجد إعلانات منشورة بعد.</div>
          ) : (
            <div className="grid">
              {listings.map((listing) => (
                <ListingCard key={listing.listingId} listing={listing} />
              ))}
            </div>
          )}
        </div>
      </section>
    </>
  );
}
