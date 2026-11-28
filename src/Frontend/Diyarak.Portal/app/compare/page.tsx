"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useCurrency, convertPrice } from "@/components/CurrencyToggle";

interface CompareItem {
  id: string;
  headline: string;
  price: string;
}

interface ListingDetail {
  listingId: string;
  headline: string;
  price: { isOnRequest: boolean; amount: number | null; currency: string | null };
  context: { transactionIntent: string };
  property: {
    category: string;
    location: { city: string; governorate: string | null; district: string | null };
    livingArea: { value: number; unit: string } | null;
    totalRooms: number | null;
    bedroomCount: number | null;
    bathroomCount: number | null;
    floorNumber: number | null;
    buildingFloorCount: number | null;
    legalTitleType: string | null;
    finishingLevel: string | null;
    features: string[];
    constructionYear: number | null;
    parkingSpaceCount: number | null;
  };
  photos: Array<{ photoId: string; url: string }>;
}

const labels: Record<string, string> = {
  Sell: "للبيع", Rent: "للإيجار", RentForLimitedPeriod: "إيجار مؤقت",
  Apartment: "شقة", House: "بيت", Plot: "أرض", InvestmentProperty: "عقار استثماري",
  CommercialProperty: "عقار تجاري", GarageOrParkingSpace: "كراج / موقف",
  GreenTitleDeed: "طابو أخضر", AgriculturalTitleDeed: "طابو زراعي",
  TitleDeedShares: "طابو أسهم", CourtRuling: "حكم محكمة",
  NotarizedPowerOfAttorney: "وكالة كاتب عدل", HousingCooperativeAllocation: "جمعية سكنية",
  KeyMoneyTenancy: "فروغ",
  Unfinished: "على العظم", PartiallyFinished: "إكساء جزئي",
  Standard: "إكساء عادي", Deluxe: "ديلوكس", SuperDeluxe: "سوبر ديلوكس",
  Damascus: "دمشق", RifDimashq: "ريف دمشق", Aleppo: "حلب", Homs: "حمص",
  Hama: "حماة", Latakia: "اللاذقية", Tartus: "طرطوس", Idlib: "إدلب",
  Daraa: "درعا", AsSuwayda: "السويداء", Quneitra: "القنيطرة",
  DeirEzZor: "دير الزور", Raqqa: "الرقة", AlHasakah: "الحسكة",
};

const fmt = new Intl.NumberFormat("ar-SY-u-nu-latn", { maximumFractionDigits: 2 });

function l(value: string | null | undefined): string {
  if (!value) return "";
  return labels[value] ?? value;
}

function formatConvertedPrice(
  price: ListingDetail["price"],
  toCurrency: "USD" | "SYP",
  rate: number,
): string {
  if (price.isOnRequest || price.amount === null) return "السعر عند الاتصال";
  const converted = convertPrice(price.amount, price.currency, toCurrency, rate);
  if (converted.amount === null) return "السعر عند الاتصال";
  const symbol = converted.currency === "USD" ? "$" : converted.currency === "SYP" ? "ل.س" : converted.currency;
  return `${fmt.format(converted.amount)} ${symbol}`;
}

export default function ComparePage() {
  const [listings, setListings] = useState<ListingDetail[]>([]);
  const [loading, setLoading] = useState(true);
  const { currency, rate } = useCurrency();

  useEffect(() => {
    const stored: CompareItem[] = (() => {
      try {
        const raw = localStorage.getItem("diyarak_compare");
        return raw ? JSON.parse(raw) : [];
      } catch { return []; }
    })();

    if (stored.length === 0) {
      setLoading(false);
      return;
    }

    Promise.all(
      stored.map((item) =>
        fetch(`/api/public/listings/${item.id}`)
          .then((r) => (r.ok ? r.json() : null))
          .catch(() => null),
      ),
    ).then((results) => {
      setListings(results.filter(Boolean));
      setLoading(false);
    });
  }, []);

  if (loading) return <div className="container"><p>جارٍ التحميل...</p></div>;
  if (listings.length === 0) {
    return (
      <div className="container">
        <h1>مقارنة الإعلانات</h1>
        <p>لم تُضف أي إعلانات للمقارنة بعد.</p>
        <Link href="/search" className="button">تصفح الإعلانات</Link>
      </div>
    );
  }

  const rows: Array<{ label: string; values: (listing: ListingDetail) => string }> = [
    { label: "السعر", values: (li) => formatConvertedPrice(li.price, currency, rate) },
    { label: "نوع العرض", values: (li) => l(li.context.transactionIntent) },
    { label: "نوع العقار", values: (li) => l(li.property.category) },
    { label: "الموقع", values: (li) => [li.property.location.district, l(li.property.location.governorate)].filter(Boolean).join(", ") },
    { label: "المساحة", values: (li) => li.property.livingArea ? `${fmt.format(li.property.livingArea.value)} م²` : "—" },
    { label: "غرف النوم", values: (li) => li.property.bedroomCount !== null ? String(li.property.bedroomCount) : "—" },
    { label: "الحمامات", values: (li) => li.property.bathroomCount !== null ? String(li.property.bathroomCount) : "—" },
    { label: "عدد الغرف", values: (li) => li.property.totalRooms !== null ? String(li.property.totalRooms) : "—" },
    { label: "الطابق", values: (li) => li.property.floorNumber !== null ? String(li.property.floorNumber) : "—" },
    { label: "الوضع القانوني", values: (li) => l(li.property.legalTitleType) || "—" },
    { label: "الإكساء", values: (li) => l(li.property.finishingLevel) || "—" },
    { label: "سنة البناء", values: (li) => li.property.constructionYear !== null ? String(li.property.constructionYear) : "—" },
    { label: "مواقف", values: (li) => li.property.parkingSpaceCount !== null ? String(li.property.parkingSpaceCount) : "—" },
    { label: "المزايا", values: (li) => li.property.features.length > 0 ? li.property.features.map((f) => l(f) || f).join("، ") : "—" },
  ];

  return (
    <div className="container section">
      <nav className="breadcrumbs" aria-label="مسار التنقل">
        <Link href="/">الرئيسية</Link>
        <span>›</span>
        <span>مقارنة الإعلانات</span>
      </nav>
      <h1>مقارنة الإعلانات</h1>
      <div className="compare-table-wrapper">
        <table className="compare-table">
          <caption className="sr-only">مقارنة الإعلانات</caption>
          <thead>
            <tr>
              <th></th>
              {listings.map((li) => (
                <th key={li.listingId}>
                  <Link href={`/listings/${li.listingId}`}>{li.headline}</Link>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.label}>
                <td className="compare-label">{row.label}</td>
                {listings.map((li) => (
                  <td key={li.listingId}>{row.values(li)}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
