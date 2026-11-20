"use client";

import { useState } from "react";
import { photoSrc } from "@/lib/format";
import type { PublicListingPhoto } from "@/lib/types";

export function Gallery({ listingId, photos, title }: { listingId: string; photos: PublicListingPhoto[]; title: string }) {
  const [index, setIndex] = useState(0);
  const current = photos[index];

  if (!current) {
    return <div className="gallery-main empty">لا توجد صور لهذا الإعلان</div>;
  }

  return (
    <div>
      <div className="gallery-main">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src={photoSrc(listingId, current.photoId)} alt={`${title} — صورة ${index + 1} من ${photos.length}`} />
      </div>
      {photos.length > 1 ? (
        <div className="gallery-thumbs" role="list">
          {photos.map((photo, photoIndex) => (
            <button
              key={photo.photoId}
              type="button"
              role="listitem"
              aria-current={photoIndex === index}
              aria-label={`عرض الصورة ${photoIndex + 1}`}
              onClick={() => setIndex(photoIndex)}
            >
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={photoSrc(listingId, photo.photoId)} alt="" loading="lazy" />
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}
