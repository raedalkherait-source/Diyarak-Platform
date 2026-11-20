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
}

export interface PublicListingPage {
  items: PublicListing[];
  page: number;
  pageSize: number;
  hasMore: boolean;
}
