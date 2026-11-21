import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { changeListingStatus, deleteDraft, deletePhoto } from "@/app/my/actions";
import { EditListingForm } from "@/components/EditListingForm";
import { EditPropertyForm } from "@/components/EditPropertyForm";
import { ListingStatsPanel } from "@/components/ListingStatsPanel";
import type { ListingStatistics } from "@/lib/statistics";
import { PhotoUploader } from "@/components/PhotoUploader";
import { PublishButton } from "@/components/PublishButton";
import { isListingId } from "@/lib/contact";
import { formatPrice } from "@/lib/format";
import { mapConfig } from "@/lib/map";
import { propertyFormValues, type ManagementProperty } from "@/lib/listingForm";
import { label, publishingRoleLabels, transactionIntentLabels } from "@/lib/labels";
import { managementFetch, type ManagementListing, type ManagementPhoto } from "@/lib/managementApi";
import { closureReasonLabels, listingStatusLabels } from "@/lib/status";

export const metadata: Metadata = { title: "إدارة الإعلان", robots: { index: false } };

type Props = { params: Promise<{ id: string }>; searchParams: Promise<{ notice?: string }> };

const notices: Record<string, [string, "success" | "form-error"]> = {
  created: ["تم حفظ الإعلان كمسودة. أضف الصور ثم انشره.", "success"],
  "contact-unverified": ["تم حفظ الإعلان، لكن لم نتمكن من تفعيل التواصل لأن حسابك بلا رقم موثّق.", "form-error"],
  updated: ["تم حفظ التعديلات.", "success"],
  "property-updated": ["تم حفظ تفاصيل العقار.", "success"],
  "status-pause": ["تم إيقاف الإعلان مؤقتاً. لن يظهر للزوار حتى تعيد تفعيله.", "success"],
  "status-resume": ["عاد الإعلان للظهور للزوار.", "success"],
  "status-close": ["تم إنهاء الإعلان. شكراً لاستخدامك ديارك.", "success"],
  "delete-failed": ["تعذر حذف المسودة. ربما تغيّرت أو نُشرت؛ حدّث الصفحة وحاول مجدداً.", "form-error"],
  "status-failed": ["تعذر تغيير حالة الإعلان. حدّث الصفحة وحاول مجدداً.", "form-error"],
};

