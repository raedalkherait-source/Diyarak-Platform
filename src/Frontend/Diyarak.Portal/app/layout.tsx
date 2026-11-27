import type { Metadata, Viewport } from "next";
import Link from "next/link";
import { NavLink } from "@/components/NavLink";
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
  openGraph: {
    type: "website",
    siteName: "ديارك",
    locale: "ar_SY",
  },
  twitter: {
    card: "summary_large_image",
    site: "@diyarak_app",
  },
};

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  viewportFit: "cover",
  themeColor: "#0f6b5c",
};

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  const signedIn = (await getSessionToken()) !== null;

  return (
    <html lang="ar" dir="rtl">
      <head>
        <meta name="color-scheme" content="light dark" />
        <link rel="apple-touch-icon" href="/icon-192.png" />
        <link rel="dns-prefetch" href="https://tile.openstreetmap.org" />
        <link rel="preconnect" href="https://tile.openstreetmap.org" crossOrigin="anonymous" />
      </head>
      <body>
        <noscript><div className="noscript-banner">يُرجى تفعيل JavaScript لاستخدام جميع ميزات ديارك.</div></noscript>
        <a href="#main-content" className="skip-link">تخطي إلى المحتوى</a>
        <header className="site-header">
          <div className="container">
            <Link href="/" className="logo">ديارك</Link>
            <nav className="nav" aria-label="التنقل الرئيسي">
              <Link href="/search?transactionIntent=Sell">شراء</Link>
              <Link href="/search?transactionIntent=Rent">إيجار</Link>
              <Link href="/search?propertyCategory=CommercialProperty">تجاري</Link>
              <NavLink href="/projects">مشاريع</NavLink>
              <NavLink href="/prices">أسعار</NavLink>
              <NavLink href="/contractors">مقاولون</NavLink>
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
        <main id="main-content" tabIndex={-1}>{children}</main>
        <BackToTop />
        <footer className="site-footer">
          <div className="container footer-grid">
            <nav className="footer-col" aria-label="تصفح العقارات">
              <h3 className="footer-heading">تصفح العقارات</h3>
              <Link href="/search?transactionIntent=Sell">شراء</Link>
              <Link href="/search?transactionIntent=Rent">إيجار</Link>
              <Link href="/search?propertyCategory=CommercialProperty">عقارات تجارية</Link>
              <NavLink href="/projects">مشاريع على المخطط</NavLink>
            </nav>
            <nav className="footer-col" aria-label="خدمات">
              <h3 className="footer-heading">خدمات</h3>
              <NavLink href="/prices">أسعار العقارات</NavLink>
              <NavLink href="/contractors">دليل المقاولين</NavLink>
              <NavLink href="/tools">أدوات عقارية</NavLink>
              <NavLink href="/compare">مقارنة الإعلانات</NavLink>
            </nav>
            <nav className="footer-col" aria-label="ديارك">
              <h3 className="footer-heading">ديارك</h3>
              <NavLink href="/faq">الأسئلة الشائعة</NavLink>
              <NavLink href="/contact">تواصل معنا</NavLink>
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
