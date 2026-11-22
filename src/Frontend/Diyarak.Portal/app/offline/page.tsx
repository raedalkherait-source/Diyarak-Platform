"use client";

export default function OfflinePage() {
  return (
    <div className="container section offline-page">
      <h1>غير متصل بالإنترنت</h1>
      <p>تحقق من اتصالك بالإنترنت وحاول مجدداً.</p>
      <button className="button" onClick={() => window.location.reload()}>
        إعادة المحاولة
      </button>
    </div>
  );
}
