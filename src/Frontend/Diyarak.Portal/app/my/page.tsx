import type { Metadata } from "next";
import Link from "next/link";
import { logout, logoutEverywhere } from "./actions";
import { formatPrice } from "@/lib/format";
import { label, transactionIntentLabels } from "@/lib/labels";
import { managementFetch, type ManagementListing } from "@/lib/managementApi";
import { closureReasonLabels, listingStatusLabels } from "@/lib/status";
import { NotificationBell, type Notification } from "@/components/NotificationCenter";

export const metadata: Metadata = { title: "إعلاناتي", robots: { index: false } };

type Props = { searchParams: Promise<{ notice?: string }> };

type QuotaInfo = { quota: number; activeListings: number; remaining: number; activePackage: { packageId: string; listingQuota: number; expiresAt: string } | null };

export default async function MyListingsPage({ searchParams }: Props) {
  const { notice } = await searchParams;
  const result = await managementFetch<{ items: ManagementListing[]; hasMore: boolean }>("/api/market/listings?pageSize=100");
  const listings = result.ok ? result.value.items : [];
  const totals = await managementFetch<{ items: { listingId: string; views: number; contactReveals: number }[] }>(
    "/api/market/listings/statistics?days=30",
  );
  const totalsById = new Map((totals.ok ? totals.value.items : []).map((item) => [item.listingId, item]));
  const quotaResult = await managementFetch<QuotaInfo>("/api/market/quota");
  const quota = quotaResult.ok ? quotaResult.value : null;

  const savedSearches = await managementFetch<{ items: Array<{ savedSearchId: string; name: string; newCount: number }> }>(
    "/api/market/saved-searches",
  );
  const notifications: Notification[] = [];
  if (savedSearches.ok) {
    for (const s of savedSearches.value.items) {
      if (s.newCount > 0) {
        notifications.push({
          id: `ss-${s.savedSearchId}`,
          type: "saved-search",
          title: s.name,
          message: `${s.newCount} إعلان جديد`,
          href: "/my/searches",
          time: new Date().toISOString(),
        });
      }
    }
  }

  return (
    <div className="container section">
      <div className="section-title">
        <h1>إعلاناتي</h1>
        <NotificationBell notifications={notifications} />
        <div className="toolbar">
          <Link className="button" href="/my/new">+ إعلان جديد</Link>
          <Link className="button secondary" href="/my/favorites">المفضلة</Link>
          <Link className="button secondary" href="/my/searches">عمليات البحث المحفوظة</Link>
          <Link className="button secondary" href="/my/agency">مكتبي العقاري</Link>
          <Link className="button secondary" href="/my/import">استيراد CSV</Link>
          <Link className="button secondary" href="/my/projects">مشاريع على المخطط</Link>
          <Link className="button secondary" href="/my/contractor">ملف المقاول</Link>
          <Link className="button secondary" href="/my/settings">⚙ الإعدادات</Link>
          <form action={logout}>
            <button className="button secondary" type="submit">تسجيل الخروج</button>
          </form>
          <form action={logoutEverywhere}>
            <button className="button secondary" type="submit">الخروج من كل الأجهزة</button>
          </form>
        </div>
      </div>

      {listings.length > 0 ? (() => {
        const published = listings.filter((l) => l.status === "Published").length;
        const totalViews = listings.reduce((sum, l) => sum + (totalsById.get(l.listingId)?.views ?? 0), 0);
        const totalReveals = listings.reduce((sum, l) => sum + (totalsById.get(l.listingId)?.contactReveals ?? 0), 0);
        return (
          <div className="dashboard-stats">
            <div className="dash-stat"><span className="dash-stat-number">{listings.length}</span><span className="dash-stat-label">إجمالي الإعلانات</span></div>
            <div className="dash-stat"><span className="dash-stat-number">{published}</span><span className="dash-stat-label">منشور</span></div>
            <div className="dash-stat"><span className="dash-stat-number">{totalViews}</span><span className="dash-stat-label">مشاهدة (30 يوم)</span></div>
            <div className="dash-stat"><span className="dash-stat-number">{totalReveals}</span><span className="dash-stat-label">إظهار رقم (30 يوم)</span></div>
          </div>
        );
      })() : null}

      {quota ? (
        <div className="panel quota-bar">
          <span>الإعلانات النشطة: <strong>{quota.activeListings}</strong> / {quota.quota}</span>
          {quota.remaining === 0 ? <span className="warning">تم استنفاد الحصة</span> : null}
          {quota.activePackage ? (
            <span className="notice">باقة {quota.activePackage.listingQuota} إعلان · تنتهي <time dateTime={quota.activePackage.expiresAt}>{new Date(quota.activePackage.expiresAt).toLocaleDateString("ar-SY-u-nu-latn")}</time></span>
          ) : (
            <span className="notice">الباقة المجانية ({quota.quota} إعلانات)</span>
          )}
        </div>
      ) : null}

      {notice === "deleted" ? <p className="success" role="status">تم حذف المسودة.</p> : null}

      {!result.ok ? (
        <div className="empty">تعذر تحميل إعلاناتك. حاول مجدداً.</div>
      ) : listings.length === 0 ? (
        <div className="empty">
          <p>لم تنشر أي إعلان بعد.</p>
          <Link className="button" href="/my/new">أضف إعلانك الأول</Link>
        </div>
      ) : (
        <ul className="my-list" role="list">
          {listings.map((listing) => (
            <li key={listing.listingId} className="panel my-item">
              <div>
                <span className={`status status-${listing.status.toLowerCase()}`}>
                  {listing.closureReason ? label(closureReasonLabels, listing.closureReason) : label(listingStatusLabels, listing.status)}
                </span>
                <h2>
                  <Link href={`/my/listings/${listing.listingId}`}>{listing.headline ?? "إعلان بدون عنوان"}</Link>
                </h2>
                <p className="notice">
                  {[label(transactionIntentLabels, listing.context?.transactionIntent), listing.price ? formatPrice(listing.price) : ""]
                    .filter(Boolean)
                    .join(" · ")}
                </p>
                {listing.status !== "Draft" ? (
                  <p className="notice listing-totals">
                    آخر 30 يوماً: {totalsById.get(listing.listingId)?.views ?? 0} مشاهدة · {totalsById.get(listing.listingId)?.contactReveals ?? 0} إظهار رقم
                  </p>
                ) : null}
              </div>
              <div className="toolbar">
                <Link className="button secondary" href={`/my/listings/${listing.listingId}`}>
                  {listing.status === "Draft" ? "تعديل ونشر" : listing.status === "Closed" ? "عرض" : "إدارة"}
                </Link>
                {listing.status === "Published" ? (
                  <Link className="button secondary" href={`/listings/${listing.listingId}`}>الصفحة العامة</Link>
                ) : null}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
