"use client";

export default function ErrorPage({ reset }: { error: Error; reset: () => void }) {
  return (
    <div className="container section">
      <div className="error-page">
        <h1>حدث خطأ غير متوقع</h1>
        <p>نعتذر عن هذا الخلل. يمكنك إعادة المحاولة أو العودة إلى الصفحة الرئيسية.</p>
        <div className="error-actions">
          <button type="button" className="button" onClick={reset}>إعادة المحاولة</button>
          <a className="button secondary" href="/">الصفحة الرئيسية</a>
        </div>
      </div>
    </div>
  );
}