export default async function ManageListingPage({ params, searchParams }: Props) {
  const { id } = await params;
  const { notice } = await searchParams;
  if (!isListingId(id)) notFound();

  const listing = await managementFetch<ManagementListing>(`/api/market/listings/${id}`);
  if (!listing.ok) notFound();

  const photos = await managementFetch<{ items: ManagementPhoto[] }>(`/api/market/listings/${id}/photos`);
  const items = photos.ok ? photos.value.items : [];
  const value = listing.value;
  const editable = value.status !== "Closed";

  const stats =
    value.status !== "Draft" ? await managementFetch<ListingStatistics>(`/api/market/listings/${id}/statistics?days=30`) : null;

  const property =
    editable && value.subject.subjectType === "market.property"
      ? await managementFetch<ManagementProperty>(`/api/market/properties/${value.subject.subjectId}`)
      : null;
  const status = changeListingStatus.bind(null, id);
  const shownNotice = notice ? notices[notice] : undefined;

  return (
    <div className="container section narrow">
      <Link href="/my" className="notice">→ إعلاناتي</Link>
      {shownNotice ? <p className={shownNotice[1]} role="status">{shownNotice[0]}</p> : null}

      <div className="panel">
        <span className={`status status-${value.status.toLowerCase()}`}>
          {value.closureReason ? label(closureReasonLabels, value.closureReason) : label(listingStatusLabels, value.status)}
        </span>
        <h1>{value.headline ?? "إعلان بدون عنوان"}</h1>
        <p className="notice">
          {[
            label(transactionIntentLabels, value.context?.transactionIntent),
            value.price ? formatPrice(value.price) : "",
            value.context ? `المعلن: ${label(publishingRoleLabels, value.context.publishingRole)}` : "",
          ]
            .filter(Boolean)
            .join(" · ")}
        </p>
        {value.status === "Published" ? <Link className="button secondary" href={`/listings/${id}`}>عرض الصفحة العامة</Link> : null}
      </div>

      {stats?.ok ? <ListingStatsPanel stats={stats.value} /> : null}

      {value.status === "Suspended" ? (
        <section className="panel">
          <h2>أوقفت الإدارة هذا الإعلان</h2>
          <p className="form-error">{value.moderationNote}</p>
          <p className="notice">يمكنك تعديل الإعلان لتصحيح المشكلة ثم التواصل مع فريق ديارك، أو سحبه.</p>
          <form action={status} className="toolbar">
            <input type="hidden" name="action" value="close" />
            <button className="button secondary" type="submit" name="reason" value="Withdrawn">سحب الإعلان</button>
          </form>
        </section>
      ) : null}

      {value.status === "Published" || value.status === "Paused" ? (
        <section className="panel">
          <h2>حالة الإعلان</h2>
          <div className="toolbar">
            {value.status === "Published" ? (
              <form action={status}>
                <input type="hidden" name="action" value="pause" />
                <button className="button secondary" type="submit">إيقاف مؤقت</button>
              </form>
            ) : (
              <form action={status}>
                <input type="hidden" name="action" value="resume" />
                <button className="button" type="submit">إعادة التفعيل</button>
              </form>
            )}
          </div>
          <details className="close-listing">
            <summary>إنهاء الإعلان نهائياً…</summary>
            <p className="notice">لن يظهر الإعلان للزوار ولن تتمكن من تعديله بعد الإنهاء.</p>
            <form action={status} className="toolbar">
              <input type="hidden" name="action" value="close" />
              <button className="button" type="submit" name="reason" value={value.context?.transactionIntent === "Sell" ? "Sold" : "Rented"}>
                {value.context?.transactionIntent === "Sell" ? "تم البيع" : "تم التأجير"}
              </button>
              <button className="button secondary" type="submit" name="reason" value="Withdrawn">سحب الإعلان</button>
            </form>
          </details>
        </section>
      ) : null}

      {editable && value.status !== "Draft" ? (
        <section className="panel">
          <h2>تعديل الإعلان</h2>
          <EditListingForm listing={value} />
        </section>
      ) : null}

      {property?.ok ? (
        <section className="panel">
          <h2>تفاصيل العقار</h2>
          <EditPropertyForm
            listingId={id}
            propertyId={property.value.propertyId}
            version={property.value.version}
            category={property.value.category}
            initialValues={propertyFormValues(property.value)}
            mapConfig={mapConfig()}
          />
        </section>
      ) : null}

      <section className="panel">
        <h2>الصور ({items.length})</h2>
        {items.length > 0 ? (
          <ul className="photo-grid">
            {items.map((photo) => (
              <li key={photo.photoId}>
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={`/my/photo/${id}/${photo.photoId}`} alt={`صورة ${photo.position + 1}`} />
                {photo.position === 0 ? <span className="badge">الغلاف</span> : null}
                {editable ? (
                  <form action={deletePhoto.bind(null, id, photo.photoId)}>
                    <button className="photo-delete" type="submit" aria-label={`حذف الصورة ${photo.position + 1}`}>×</button>
                  </form>
                ) : null}
              </li>
            ))}
          </ul>
        ) : (
          <p className="notice">لا توجد صور بعد.</p>
        )}
        {editable ? <PhotoUploader listingId={id} remaining={30 - items.length} /> : null}
      </section>

      {value.status === "Draft" ? (
        <section className="panel">
          <h2>النشر</h2>
          <PublishButton listingId={id} version={value.version} photoCount={items.length} />
          <details className="close-listing">
            <summary>حذف المسودة…</summary>
            <p className="notice">ستُحذف المسودة وصورها وتفاصيل العقار نهائياً، ولا يمكن التراجع عن ذلك.</p>
            <form action={deleteDraft.bind(null, id, value.version)}>
              <button className="button secondary danger" type="submit">حذف المسودة نهائياً</button>
            </form>
          </details>
        </section>
      ) : null}
    </div>
  );
}
