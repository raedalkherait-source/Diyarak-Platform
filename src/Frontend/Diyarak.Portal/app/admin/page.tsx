import type { Metadata } from "next";
import Link from "next/link";
import { decideAgency, decidePayment, moderate } from "./actions";
import type { PaymentOrderView } from "@/components/FeatureListingPanel";
import type { OwnerAgency } from "@/lib/agency";
import { formatPrice } from "@/lib/format";
import { label } from "@/lib/labels";
import { managementFetch } from "@/lib/managementApi";
import { reportReasonLabels } from "@/lib/moderation";
import { listingStatusLabels } from "@/lib/status";

export const metadata: Metadata = { title: "المراجعة", robots: { index: false } };

interface ModerationListing {
  listingId: string;
  status: string;
  publisherUserId: string;
  headline: string | null;
  transactionIntent: string | null;
  price: { isOnRequest: boolean; amount: number | null; currency: string | null } | null;
  moderationNote: string | null;
}

interface QueueItem {
  listing: ModerationListing;
  openReports: { reportId: string; reason: string; comment: string | null; createdAt: string }[];
}

const dateFormat = new Intl.DateTimeFormat("ar-SY-u-nu-latn", { dateStyle: "medium", timeStyle: "short" });

export default async function AdminPage() {
  const queue = await managementFetch<{ items: QueueItem[] }>("/api/moderation/queue", { allowForbidden: true });

  if (!queue.ok && queue.status === 403) {
    return (
      <div className="container section">
        <div className="empty">هذه الصفحة مخصصة لفريق المراجعة فقط.</div>
      </div>
    );
  }

  const suspended = await managementFetch<{ items: ModerationListing[] }>("/api/moderation/suspended", { allowForbidden: true });
  const agencies = await managementFetch<{ items: OwnerAgency[] }>("/api/moderation/agencies", { allowForbidden: true });
  const pendingAgencies = agencies.ok ? agencies.value.items : [];
  const payments = await managementFetch<{ items: PaymentOrderView[] }>("/api/moderation/payments", { allowForbidden: true });
  const pendingPayments = payments.ok ? payments.value.items : [];
  const items = queue.ok ? queue.value.items : [];
  const suspendedItems = suspended.ok ? suspended.value.items : [];

  return (
    <div className="container section narrow">
      <h1>مراجعة الإعلانات</h1>

      <section>
        <h2>مدفوعات بانتظار التأكيد ({pendingPayments.length})</h2>
        {pendingPayments.length === 0 ? <div className="empty">لا توجد مدفوعات بانتظار التأكيد.</div> : null}
        <ul className="my-list">
          {pendingPayments.map((order) => (
            <li key={order.orderId} className="panel moderation-item">
              <div>
                <h3><Link href={`/listings/${order.listingId}`} target="_blank">تمييز {order.days} يوماً</Link></h3>
                <p className="notice">
                  {order.amount.toLocaleString("en-US")} {order.currency} · {order.method} · رقم العملية <strong dir="ltr" translate="no">{order.reference}</strong> ·{" "}
                  {dateFormat.format(new Date(order.createdAt))}
                </p>
              </div>
              <form action={decidePayment.bind(null, order.orderId)} className="toolbar">
                <button className="button" type="submit" name="decision" value="confirm">وصل المبلغ — تفعيل</button>
                <input name="note" placeholder="سبب الرفض" maxLength={300} aria-label="سبب الرفض" />
                <button className="button secondary" type="submit" name="decision" value="reject">رفض</button>
              </form>
            </li>
          ))}
        </ul>
      </section>

      <section>
        <h2>طلبات توثيق المكاتب ({pendingAgencies.length})</h2>
        {pendingAgencies.length === 0 ? <div className="empty">لا توجد طلبات توثيق.</div> : null}
        <ul className="my-list">
          {pendingAgencies.map((agency) => (
            <li key={agency.agencyId} className="panel moderation-item">
              <div>
                <h3><Link href={`/agencies/${agency.agencyId}`} target="_blank">{agency.name}</Link></h3>
                <p className="notice">{agency.city} · رقم السجل/الترخيص: <strong dir="ltr" translate="no">{agency.registrationNumber}</strong></p>
              </div>
              <form action={decideAgency.bind(null, agency.agencyId)} className="toolbar">
                <button className="button" type="submit" name="decision" value="verify">توثيق</button>
                <input name="note" placeholder="سبب الرفض" maxLength={500} aria-label="سبب الرفض" />
                <button className="button secondary" type="submit" name="decision" value="reject">رفض</button>
              </form>
            </li>
          ))}
        </ul>
      </section>

      <section>
        <h2>البلاغات المفتوحة ({items.length})</h2>
        {items.length === 0 ? <div className="empty">لا توجد بلاغات بانتظار المراجعة.</div> : null}
        <ul className="my-list">
          {items.map(({ listing, openReports }) => (
            <li key={listing.listingId} className="panel moderation-item">
              <div>
                <span className={`status status-${listing.status.toLowerCase()}`}>{label(listingStatusLabels, listing.status)}</span>
                <h3>
                  {listing.status === "Published" ? (
                    <Link href={`/listings/${listing.listingId}`} target="_blank">{listing.headline ?? "بدون عنوان"}</Link>
                  ) : (
                    listing.headline ?? "بدون عنوان"
                  )}
                </h3>
                <p className="notice">{listing.price ? formatPrice(listing.price) : ""}</p>
                <ul className="report-list">
                  {openReports.map((report) => (
                    <li key={report.reportId}>
                      <strong>{label(reportReasonLabels, report.reason)}</strong>
                      <span className="notice"> · {dateFormat.format(new Date(report.createdAt))}</span>
                      {report.comment ? <p>{report.comment}</p> : null}
                    </li>
                  ))}
                </ul>
              </div>
              <div className="moderation-actions">
                {listing.status === "Published" || listing.status === "Paused" ? (
                  <form action={moderate.bind(null, listing.listingId)} className="suspend-form">
                    <input type="hidden" name="action" value="suspend" />
                    <label className="field">
                      سبب الإيقاف (يظهر للمعلن)
                      <input name="note" required maxLength={500} />
                    </label>
                    <button className="button" type="submit">إيقاف الإعلان</button>
                  </form>
                ) : null}
                <form action={moderate.bind(null, listing.listingId)}>
                  <input type="hidden" name="action" value="dismiss-reports" />
                  <button className="button secondary" type="submit">رفض البلاغات</button>
                </form>
              </div>
            </li>
          ))}
        </ul>
      </section>

      <section className="section">
        <h2>الإعلانات الموقوفة ({suspendedItems.length})</h2>
        {suspendedItems.length === 0 ? <div className="empty">لا توجد إعلانات موقوفة.</div> : null}
        <ul className="my-list">
          {suspendedItems.map((listing) => (
            <li key={listing.listingId} className="panel my-item">
              <div>
                <h3>{listing.headline ?? "بدون عنوان"}</h3>
                <p className="notice">السبب: {listing.moderationNote}</p>
              </div>
              <form action={moderate.bind(null, listing.listingId)}>
                <input type="hidden" name="action" value="reinstate" />
                <button className="button secondary" type="submit">إعادة النشر</button>
              </form>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
