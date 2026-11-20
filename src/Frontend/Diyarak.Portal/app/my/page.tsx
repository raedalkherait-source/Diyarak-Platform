import type { Metadata } from "next";
import Link from "next/link";
import { logout } from "./actions";
import { formatPrice } from "@/lib/format";
import { label, transactionIntentLabels } from "@/lib/labels";
import { managementFetch, type ManagementListing } from "@/lib/managementApi";

export const metadata: Metadata = { title: "إعلاناتي", robots: { index: false } };

export default async function MyListingsPage() {
  const result = await managementFetch<{ items: ManagementListing[]; hasMore: boolean }>("/api/market/listings?pageSize=100");
  const listings = result.ok ? result.value.items : [];

  return (
    <div className="container section">
      <div className="section-title">
        <h1>إعلاناتي</h1>
        <div className="toolbar">
          <Link className="button" href="/my/new">+ إعلان جديد</Link>
          <form action={logout}>
            <button className="button secondary" type="submit">تسجيل الخروج</button>
          </form>
        </div>
      </div>

      {!result.ok ? (
        <div className="empty">تعذر تحميل إعلاناتك. حاول مجدداً.</div>
      ) : listings.length === 0 ? (
        <div className="empty">
          <p>لم تنشر أي إعلان بعد.</p>
          <Link className="button" href="/my/new">أضف إعلانك الأول</Link>
        </div>
      ) : (
        <ul className="my-list">
          {listings.map((listing) => (
            <li key={listing.listingId} className="panel my-item">
              <div>
                <span className={`status status-${listing.status.toLowerCase()}`}>
                  {listing.status === "Published" ? "منشور" : "مسودة"}
                </span>
                <h2>
                  <Link href={`/my/listings/${listing.listingId}`}>{listing.headline ?? "إعلان بدون عنوان"}</Link>
                </h2>
                <p className="notice">
                  {[label(transactionIntentLabels, listing.context?.transactionIntent), listing.price ? formatPrice(listing.price) : ""]
                    .filter(Boolean)
                    .join(" · ")}
                </p>
              </div>
              <div className="toolbar">
                <Link className="button secondary" href={`/my/listings/${listing.listingId}`}>
                  {listing.status === "Draft" ? "تعديل ونشر" : "عرض"}
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
