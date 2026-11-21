export type TransactionIntent = "Rent" | "Sell" | "RentForLimitedPeriod";
export type PublishingRole = "Owner" | "Tenant" | "ProfessionalOrAgent";

export interface PublicListingPhoto {
  photoId: string;
  url: string;
}

export interface PublicListingArea {
  value: number;
  unit: string;
}

export interface PublicListing {
  listingId: string;
  context: { publishingRole: PublishingRole; transactionIntent: TransactionIntent };
  headline: string;
  price: { isOnRequest: boolean; amount: number | null; currency: string | null };
  property: {
    category: string;
    location: {
      city: string;
      postalCode: string | null;
      governorate: string | null;
      district: string | null;
      approximate: { latitude: number; longitude: number; radiusMeters: number } | null;
    };
    livingArea: PublicListingArea | null;
    totalRooms: number | null;
    bedroomCount: number | null;
    bathroomCount: number | null;
    furnishingQuality: string | null;
    features: string[];
    constructionYear: number | null;
    commercialSubtype: string | null;
    parkingSpaceCount: number | null;
    legalTitleType: string | null;
    finishingLevel: string | null;
    floorNumber: number | null;
    buildingFloorCount: number | null;
  };
  availableFromDate: string | null;
  photos: PublicListingPhoto[];
  /** Which contact channels exist. The number itself is fetched on demand. */
  contact: { allowCalls: boolean; allowWhatsApp: boolean } | null;
  agency: { agencyId: string; name: string; verified: boolean } | null;
  /** A paid featured period is running (ADR-0071). Absent on single-listing responses. */
  featured?: boolean;
}

export interface PublicListingContact {
  phoneNumber: string;
  callUrl: string | null;
  whatsAppUrl: string | null;
}

export interface PublicListingPage {
  items: PublicListing[];
  page: number;
  pageSize: number;
  hasMore: boolean;
}
