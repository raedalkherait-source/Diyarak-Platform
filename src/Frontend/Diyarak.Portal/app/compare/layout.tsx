import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "مقارنة الإعلانات",
  description: "قارن بين الإعلانات العقارية جنباً إلى جنب — الأسعار والمساحات والمزايا.",
  alternates: { canonical: "/compare" },
  openGraph: {
    title: "مقارنة الإعلانات — ديارك",
    description: "قارن بين الإعلانات العقارية جنباً إلى جنب.",
  },
};

export default function CompareLayout({ children }: { children: React.ReactNode }) {
  return children;
}
