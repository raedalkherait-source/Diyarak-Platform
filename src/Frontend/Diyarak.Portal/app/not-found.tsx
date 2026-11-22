import Link from "next/link";

export default function NotFound() {
  return (
    <div className="container section">
      <div className="error-page">
        <div className="error-code">404</div>
        <h1>الصفحة غير موجودة</h1>
        <p>ربما حُذف الإعلان أو لم يُنشر بعد، أو أن الرابط غير صحيح.</p>
        <div className="error-actions">
          <Link className="button" href="/search">تصفح الإعلانات</Link>
          <Link className="button secondary" href="/">الصفحة الرئيسية</Link>
        </div>
      </div>
    </div>
  );
}
