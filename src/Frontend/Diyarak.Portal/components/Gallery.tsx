"use client";

import Image from "next/image";
import { useCallback, useEffect, useRef, useState } from "react";
import { photoSrc } from "@/lib/format";
import type { PublicListingPhoto } from "@/lib/types";

export function Gallery({ listingId, photos, title }: { listingId: string; photos: PublicListingPhoto[]; title: string }) {
  const [index, setIndex] = useState(0);
  const [lightbox, setLightbox] = useState(false);
  const current = photos[index];
  const closeRef = useRef<HTMLButtonElement>(null);
  const lightboxRef = useRef<HTMLDivElement>(null);

  const prev = useCallback(() => setIndex((i) => (i - 1 + photos.length) % photos.length), [photos.length]);
  const next = useCallback(() => setIndex((i) => (i + 1) % photos.length), [photos.length]);

  useEffect(() => {
    if (!lightbox) return;
    closeRef.current?.focus();
    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") { setLightbox(false); return; }
      if (e.key === "ArrowLeft") { next(); return; }
      if (e.key === "ArrowRight") { prev(); return; }
      if (e.key !== "Tab" || !lightboxRef.current) return;
      const focusable = lightboxRef.current.querySelectorAll<HTMLElement>("button");
      if (focusable.length === 0) return;
      const first = focusable[0]!;
      const last = focusable[focusable.length - 1]!;
      if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
      else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
    }
    document.addEventListener("keydown", onKey);
    document.body.style.overflow = "hidden";
    return () => { document.removeEventListener("keydown", onKey); document.body.style.overflow = ""; };
  }, [lightbox, prev, next]);

  if (!current) {
    return <div className="gallery-main empty">لا توجد صور لهذا الإعلان</div>;
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === "ArrowLeft") next();
    else if (e.key === "ArrowRight") prev();
  }

  return (
    <div onKeyDown={handleKeyDown}>
      <div
        className="gallery-main clickable"
        tabIndex={0}
        role="img"
        aria-label={`${title} — صورة ${index + 1} من ${photos.length}`}
        onClick={() => setLightbox(true)}
      >
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
            <button type="button" onClick={(e) => { e.stopPropagation(); prev(); }} aria-label="الصورة السابقة">→</button>
            <span>{index + 1} / {photos.length}</span>
            <button type="button" onClick={(e) => { e.stopPropagation(); next(); }} aria-label="الصورة التالية">←</button>
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

      {lightbox ? (
        <div ref={lightboxRef} className="lightbox" onClick={() => setLightbox(false)} role="dialog" aria-modal="true" aria-label={`${title} — صورة ${index + 1} من ${photos.length}`}>
          <button ref={closeRef} type="button" className="lightbox-close" onClick={() => setLightbox(false)} aria-label="إغلاق">✕</button>
          <div className="lightbox-content" onClick={(e) => e.stopPropagation()}>
            <Image
              src={photoSrc(listingId, current.photoId)}
              alt={`${title} — صورة ${index + 1} من ${photos.length}`}
              fill
              sizes="100vw"
              quality={85}
              style={{ objectFit: "contain" }}
            />
          </div>
          {photos.length > 1 ? (
            <>
              <button type="button" className="lightbox-prev" onClick={(e) => { e.stopPropagation(); prev(); }} aria-label="السابقة">→</button>
              <button type="button" className="lightbox-next" onClick={(e) => { e.stopPropagation(); next(); }} aria-label="التالية">←</button>
              <div className="lightbox-counter">{index + 1} / {photos.length}</div>
            </>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
