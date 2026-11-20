import type { Metadata, Viewport } from "next";
import Link from "next/link";
import "./globals.css";

export const metadata: Metadata = {
  title: { default: "ديارك — عقارات سوريا للبيع والإيجار", template: "%s | ديارك" },
  description: "ابحث عن شقق وبيوت ومحلات للبيع والإيجار في جميع المحافظات السورية.",
};

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  themeColor: "#0f6b5c",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="ar" dir="rtl">
      <body>
        <header className="site-header">
          <div className="container">
            <Link href="/" className="logo">ديارك</Link>
            <nav className="nav" aria-label="التنقل الرئيسي">
              <Link href="/search?transactionIntent=Sell">شراء</Link>
              <Link href="/search?transactionIntent=Rent">إيجار</Link>
              <Link href="/search?propertyCategory=CommercialProperty">تجاري</Link>
            </nav>
          </div>
        </header>
        <main>{children}</main>
        <footer className="site-footer">
          <div className="container">© ديارك — منصة العقارات في سوريا</div>
        </footer>
      </body>
    </html>
  );
}
