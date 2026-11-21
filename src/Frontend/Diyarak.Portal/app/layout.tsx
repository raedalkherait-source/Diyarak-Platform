import type { Metadata, Viewport } from "next";
import Link from "next/link";
import { ServiceWorkerRegistration } from "@/components/ServiceWorkerRegistration";
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
      </head>
      <body>
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
            <Link className="button header-cta" href={signedIn ? "/my" : "/login"}>
              {signedIn ? "إعلاناتي" : "أضف إعلانك"}
            </Link>
          </div>
        </header>
        <ServiceWorkerRegistration />
        <main>{children}</main>
        <footer className="site-footer">
          <div className="container">© ديارك — منصة العقارات في سوريا</div>
        </footer>
      </body>
    </html>
  );
}
