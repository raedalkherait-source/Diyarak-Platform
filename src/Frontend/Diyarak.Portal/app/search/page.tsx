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
import {
  categoryLabels,
  finishingLevelLabels,
  governorateLabels,
  label,
  legalTitleLabels,
  transactionIntentLabels,
} from "@/lib/labels";
import { isIndexableSearch } from "@/lib/seo";
import { firstValue, pageHref, savedSearchHref, savedSearchQuery, toApiQuery, type RawSearchParams } from "@/lib/search";
import type { PublicListingPage } from "@/lib/types";

type Props = { searchParams: Promise<RawSearchParams> };

function describe(params: RawSearchParams): string {
  const intent = label(transactionIntentLabels, firstValue(params.transactionIntent));
  const category = label(categoryLabels, firstValue(params.propertyCategory)) || "عقارات";
  const place = firstValue(params.district) || label(governorateLabels, firstValue(params.governorate)) || "سوريا";
  return [category, intent, "في", place].filter(Boolean).join(" ");
}

export async function generateMetadata({ searchParams }: Props): Promise<Metadata> {
  const params = await searchParams;
  return {
    title: describe(params),
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

  return (
    <div className="container search-layout">
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
        <button className="button" type="submit">تطبيق</button>
        <Link className="button secondary" href="/search">مسح الفلاتر</Link>
      </form>
      </FiltersDisclosure>

      <section aria-live="polite">
        <div className="results-header">
          <h1>{describe(params)}</h1>
          {page > 1 ? <span className="notice">الصفحة {page}</span> : null}
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
          <div className="empty">لا توجد نتائج مطابقة. جرّب توسيع البحث.</div>
        ) : (
          <>
            {mapMarkers(result).length > 0 ? <ResultsMap config={mapConfig()} markers={mapMarkers(result)} /> : null}
            <div className="grid">
              {result.items.map((listing) => (
                <ListingCard key={listing.listingId} listing={listing} />
              ))}
            </div>
            <nav className="pagination" aria-label="الصفحات">
              {page > 1 ? <Link className="button secondary" href={pageHref(params, page - 1)}>→ السابق</Link> : null}
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
