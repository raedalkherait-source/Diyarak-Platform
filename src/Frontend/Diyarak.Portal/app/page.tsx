import Link from "next/link";
import { ListingCard } from "@/components/ListingCard";
import { Options } from "@/components/Options";
import { SearchHistory } from "@/components/SearchHistory";
import { ApiUnavailableError, searchListings } from "@/lib/api";
import { categoryLabels, governorateLabels } from "@/lib/labels";
import { toApiQuery } from "@/lib/search";
import { organizationStructuredData, websiteStructuredData } from "@/lib/seo";
import { jsonForScript } from "@/lib/site";
import type { PublicListing } from "@/lib/types";

import type { Metadata } from "next";

export const metadata: Metadata = { alternates: { canonical: "/" } };
export const dynamic = "force-dynamic";

async function latestListings(): Promise<PublicListing[] | null> {
  try {
    return (await searchListings(toApiQuery({}, 8)))?.items ?? [];
  } catch (error) {
    if (error instanceof ApiUnavailableError) return null;
    throw error;
  }
}

async function featuredListings(): Promise<PublicListing[] | null> {
  try {
    const result = await searchListings(toApiQuery({ sort: "newest" }, 12));
    return (result?.items ?? []).filter((l) => l.featured).slice(0, 4);
  } catch {
    return null;
  }
}

export default async function HomePage() {
  const [listings, featured] = await Promise.all([latestListings(), featuredListings()]);

  return (
    <>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: jsonForScript(websiteStructuredData()) }} />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: jsonForScript(organizationStructuredData()) }} />
      <section className="hero">
        <div className="container">
          <h1>ابحث عن بيتك في سوريا</h1>
          <p>شقق، بيوت، أراضٍ ومحلات للبيع والإيجار في جميع المحافظات.</p>
          <form className="search-card" action="/search" method="get" role="search" aria-label="البحث عن عقار">
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
              <input name="district" placeholder="مثال: المزة" maxLength={100} enterKeyHint="search" />
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

      {featured && featured.length > 0 ? (
        <section className="section">
          <div className="container">
            <div className="section-title">
              <h2>★ إعلانات مميزة</h2>
              <Link href="/search">عرض الكل ←</Link>
            </div>
            <div className="grid">
              {featured.map((listing) => (
                <ListingCard key={listing.listingId} listing={listing} priority />
              ))}
            </div>
          </div>
        </section>
      ) : null}

      <section className="section">
        <div className="container">
          <div className="stats-bar">
            <div className="stat-item">
              <span className="stat-number">14</span>
              <span className="stat-label">محافظة</span>
            </div>
            <div className="stat-item">
              <span className="stat-number">6</span>
              <span className="stat-label">أنواع عقارات</span>
            </div>
            <div className="stat-item">
              <span className="stat-number">3</span>
              <span className="stat-label">أدوات مجانية</span>
            </div>
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
