import type { Metadata } from "next";
import Link from "next/link";
import { getPriceReport, type PriceReportEntry } from "@/lib/api";
import { governorateLabels, categoryLabels, transactionIntentLabels, label } from "@/lib/labels";
import { breadcrumbStructuredData } from "@/lib/seo";
import { jsonForScript } from "@/lib/site";

export const metadata: Metadata = {
  title: "أسعار العقارات في سوريا",
  description: "متوسط أسعار العقارات حسب المحافظة والمنطقة في سوريا — شقق، بيوت، أراضي ومحلات تجارية.",
  alternates: { canonical: "/prices" },
  openGraph: {
    title: "أسعار العقارات في سوريا — ديارك",
    description: "متوسط أسعار العقارات حسب المحافظة والمنطقة في سوريا.",
  },
};

const fmt = new Intl.NumberFormat("ar-SY-u-nu-latn", { maximumFractionDigits: 0 });

function groupByArea(entries: PriceReportEntry[]): Map<string, PriceReportEntry[]> {
  const map = new Map<string, PriceReportEntry[]>();
  for (const e of entries) {
    const list = map.get(e.area);
    if (list) list.push(e);
    else map.set(e.area, [e]);
  }
  return map;
}

export default async function PricesPage({
  searchParams,
}: {
  searchParams: Promise<{ governorate?: string }>;
}) {
  const { governorate } = await searchParams;
  const report = await getPriceReport(governorate);
  const entries = report?.entries ?? [];
  const grouped = groupByArea(entries);
  const isDistrict = !!governorate;
  const governorates = Object.keys(governorateLabels);

  return (
    <div className="container section">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: jsonForScript(breadcrumbStructuredData([
        { name: "الرئيسية", href: "/" },
        { name: "أسعار العقارات" },
      ])) }} />
      <nav className="breadcrumbs" aria-label="مسار التنقل">
        <Link href="/">الرئيسية</Link>
        <span>›</span>
        {isDistrict ? (
          <>
            <Link href="/prices">أسعار العقارات</Link>
            <span>›</span>
            <span>{label(governorateLabels, governorate)}</span>
          </>
        ) : (
          <span>أسعار العقارات</span>
        )}
      </nav>

      <h1>
        {isDistrict
          ? `أسعار العقارات في ${label(governorateLabels, governorate)}`
          : "أسعار العقارات حسب المحافظة"}
      </h1>
      <p className="notice">
        {isDistrict
          ? "متوسط الأسعار حسب المنطقة — بناءً على الإعلانات المنشورة حالياً."
          : "متوسط الأسعار حسب المحافظة — بناءً على الإعلانات المنشورة حالياً. اضغط على المحافظة لعرض التفاصيل حسب المنطقة."}
      </p>

      {isDistrict && (
        <p className="back-link">
          <Link href="/prices">← جميع المحافظات</Link>
        </p>
      )}

      {!isDistrict && (
        <div className="filter-chips">
          {governorates.map((g) => (
            <Link key={g} href={`/prices?governorate=${g}`} className="chip">
              {label(governorateLabels, g)}
            </Link>
          ))}
        </div>
      )}

      {entries.length === 0 ? (
        <div className="empty">
          لا تتوفر بيانات كافية لعرض الأسعار حالياً. يلزم 3 إعلانات على الأقل لكل فئة.
        </div>
      ) : (
        [...grouped.entries()].map(([area, areaEntries]) => (
          <section key={area} className="price-section">
            <h2 className="price-area-title">
              {isDistrict ? area : (
                <Link href={`/prices?governorate=${area}`}>
                  {label(governorateLabels, area)}
                </Link>
              )}
            </h2>
            <div className="table-wrap">
              <table className="price-table">
                <thead>
                  <tr>
                    <th>نوع العقار</th>
                    <th>نوع المعاملة</th>
                    <th>عدد الإعلانات</th>
                    <th>متوسط السعر</th>
                    <th>أقل سعر</th>
                    <th>أعلى سعر</th>
                    <th>العملة</th>
                  </tr>
                </thead>
                <tbody>
                  {areaEntries.map((e, i) => (
                    <tr key={i}>
                      <td>{label(categoryLabels, e.propertyCategory)}</td>
                      <td>{label(transactionIntentLabels, e.transactionIntent)}</td>
                      <td>{fmt.format(e.listingCount)}</td>
                      <td className="price-avg">{fmt.format(e.averagePrice)}</td>
                      <td>{fmt.format(e.minPrice)}</td>
                      <td>{fmt.format(e.maxPrice)}</td>
                      <td>{e.currency}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        ))
      )}
    </div>
  );
}
