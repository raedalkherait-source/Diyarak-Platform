"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { uploadPhoto } from "@/app/my/actions";

export function PhotoUploader({ listingId, remaining }: { listingId: string; remaining: number }) {
  const router = useRouter();
  const [progress, setProgress] = useState<string | null>(null);
  const [errors, setErrors] = useState<string[]>([]);

  async function onChange(event: React.ChangeEvent<HTMLInputElement>) {
    const files = Array.from(event.target.files ?? []).slice(0, remaining);
    event.target.value = "";
    if (files.length === 0) return;

    const failures: string[] = [];
    for (const [index, file] of files.entries()) {
      setProgress(`جارٍ رفع الصورة ${index + 1} من ${files.length}…`);
      const form = new FormData();
      form.set("photo", file);
      const result = await uploadPhoto(listingId, form);
      if (!result.ok) failures.push(`${file.name}: ${result.error}`);
    }

    setProgress(null);
    setErrors(failures);
    router.refresh();
  }

  if (remaining <= 0) return <p className="notice">وصلت إلى الحد الأقصى من الصور.</p>;

  return (
    <div className="uploader">
      <label className="button secondary upload-label">
        + إضافة صور
        <input type="file" accept="image/jpeg,image/png,image/webp" multiple onChange={onChange} disabled={progress !== null} />
      </label>
      <p className="notice">JPEG أو PNG أو WebP، حتى 10 ميغابايت للصورة. نحذف بيانات الموقع (GPS) من الصور تلقائياً.</p>
      {progress ? <p className="notice" aria-live="polite">{progress}</p> : null}
      {errors.map((message) => (
        <p key={message} className="form-error" role="alert">{message}</p>
      ))}
    </div>
  );
}
