import type { Metadata } from "next";
import Link from "next/link";
import { FiltersDisclosure } from "@/components/FiltersDisclosure";
import { ListingCard } from "@/components/ListingCard";
import { ResultsMap } from "@/components/ListingMaps";
import { SaveSearchButton } from "@/components/SaveSearchButton";
import { getSessionToken } from "@/lib/session";
import { mapConfig } from "@/lib/map";
import { Options } from "@/components/Options";
import { ApiUnavailableError, searchListings } from "@/lib/api";
import { breadcrumbStructuredData } from "@/lib/seo";
import { jsonForScript } from "@/lib/site";
import {
  categoryLabels,
  finishingLevelLabels,
  governorateLabels,
  label,
  legalTitleLabels,
  transactionIntentLabels,
} from "@/lib/labels";
import { isIndexableSearch } from "@/lib/seo";
import { firstValue, pageHref, parseBbox, savedSearchHref, savedSearchQuery, sortOptions, toApiQuery, type RawSearchParams } from "@/lib/search";
import { SearchRecorder } from "@/components/SearchHistory";
import { CurrencyToggle } from "@/components/CurrencyToggle";
import type { PublicListingPage } from "@/lib/types";

type Props = { searchParams: Promise<RawSearchParams> };

function describe(params: RawSearchParams): string {
  const intent = label(transactionIntentLabels, firstValue(params.transactionIntent));
  const category = label(categoryLabels, firstValue(params.propertyCategory)) || "عقارات";
  const place = firstValue(params.district) || label(governorateLabels, firstValue(params.governorate)) || "سوريا";
  if (parseBbox(firstValue(params.bbox))) return [category, intent, "ضمن منطقة على الخريطة"].filter(Boolean).join(" ");
  return [category, intent, "في", place].filter(Boolean).join(" ");
}

export async function generateMetadata({ searchParams }: Props): Promise<Metadata> {
  const params = await searchParams;
  const title = describe(params);
  return {
    title,
    openGraph: { title: `${title} — ديارك`, description: "ابحث عن عقارات للبيع والإيجار في جميع المحافظات السورية." },
    // Only simple landing combinations are indexed; deep filter combinations stay crawlable but unindexed.
    robots: isIndexableSearch(params) ? undefined : { index: false, follow: true },
  };
}

