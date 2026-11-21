import type { Metadata } from "next";
import Link from "next/link";
import { listContractors } from "@/lib/api";
import { governorateLabels, contractorSpecialtyLabels, label } from "@/lib/labels";

export const metadata: Metadata = {
  title: "دليل المقاولين في سوريا",
  description: "ابحث عن مقاولين ومتعهدين في جميع المحافظات السورية — بناء، كهرباء، سباكة، تصميم داخلي وأكثر.",
};

const specialties = Object.keys(contractorSpecialtyLabels);
const governorates = Object.keys(governorateLabels);

export default async function ContractorsPage({
  searchParams,
}: {
  searchParams: Promise<{ page?: string; governorate?: string; specialty?: string }>;
}) {
  const { page: pageStr, governorate, specialty } = await searchParams;
  const page = Math.max(1, parseInt(pageStr ?? "1", 10) || 1);
  const data = await listContractors(page, governorate, specialty);
  const items = data?.items ?? [];
  const hasMore = data?.hasMore ?? false;

  return (
    <div className="container" style={{ paddingBlock: "2rem" }}>
      <h1 style={{ marginBottom: "0.5rem" }}>دليل المقاولين</h1>
      <p style={{ color: "var(--muted)", marginBottom: "1.5rem" }}>
        تصفح مقاولين ومتعهدين في جميع المحافظات السورية.
      </p>

      <div style={{ display: "flex", flexWrap: "wrap", gap: "0.5rem", marginBottom: "1rem" }}>
        <Link
          href="/contractors"
          className="button"
          style={{ fontSize: "0.875rem", fontWeight: !governorate && !specialty ? "bold" : "normal" }}
        >
          الكل
        </Link>
        {governorates.map((g) => (
          <Link
            key={g}
            href={`/contractors?governorate=${g}${specialty ? `&specialty=${specialty}` : ""}`}
            className="button"
            style={{ fontSize: "0.875rem", fontWeight: governorate === g ? "bold" : "normal" }}
          >
            {label(governorateLabels, g)}
          </Link>
        ))}
      </div>

      <div style={{ display: "flex", flexWrap: "wrap", gap: "0.5rem", marginBottom: "1.5rem" }}>
        {specialties.map((s) => (
          <Link
            key={s}
            href={`/contractors?specialty=${s}${governorate ? `&governorate=${governorate}` : ""}`}
            className="button"
            style={{ fontSize: "0.875rem", fontWeight: specialty === s ? "bold" : "normal" }}
          >
            {label(contractorSpecialtyLabels, s)}
          </Link>
        ))}
      </div>

      {items.length === 0 ? (
        <p style={{ textAlign: "center", padding: "3rem 0", color: "var(--muted)" }}>
          لا يوجد مقاولون مسجلون حالياً بهذا التصنيف.
        </p>
      ) : (
        <div style={{ display: "grid", gap: "1rem", gridTemplateColumns: "repeat(auto-fill, minmax(300px, 1fr))" }}>
          {items.map((c) => (
            <div
              key={c.id}
              style={{
                border: "1px solid var(--border)",
                borderRadius: "8px",
                padding: "1rem",
              }}
            >
              <h2 style={{ fontSize: "1.125rem", marginBottom: "0.25rem" }}>{c.name}</h2>
              <p style={{ fontSize: "0.875rem", color: "var(--muted)", marginBottom: "0.5rem" }}>
                {label(contractorSpecialtyLabels, c.specialty)} · {label(governorateLabels, c.governorate)}
                {c.district ? ` — ${c.district}` : ""}
              </p>
              {c.description && (
                <p style={{ fontSize: "0.875rem", marginBottom: "0.5rem", whiteSpace: "pre-line" }}>
                  {c.description}
                </p>
              )}
              <p style={{ fontSize: "0.875rem" }}>
                <a href={`tel:${c.phone}`} dir="ltr">{c.phone}</a>
              </p>
            </div>
          ))}
        </div>
      )}

      {(page > 1 || hasMore) && (
        <nav style={{ display: "flex", justifyContent: "center", gap: "1rem", marginTop: "2rem" }}>
          {page > 1 && (
            <Link
              href={`/contractors?page=${page - 1}${governorate ? `&governorate=${governorate}` : ""}${specialty ? `&specialty=${specialty}` : ""}`}
              className="button"
            >
              ← السابق
            </Link>
          )}
          {hasMore && (
            <Link
              href={`/contractors?page=${page + 1}${governorate ? `&governorate=${governorate}` : ""}${specialty ? `&specialty=${specialty}` : ""}`}
              className="button"
            >
              التالي →
            </Link>
          )}
        </nav>
      )}
    </div>
  );
}
