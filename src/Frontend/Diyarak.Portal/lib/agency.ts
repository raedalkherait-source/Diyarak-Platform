/** Agency shapes returned by the API (ADR-0068). */

export interface OwnerAgency {
  agencyId: string;
  version: number;
  name: string;
  description: string | null;
  governorate: string | null;
  city: string;
  verificationStatus: "None" | "Pending" | "Verified" | "Rejected";
  registrationNumber: string | null;
  verificationNote: string | null;
  verifiedAt: string | null;
}

export interface PublicAgencyProfile {
  agencyId: string;
  name: string;
  description: string | null;
  governorate: string | null;
  city: string;
  verified: boolean;
  verifiedAt: string | null;
}

export const verificationStatusLabels: Record<OwnerAgency["verificationStatus"], string> = {
  None: "غير موثّق",
  Pending: "طلب التوثيق قيد المراجعة",
  Verified: "مكتب موثّق",
  Rejected: "رُفض طلب التوثيق",
};

export function isAgencyId(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
}
