import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { deletePhoto } from "@/app/my/actions";
import { PhotoUploader } from "@/components/PhotoUploader";
import { PublishButton } from "@/components/PublishButton";
import { isListingId } from "@/lib/contact";
import { formatPrice } from "@/lib/format";
import { label, publishingRoleLabels, transactionIntentLabels } from "@/lib/labels";
import { managementFetch, type ManagementListing, type ManagementPhoto } from "@/lib/managementApi";

export const metadata: Metadata = { title: "إدارة الإعلان", robots: { index: false } };

type Props = { params: Promise<{ id: string }>; searchParams: Promise<{ notice?: string }> };

const notices: Record<string, string> = {
  created: "تم حفظ الإعلان كمسودة. أضف الصور ثم انشره.",
  "contact-unverified": "تم حفظ الإعلان، لكن لم نتمكن من تفعيل التواصل لأن حسابك بلا رقم موثّق.",
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
  const isDraft = value.status === "Draft";

  return (
    <div className="container section narrow">
      <Link href="/my" className="notice">→ إعلاناتي</Link>
      {notice && notices[notice] ? <p className="success" role="status">{notices[notice]}</p> : null}

      <div className="panel">
        <span className={`status status-${value.status.toLowerCase()}`}>{isDraft ? "مسودة" : "منشور"}</span>
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
        <p className="notice">
          التواصل:{" "}
          {value.contact
            ? [value.contact.allowCalls ? "اتصال" : "", value.contact.allowWhatsApp ? "واتساب" : ""].filter(Boolean).join(" و")
            : "غير مفعّل"}
        </p>
        {!isDraft ? <Link className="button secondary" href={`/listings/${id}`}>عرض الصفحة العامة</Link> : null}
      </div>

      <section className="panel">
        <h2>الصور ({items.length})</h2>
        {items.length > 0 ? (
          <ul className="photo-grid">
            {items.map((photo) => (
              <li key={photo.photoId}>
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={`/my/photo/${id}/${photo.photoId}`} alt={`صورة ${photo.position + 1}`} />
                {photo.position === 0 ? <span className="badge">الغلاف</span> : null}
                {isDraft ? (
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
        {isDraft ? <PhotoUploader listingId={id} remaining={30 - items.length} /> : null}
      </section>

      {isDraft ? (
        <section className="panel">
          <h2>النشر</h2>
          <PublishButton listingId={id} version={value.version} photoCount={items.length} />
        </section>
      ) : null}
    </div>
  );
}
