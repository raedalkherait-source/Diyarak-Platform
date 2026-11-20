import Image from "next/image";
import Link from "next/link";
import { formatArea, formatFloor, formatLocation, formatPrice, photoSrc } from "@/lib/format";
import { categoryLabels, label, transactionIntentLabels } from "@/lib/labels";
import type { PublicListing } from "@/lib/types";

export function ListingCard({ listing }: { listing: PublicListing }) {
  const cover = listing.photos[0];
  const property = listing.property;
  const facts = [
    label(categoryLabels, property.category),
    formatArea(property.livingArea),
    property.bedroomCount !== null ? `${property.bedroomCount} غرف نوم` : "",
    formatFloor(property.floorNumber, null),
  ].filter(Boolean);

  return (
    <article className="card">
      <Link href={`/listings/${listing.listingId}`} aria-label={listing.headline}>
        <div className="card-media">
          {cover ? (
            <Image
              src={photoSrc(listing.listingId, cover.photoId)}
              alt=""
              fill
              sizes="(max-width: 640px) 100vw, (max-width: 1100px) 50vw, 300px"
              quality={70}
            />
          ) : (
            <div className="placeholder">لا توجد صور</div>
          )}
          <span className="badge">{label(transactionIntentLabels, listing.context.transactionIntent)}</span>
          {listing.photos.length > 1 ? <span className="photo-count">{listing.photos.length} صور</span> : null}
        </div>
      </Link>
      <div className="card-body">
        <div className="price">{formatPrice(listing.price)}</div>
        <h3 className="card-title">
          <Link href={`/listings/${listing.listingId}`}>{listing.headline}</Link>
        </h3>
        <div className="location">{formatLocation(property.location)}</div>
        <div className="facts">
          {facts.map((fact) => (
            <span key={fact}>{fact}</span>
          ))}
        </div>
      </div>
    </article>
  );
}
