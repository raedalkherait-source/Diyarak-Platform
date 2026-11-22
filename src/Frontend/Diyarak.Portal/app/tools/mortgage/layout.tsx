import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "حاسبة الأقساط الشهرية",
  description: "احسب قسطك الشهري للتمويل العقاري — أداة مجانية من ديارك.",
  alternates: { canonical: "/tools/mortgage" },
  openGraph: {
    title: "حاسبة الأقساط الشهرية — ديارك",
    description: "احسب قسطك الشهري للتمويل العقاري.",
  },
};

export default function MortgageLayout({ children }: { children: React.ReactNode }) {
  return children;
}
