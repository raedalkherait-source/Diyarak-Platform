"use client";

import { useEffect } from "react";

export default function OfflinePage() {
  useEffect(() => {
    function onOnline() {
      window.location.reload();
    }
    window.addEventListener("online", onOnline);
    return () => window.removeEventListener("online", onOnline);
  }, []);

  return (
    <div className="container section offline-page">
      <h1>غير متصل بالإنترنت</h1>
      <p>تحقق من اتصالك بالإنترنت وحاول مجدداً. ستُعاد المحاولة تلقائياً عند عودة الاتصال.</p>
      <button type="button" className="button" onClick={() => window.location.reload()}>
        إعادة المحاولة
      </button>
    </div>
  );
}
