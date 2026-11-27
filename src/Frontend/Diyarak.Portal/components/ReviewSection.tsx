"use client";

import { useState, useActionState } from "react";
import { submitReview, type ReviewFormState } from "@/app/my/actions";

function StarRating({ value, onChange }: { value: number; onChange?: (v: number) => void }) {
  return (
    <span className="star-rating" role={onChange ? "radiogroup" : "img"} aria-label={`${value} من 5 نجوم`}>
      {[1, 2, 3, 4, 5].map((star) => (
        <span
          key={star}
          className={`star ${star <= value ? "filled" : ""} ${onChange ? "interactive" : ""}`}
          onClick={() => onChange?.(star)}
          onKeyDown={(e) => e.key === "Enter" && onChange?.(star)}
          role={onChange ? "radio" : undefined}
          aria-checked={onChange ? star === value : undefined}
          tabIndex={onChange ? 0 : undefined}
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
    <section className="reviews-section">
      <h2>التقييمات ({count})</h2>
      {count > 0 && (
        <p className="reviews-average">
          <StarRating value={Math.round(average)} /> {average.toFixed(1)} من 5
        </p>
      )}

      {signedIn && !state.success && (
        <form action={action} className="review-form">
          <input type="hidden" name="subjectId" value={subjectId} />
          <input type="hidden" name="subjectType" value={subjectType} />
          <input type="hidden" name="rating" value={rating} />
          <div className="review-field">
            <label className="review-label">تقييمك:</label>
            <StarRating value={rating} onChange={setRating} />
          </div>
          <div className="review-field">
            <label htmlFor="review-comment">تعليق (اختياري):</label>
            <textarea id="review-comment" name="comment" maxLength={1000} rows={3} />
          </div>
          {state.formError && <p className="form-error" role="alert">{state.formError}</p>}
          <button type="submit" className="button" disabled={pending || rating === 0}>
            {pending ? "جارٍ الإرسال..." : "أرسل التقييم"}
          </button>
        </form>
      )}
      {state.success && (
        <p className="form-success review-success">تم إرسال تقييمك بنجاح!</p>
      )}

      <div className="reviews-list">
        {reviews.map((review) => (
          <div key={review.id} className="review-card">
            <StarRating value={review.rating} />
            {review.comment && <p className="review-comment">{review.comment}</p>}
            <time className="review-time" dateTime={review.createdAt}>
              {new Date(review.createdAt).toLocaleDateString("ar-SY-u-nu-latn")}
            </time>
          </div>
        ))}
        {reviews.length === 0 && <p className="notice">لا توجد تقييمات بعد.</p>}
      </div>
    </section>
  );
}
