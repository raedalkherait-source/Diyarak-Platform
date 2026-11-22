"use client";

import Image from "next/image";
import { useState } from "react";
import { photoSrc } from "@/lib/format";
import type { PublicListingPhoto } from "@/lib/types";

export function Gallery({ listingId, photos, title }: { listingId: string; photos: PublicListingPhoto[]; title: string }) {
  const [index, setIndex] = useState(0);
  const current = photos[index];

  if (!current) {
    return <div className="gallery-main empty">لا توجد صور لهذا الإعلان</div>;
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === "ArrowLeft") setIndex((i) => (i + 1) % photos.length);
    else if (e.key === "ArrowRight") setIndex((i) => (i - 1 + photos.length) % photos.length);
  }

  return (
    <div onKeyDown={handleKeyDown}>
      <div className="gallery-main" tabIndex={0} role="img" aria-label={`${title} — صورة ${index + 1} من ${photos.length}`}>
        <Image
          src={photoSrc(listingId, current.photoId)}
          alt={`${title} — صورة ${index + 1} من ${photos.length}`}
          fill
          priority={index === 0}
          sizes="(max-width: 900px) 100vw, 800px"
          quality={75}
        />
        {photos.length > 1 ? (
          <div className="gallery-nav">
            <button type="button" onClick={() => setIndex((i) => (i - 1 + photos.length) % photos.length)} aria-label="الصورة السابقة">→</button>
            <span>{index + 1} / {photos.length}</span>
            <button type="button" onClick={() => setIndex((i) => (i + 1) % photos.length)} aria-label="الصورة التالية">←</button>
          </div>
        ) : null}
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
              <Image src={photoSrc(listingId, photo.photoId)} alt="" width={96} height={64} sizes="96px" quality={60} />
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}
