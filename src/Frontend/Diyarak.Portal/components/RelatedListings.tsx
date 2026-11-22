import { searchListings } from "@/lib/api";
import { ListingCard } from "./ListingCard";

interface RelatedListingsProps {
  listingId: string;
  governorate: string | null;
  category: string;
  transactionIntent: string;
}

export async function RelatedListings({ listingId, governorate, category, transactionIntent }: RelatedListingsProps) {
  if (!governorate) return null;

  const params = new URLSearchParams({
    governorate,
    propertyCategory: category,
    transactionIntent,
    pageSize: "5",
  });

  const result = await searchListings(params);
  const related = (result?.items ?? []).filter((l) => l.listingId !== listingId).slice(0, 4);
  if (related.length === 0) return null;

  return (
    <section className="panel related-listings">
      <h2>إعلانات مشابهة</h2>
      <div className="related-grid">
        {related.map((listing) => (
          <ListingCard key={listing.listingId} listing={listing} />
        ))}
      </div>
    </section>
  );
}