export default async function SearchPage({ searchParams }: Props) {
  const params = await searchParams;
  const query = toApiQuery(params);
  const page = Number(query.get("page"));

  let result: PublicListingPage | null = null;
  let unavailable = false;

  try {
    result = await searchListings(query);
  } catch (error) {
    if (!(error instanceof ApiUnavailableError)) throw error;
    unavailable = true;
  }

  const value = (key: string) => firstValue(params[key]);
  const activeFilters = [...new Set([...query.keys()])].filter(
    (key) => !["page", "pageSize", "priceCurrency", "livingAreaUnit"].includes(key),
  ).length;

  const searchQuery = new URLSearchParams(
    Object.entries(params).filter(([, v]) => v !== undefined).map(([k, v]) => [k, String(v)]),
  ).toString();

  return (
    <div className="container search-layout">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: jsonForScript(breadcrumbStructuredData([
        { name: "الرئيسية", href: "/" },
        { name: "البحث" },
      ])) }} />
      <nav className="breadcrumbs" aria-label="مسار التنقل">
        <Link href="/">الرئيسية</Link>
        <span>›</span>
        <span>البحث</span>
      </nav>
      {activeFilters > 0 && <SearchRecorder query={searchQuery} label={describe(params)} />}
      <FiltersDisclosure activeCount={activeFilters}>
      <form className="filters" action="/search" method="get" aria-label="تصفية النتائج">
        <h2>تصفية النتائج</h2>
        <label className="field">
          نوع العرض
          <select name="transactionIntent" defaultValue={value("transactionIntent")}>
            <Options labels={transactionIntentLabels} placeholder="الكل" />
          </select>
        </label>
        <label className="field">
          نوع العقار
          <select name="propertyCategory" defaultValue={value("propertyCategory")}>
            <Options labels={categoryLabels} placeholder="الكل" />
          </select>
        </label>
        <label className="field">
          المحافظة
          <select name="governorate" defaultValue={value("governorate")}>
            <Options labels={governorateLabels} placeholder="كل المحافظات" />
          </select>
        </label>
        <label className="field">
          الحي / المنطقة
          <input name="district" defaultValue={value("district")} maxLength={100} />
        </label>
        <div className="row">
          <label className="field">
            السعر من
            <input name="priceMin" inputMode="numeric" defaultValue={value("priceMin")} />
          </label>
          <label className="field">
            إلى
            <input name="priceMax" inputMode="numeric" defaultValue={value("priceMax")} />
          </label>
        </div>
        <label className="field">
          العملة
          <select name="priceCurrency" defaultValue={value("priceCurrency") || "USD"}>
            <option value="USD">دولار أمريكي</option>
            <option value="SYP">ليرة سورية</option>
          </select>
        </label>
        <label className="field">
          الوضع القانوني
          <select name="legalTitleType" defaultValue={value("legalTitleType")}>
            <Options labels={legalTitleLabels} placeholder="الكل" />
          </select>
        </label>
        <label className="field">
          الإكساء
          <select name="finishingLevel" defaultValue={value("finishingLevel")}>
            <Options labels={finishingLevelLabels} placeholder="الكل" />
          </select>
        </label>
        <label className="field">
          غرف النوم (على الأقل)
          <input name="bedroomCountMin" inputMode="numeric" defaultValue={value("bedroomCountMin")} />
        </label>
        <div className="row">
          <label className="field">
            المساحة من (م²)
            <input name="livingAreaMin" inputMode="numeric" defaultValue={value("livingAreaMin")} />
          </label>
          <label className="field">
            إلى
            <input name="livingAreaMax" inputMode="numeric" defaultValue={value("livingAreaMax")} />
          </label>
        </div>
        <label className="field">
          الترتيب
          <select name="sort" defaultValue={value("sort")}>
            {Object.entries(sortOptions).map(([k, v]) => (
              <option key={k} value={k}>{v}</option>
            ))}
          </select>
        </label>
        <button className="button" type="submit">تطبيق</button>
        <Link className="button secondary" href="/search">مسح الفلاتر</Link>
      </form>
      </FiltersDisclosure>

      <section aria-live="polite">
        <div className="results-header">
          <h1>{describe(params)}</h1>
          <CurrencyToggle />
          {result && result.items.length > 0 ? (
            <span className="results-count">
              {page > 1 ? `الصفحة ${page} — ` : ""}
              {result.items.length}{result.hasMore ? "+" : ""} نتيجة
            </span>
          ) : page > 1 ? <span className="notice">الصفحة {page}</span> : null}
          <SaveSearchButton
            query={savedSearchQuery(params)}
            name={describe(params)}
            signedIn={Boolean(await getSessionToken())}
            returnTo={savedSearchHref(savedSearchQuery(params))}
          />
        </div>

        {unavailable ? (
          <div className="empty">تعذر تحميل النتائج حالياً. حاول مرة أخرى بعد قليل.</div>
        ) : !result ? (
          <div className="empty">قيم البحث غير صالحة. عدّل الفلاتر وحاول مجدداً.</div>
        ) : result.items.length === 0 ? (
          <>
            {parseBbox(firstValue(params.bbox)) ? (
              <ResultsMap config={mapConfig()} markers={[]} query={savedSearchQuery(params)} area={parseBbox(firstValue(params.bbox))} />
            ) : null}
            <div className="empty">لا توجد نتائج مطابقة. جرّب توسيع البحث.</div>
          </>
        ) : (
          <>
            {mapMarkers(result).length > 0 || parseBbox(firstValue(params.bbox)) ? (
              <ResultsMap
                config={mapConfig()}
                markers={mapMarkers(result)}
                query={savedSearchQuery(params)}
                area={parseBbox(firstValue(params.bbox))}
              />
            ) : null}
            <div className="grid">
              {result.items.map((listing) => (
                <ListingCard key={listing.listingId} listing={listing} />
              ))}
            </div>
            <nav className="pagination" aria-label="الصفحات">
              {page > 1 ? <Link className="button secondary" href={pageHref(params, page - 1)}>→ السابق</Link> : null}
              {Array.from({ length: Math.min(page + (result.hasMore ? 2 : 0), 5) }, (_, i) => {
                const start = Math.max(1, page - 2);
                const p = start + i;
                if (p > page && !result.hasMore) return null;
                if (p > page + 1) return null;
                return (
                  <Link key={p} className={`button ${p === page ? "" : "secondary"} pagination-num`} href={pageHref(params, p)} aria-current={p === page ? "page" : undefined}>
                    {p}
                  </Link>
                );
              })}
              {result.hasMore ? <Link className="button secondary" href={pageHref(params, page + 1)}>التالي ←</Link> : null}
            </nav>
          </>
        )}
      </section>
    </div>
  );
}

function mapMarkers(result: PublicListingPage) {
  return result.items.flatMap((listing) =>
    listing.property.location.approximate
      ? [{
          latitude: listing.property.location.approximate.latitude,
          longitude: listing.property.location.approximate.longitude,
          href: `/listings/${listing.listingId}`,
          label: listing.headline,
        }]
      : [],
  );
}
