import type { Metadata } from "next";
import Link from "next/link";
import { deleteSavedSearch, openSavedSearch } from "@/app/my/actions";
import { managementFetch } from "@/lib/managementApi";
import { savedSearchHref } from "@/lib/search";

export const metadata: Metadata = { title: "عمليات البحث المحفوظة", robots: { index: false } };

interface SavedSearch {
  searchId: string;
  name: string;
  query: string;
  createdAt: string;
  lastSeenAt: string;
  newListingCount: number;
  newListingCountCapped: boolean;
}

const dateFormat = new Intl.DateTimeFormat("ar-SY", { dateStyle: "medium", timeZone: "Asia/Damascus" });

export default async function SavedSearchesPage() {
  const result = await managementFetch<{ items: SavedSearch[] }>("/api/market/saved-searches");
  const items = result.ok ? result.value.items : [];

  return (
    <div className="container section narrow">
      <div className="section-title">
        <h1>عمليات البحث المحفوظة</h1>
        <Link className="button secondary" href="/my">إعلاناتي</Link>
      </div>
      {!result.ok ? (
        <div className="empty">تعذر تحميل عمليات البحث. حاول مجدداً.</div>
      ) : items.length === 0 ? (
        <div className="empty">
          <p>لم تحفظ أي بحث بعد. ابحث عن العقار الذي تريده ثم اضغط «احفظ هذا البحث» لتعرف هنا بالإعلانات الجديدة.</p>
          <Link className="button" href="/search">ابدأ البحث</Link>
        </div>
      ) : (
        <ul className="saved-searches">
          {items.map((search) => {
            const href = savedSearchHref(search.query);
            return (
              <li key={search.searchId} className="panel saved-search">
                <div>
                  <h2>{search.name}</h2>
                  <p className="notice">آخر زيارة: {dateFormat.format(new Date(search.lastSeenAt))}</p>
                </div>
                {search.newListingCount > 0 ? (
                  <span className="new-count">
                    {search.newListingCountCapped ? "+99" : search.newListingCount} جديد
                  </span>
                ) : (
                  <span className="notice">لا جديد</span>
                )}
                <div className="toolbar">
                  <form action={openSavedSearch.bind(null, search.searchId, href)}>
                    <button className="button" type="submit">عرض النتائج</button>
                  </form>
                  <form action={deleteSavedSearch.bind(null, search.searchId)}>
                    <button className="button secondary danger" type="submit" aria-label={`حذف ${search.name}`}>حذف</button>
                  </form>
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
