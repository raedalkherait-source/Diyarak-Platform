"use client";

import { useState } from "react";

interface ShareButtonsProps {
  url: string;
  title: string;
}

export function ShareButtons({ url, title }: ShareButtonsProps) {
  const encoded = encodeURIComponent(url);
  const encodedTitle = encodeURIComponent(title);
  const [copied, setCopied] = useState(false);

  async function handleNativeShare() {
    try {
      await navigator.share({ title, url });
    } catch {
      // User cancelled or not supported
    }
  }

  async function handleCopyLink() {
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // Fallback: ignored
    }
  }

  return (
    <div className="share-buttons">
      <span className="share-label">مشاركة:</span>
      <a
        href={`https://wa.me/?text=${encodedTitle}%20${encoded}`}
        target="_blank"
        rel="noopener noreferrer"
        className="button secondary share-btn"
        title="مشاركة عبر واتساب"
      >
        واتساب
      </a>
      <a
        href={`https://t.me/share/url?url=${encoded}&text=${encodedTitle}`}
        target="_blank"
        rel="noopener noreferrer"
        className="button secondary share-btn"
        title="مشاركة عبر تلغرام"
      >
        تلغرام
      </a>
      <button type="button" className="button secondary share-btn" onClick={handleCopyLink} title="نسخ الرابط">
        {copied ? "تم النسخ ✓" : "نسخ الرابط"}
      </button>
      <span role="status" aria-live="polite" className="sr-only">{copied ? "تم نسخ الرابط" : ""}</span>
      {"share" in globalThis.navigator && (
        <button type="button" className="button secondary share-btn" onClick={handleNativeShare} title="مشاركة">
          مشاركة
        </button>
      )}
    </div>
  );
}
