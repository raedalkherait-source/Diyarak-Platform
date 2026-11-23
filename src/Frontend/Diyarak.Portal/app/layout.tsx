import type { Metadata, Viewport } from "next";
import Link from "next/link";
import { ServiceWorkerRegistration } from "@/components/ServiceWorkerRegistration";
import { ThemeToggle } from "@/components/ThemeToggle";
import { CompareBar } from "@/components/CompareListings";
import { BackToTop } from "@/components/BackToTop";
import { MobileNav } from "@/components/MobileNav";
import { getSessionToken } from "@/lib/session";
import "./globals.css";

export const metadata: Metadata = {
  // Absolute base for Open Graph image URLs in shared links.
  metadataBase: new URL(process.env.PORTAL_PUBLIC_URL ?? "http://localhost:3000"),
  title: { default: "ديارك — عقارات سوريا للبيع والإيجار", template: "%s | ديارك" },
  description: "ابحث عن شقق وبيوت ومحلات للبيع والإيجار في جميع المحافظات السورية.",
  manifest: "/manifest.json",
  appleWebApp: { capable: true, statusBarStyle: "default", title: "ديارك" },
};

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  themeColor: "#0f6b5c",
};

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  const signedIn = (await getSessionToken()) !== null;

  return (
    <html lang="ar" dir="rtl">
      <head>
        <link rel="apple-touch-icon" href="/icon-192.png" />
        <link rel="dns-prefetch" href="https://tile.openstreetmap.org" />
        <link rel="preconnect" href="https://tile.openstreetmap.org" crossOrigin="anonymous" />
      </head>
      <body>
        <a href="#main-content" className="skip-link">تخطي إلى المحتوى</a>
        <header className="site-header">
          <div className="container">
            <Link href="/" className="logo">ديارك</Link>
            <nav className="nav" aria-label="التنقل الرئيسي">
              <Link href="/search?transactionIntent=Sell">شراء</Link>
              <Link href="/search?transactionIntent=Rent">إيجار</Link>
              <Link href="/search?propertyCategory=CommercialProperty">تجاري</Link>
              <Link href="/projects">مشاريع</Link>
              <Link href="/prices">أسعار</Link>
              <Link href="/contractors">مقاولون</Link>
            </nav>
            <ThemeToggle />
            <Link className="button header-cta desktop-only" href={signedIn ? "/my" : "/login"}>
              {signedIn ? "إعلاناتي" : "أضف إعلانك"}
            </Link>
            <MobileNav signedIn={signedIn} />
          </div>
        </header>
        <ServiceWorkerRegistration />
        <CompareBar />
        <main id="main-content">{children}</main>
        <BackToTop />
        <footer className="site-footer">
          <div className="container footer-grid">
            <nav className="footer-col" aria-label="تصفح العقارات">
              <h3 className="footer-heading">تصفح العقارات</h3>
              <Link href="/search?transactionIntent=Sell">شراء</Link>
              <Link href="/search?transactionIntent=Rent">إيجار</Link>
              <Link href="/search?propertyCategory=CommercialProperty">عقارات تجارية</Link>
              <Link href="/projects">مشاريع على المخطط</Link>
            </nav>
            <nav className="footer-col" aria-label="خدمات">
              <h3 className="footer-heading">خدمات</h3>
              <Link href="/prices">أسعار العقارات</Link>
              <Link href="/contractors">دليل المقاولين</Link>
              <Link href="/tools">أدوات عقارية</Link>
              <Link href="/compare">مقارنة الإعلانات</Link>
            </nav>
            <nav className="footer-col" aria-label="ديارك">
              <h3 className="footer-heading">ديارك</h3>
              <Link href="/faq">الأسئلة الشائعة</Link>
              <Link href="/contact">تواصل معنا</Link>
            </nav>
          </div>
          <div className="container footer-bottom">
            <span>© {new Date().getFullYear()} ديارك — منصة العقارات في سوريا</span>
          </div>
        </footer>
      </body>
    </html>
  );
}
