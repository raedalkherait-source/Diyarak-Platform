import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "تقدير قيمة العقار",
  description: "قدّر قيمة عقارك بناءً على المحافظة والمنطقة ونوع العقار — أداة مجانية من ديارك.",
  alternates: { canonical: "/tools/estimator" },
  openGraph: {
    title: "تقدير قيمة العقار — ديارك",
    description: "قدّر قيمة عقارك بناءً على بيانات السوق.",
  },
};

export default function EstimatorLayout({ children }: { children: React.ReactNode }) {
  return children;
}
