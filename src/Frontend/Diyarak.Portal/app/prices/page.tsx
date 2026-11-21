import type { Metadata } from "next";
import Link from "next/link";
import { getPriceReport, type PriceReportEntry } from "@/lib/api";
import { governorateLabels, categoryLabels, transactionIntentLabels, label } from "@/lib/labels";

export const metadata: Metadata = {
  title: "أسعار العقارات في سوريا",
  description: "متوسط أسعار العقارات حسب المحافظة والمنطقة في سوريا — شقق، بيوت، أراضي ومحلات تجارية.",
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
    <div className="container" style={{ paddingBlock: "2rem" }}>
      <h1 style={{ marginBottom: "0.5rem" }}>
        {isDistrict
          ? `أسعار العقارات في ${label(governorateLabels, governorate)}`
          : "أسعار العقارات حسب المحافظة"}
      </h1>
      <p style={{ color: "var(--muted)", marginBottom: "1.5rem" }}>
        {isDistrict
          ? "متوسط الأسعار حسب المنطقة — بناءً على الإعلانات المنشورة حالياً."
          : "متوسط الأسعار حسب المحافظة — بناءً على الإعلانات المنشورة حالياً. اضغط على المحافظة لعرض التفاصيل حسب المنطقة."}
      </p>

      {isDistrict && (
        <p style={{ marginBottom: "1.5rem" }}>
          <Link href="/prices">← جميع المحافظات</Link>
        </p>
      )}

      {!isDistrict && (
        <nav style={{ display: "flex", flexWrap: "wrap", gap: "0.5rem", marginBottom: "1.5rem" }}>
          {governorates.map((g) => (
            <Link
              key={g}
              href={`/prices?governorate=${g}`}
              className="button"
              style={{ fontSize: "0.875rem" }}
            >
              {label(governorateLabels, g)}
            </Link>
          ))}
        </nav>
      )}

      {entries.length === 0 ? (
        <p style={{ textAlign: "center", padding: "3rem 0", color: "var(--muted)" }}>
          لا تتوفر بيانات كافية لعرض الأسعار حالياً. يلزم 3 إعلانات على الأقل لكل فئة.
        </p>
      ) : (
        [...grouped.entries()].map(([area, areaEntries]) => (
          <section key={area} style={{ marginBottom: "2rem" }}>
            <h2 style={{ fontSize: "1.25rem", marginBottom: "0.75rem" }}>
              {isDistrict ? area : (
                <Link href={`/prices?governorate=${area}`}>
                  {label(governorateLabels, area)}
                </Link>
              )}
            </h2>
            <div style={{ overflowX: "auto" }}>
              <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "0.875rem" }}>
                <thead>
                  <tr style={{ borderBottom: "2px solid var(--border)" }}>
                    <th style={{ textAlign: "right", padding: "0.5rem" }}>نوع العقار</th>
                    <th style={{ textAlign: "right", padding: "0.5rem" }}>نوع المعاملة</th>
                    <th style={{ textAlign: "right", padding: "0.5rem" }}>عدد الإعلانات</th>
                    <th style={{ textAlign: "right", padding: "0.5rem" }}>متوسط السعر</th>
                    <th style={{ textAlign: "right", padding: "0.5rem" }}>أقل سعر</th>
                    <th style={{ textAlign: "right", padding: "0.5rem" }}>أعلى سعر</th>
                    <th style={{ textAlign: "right", padding: "0.5rem" }}>العملة</th>
                  </tr>
                </thead>
                <tbody>
                  {areaEntries.map((e, i) => (
                    <tr key={i} style={{ borderBottom: "1px solid var(--border)" }}>
                      <td style={{ padding: "0.5rem" }}>{label(categoryLabels, e.propertyCategory)}</td>
                      <td style={{ padding: "0.5rem" }}>{label(transactionIntentLabels, e.transactionIntent)}</td>
                      <td style={{ padding: "0.5rem" }}>{fmt.format(e.listingCount)}</td>
                      <td style={{ padding: "0.5rem" }}>{fmt.format(e.averagePrice)}</td>
                      <td style={{ padding: "0.5rem" }}>{fmt.format(e.minPrice)}</td>
                      <td style={{ padding: "0.5rem" }}>{fmt.format(e.maxPrice)}</td>
                      <td style={{ padding: "0.5rem" }}>{e.currency}</td>
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
