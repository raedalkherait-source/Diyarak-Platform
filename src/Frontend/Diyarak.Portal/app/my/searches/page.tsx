import type { Metadata } from "next";
import Link from "next/link";
import { deleteSavedSearch, openSavedSearch, setSavedSearchAlerts } from "@/app/my/actions";
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
  alertsEnabled: boolean;
}

const notices: Record<string, string> = {
  "alerts-on": "فُعّل التنبيه. ستصلك رسالة SMS واحدة في اليوم على الأكثر عند وجود إعلانات جديدة.",
  "alerts-off": "أُوقف التنبيه.",
};

type Props = { searchParams: Promise<{ notice?: string }> };

const dateFormat = new Intl.DateTimeFormat("ar-SY-u-nu-latn", { dateStyle: "medium", timeZone: "Asia/Damascus" });

export default async function SavedSearchesPage({ searchParams }: Props) {
  const { notice } = await searchParams;
  const result = await managementFetch<{ items: SavedSearch[] }>("/api/market/saved-searches");
  const items = result.ok ? result.value.items : [];

  return (
    <div className="container section narrow">
      <div className="section-title">
        <h1>عمليات البحث المحفوظة</h1>
        <Link className="button secondary" href="/my">إعلاناتي</Link>
      </div>
      {notice && notices[notice] ? <p className="success" role="status">{notices[notice]}</p> : null}
      {!result.ok ? (
        <div className="empty">تعذر تحميل عمليات البحث. حاول مجدداً.</div>
      ) : items.length === 0 ? (
        <div className="empty">
          <p>لم تحفظ أي بحث بعد. ابحث عن العقار الذي تريده ثم اضغط «احفظ هذا البحث» لتعرف هنا بالإعلانات الجديدة.</p>
          <Link className="button" href="/search">ابدأ البحث</Link>
        </div>
      ) : (
        <>
        <p className="notice">يمكنك تفعيل تنبيه برسالة SMS لكل بحث: رسالة واحدة يومياً على الأكثر تخبرك بعدد الإعلانات الجديدة.</p>
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
                  <form action={setSavedSearchAlerts.bind(null, search.searchId, !search.alertsEnabled)}>
                    <button className="button secondary" type="submit" aria-pressed={search.alertsEnabled}>
                      {search.alertsEnabled ? "🔔 التنبيه مفعّل — إيقاف" : "🔕 تفعيل تنبيه SMS"}
                    </button>
                  </form>
                  <form action={deleteSavedSearch.bind(null, search.searchId)}>
                    <button className="button secondary danger" type="submit" aria-label={`حذف ${search.name}`}>حذف</button>
                  </form>
                </div>
              </li>
            );
          })}
        </ul>
        </>
      )}
    </div>
  );
}
