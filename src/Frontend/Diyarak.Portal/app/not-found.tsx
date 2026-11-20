import Link from "next/link";

export default function NotFound() {
  return (
    <div className="container section">
      <div className="empty">
        <h1>الصفحة غير موجودة</h1>
        <p>ربما حُذف الإعلان أو لم يُنشر بعد.</p>
        <Link className="button" href="/search">تصفح الإعلانات</Link>
      </div>
    </div>
  );
}
