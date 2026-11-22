"use client";

import { useState, useActionState } from "react";
import { submitReview, type ReviewFormState } from "@/app/my/actions";

function StarRating({ value, onChange }: { value: number; onChange?: (v: number) => void }) {
  return (
    <span className="star-rating" role={onChange ? "radiogroup" : "img"} aria-label={`${value} من 5 نجوم`}>
      {[1, 2, 3, 4, 5].map((star) => (
        <span
          key={star}
          className={star <= value ? "star filled" : "star"}
          onClick={() => onChange?.(star)}
          onKeyDown={(e) => e.key === "Enter" && onChange?.(star)}
          role={onChange ? "radio" : undefined}
          aria-checked={onChange ? star === value : undefined}
          tabIndex={onChange ? 0 : undefined}
          style={{ cursor: onChange ? "pointer" : "default", fontSize: "1.5rem" }}
        >
          {star <= value ? "★" : "☆"}
        </span>
      ))}
    </span>
  );
}

interface ReviewItem {
  id: string;
  authorUserId: string;
  rating: number;
  comment: string | null;
  createdAt: string;
}

interface ReviewSectionProps {
  subjectId: string;
  subjectType: "Contractor" | "Agency";
  reviews: ReviewItem[];
  average: number;
  count: number;
  hasMore: boolean;
  signedIn: boolean;
}

export function ReviewSection({ subjectId, subjectType, reviews, average, count, hasMore, signedIn }: ReviewSectionProps) {
  const [state, action, pending] = useActionState<ReviewFormState, FormData>(submitReview, {});
  const [rating, setRating] = useState(0);

  return (
    <section style={{ marginTop: "2rem" }}>
      <h2 style={{ marginBottom: "0.5rem" }}>التقييمات ({count})</h2>
      {count > 0 && (
        <p style={{ marginBottom: "1rem", color: "var(--muted)" }}>
          <StarRating value={Math.round(average)} /> {average.toFixed(1)} من 5
        </p>
      )}

      {signedIn && !state.success && (
        <form action={action} style={{ marginBottom: "1.5rem", padding: "1rem", border: "1px solid var(--border)", borderRadius: "8px" }}>
          <input type="hidden" name="subjectId" value={subjectId} />
          <input type="hidden" name="subjectType" value={subjectType} />
          <input type="hidden" name="rating" value={rating} />
          <div style={{ marginBottom: "0.75rem" }}>
            <label style={{ display: "block", marginBottom: "0.25rem", fontWeight: 600 }}>تقييمك:</label>
            <StarRating value={rating} onChange={setRating} />
          </div>
          <div style={{ marginBottom: "0.75rem" }}>
            <label htmlFor="review-comment" style={{ display: "block", marginBottom: "0.25rem" }}>تعليق (اختياري):</label>
            <textarea id="review-comment" name="comment" maxLength={1000} rows={3} style={{ width: "100%" }} />
          </div>
          {state.formError && <p style={{ color: "var(--error, #c00)", marginBottom: "0.5rem" }}>{state.formError}</p>}
          <button type="submit" className="button" disabled={pending || rating === 0}>
            {pending ? "جارٍ الإرسال..." : "أرسل التقييم"}
          </button>
        </form>
      )}
      {state.success && (
        <p style={{ color: "var(--success, #060)", marginBottom: "1rem", fontWeight: 600 }}>تم إرسال تقييمك بنجاح!</p>
      )}

      <div style={{ display: "flex", flexDirection: "column", gap: "0.75rem" }}>
        {reviews.map((review) => (
          <div
            key={review.id}
            style={{ padding: "0.75rem", border: "1px solid var(--border)", borderRadius: "8px" }}
          >
            <StarRating value={review.rating} />
            {review.comment && <p style={{ marginTop: "0.25rem", whiteSpace: "pre-line" }}>{review.comment}</p>}
            <time style={{ fontSize: "0.75rem", color: "var(--muted)" }}>
              {new Date(review.createdAt).toLocaleDateString("ar-SY-u-nu-latn")}
            </time>
          </div>
        ))}
        {reviews.length === 0 && <p style={{ color: "var(--muted)" }}>لا توجد تقييمات بعد.</p>}
      </div>
    </section>
  );
}
