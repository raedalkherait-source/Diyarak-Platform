/** Arabic display labels for API enum values. Unknown values fall back to the raw value. */

export const governorateLabels: Record<string, string> = {
  Damascus: "دمشق",
  RifDimashq: "ريف دمشق",
  Aleppo: "حلب",
  Homs: "حمص",
  Hama: "حماة",
  Latakia: "اللاذقية",
  Tartus: "طرطوس",
  Idlib: "إدلب",
  Daraa: "درعا",
  AsSuwayda: "السويداء",
  Quneitra: "القنيطرة",
  DeirEzZor: "دير الزور",
  Raqqa: "الرقة",
  AlHasakah: "الحسكة",
};

export const categoryLabels: Record<string, string> = {
  Apartment: "شقة",
  House: "بيت",
  Plot: "أرض",
  InvestmentProperty: "عقار استثماري",
  GarageOrParkingSpace: "كراج / موقف",
  CommercialProperty: "عقار تجاري",
};

export const commercialSubtypeLabels: Record<string, string> = {
  OfficeOrPractice: "مكتب / عيادة",
  RestaurantOrHotel: "مطعم / فندق",
  HallOrProduction: "مستودع / صالة إنتاج",
  Retail: "محل تجاري",
  CommercialPlot: "أرض تجارية",
};

export const transactionIntentLabels: Record<string, string> = {
  Sell: "للبيع",
  Rent: "للإيجار",
  RentForLimitedPeriod: "إيجار مؤقت",
};

export const publishingRoleLabels: Record<string, string> = {
  Owner: "المالك",
  Tenant: "المستأجر",
  ProfessionalOrAgent: "مكتب عقاري",
};

export const legalTitleLabels: Record<string, string> = {
  GreenTitleDeed: "طابو أخضر",
  AgriculturalTitleDeed: "طابو زراعي",
  TitleDeedShares: "طابو أسهم",
  CourtRuling: "حكم محكمة",
  NotarizedPowerOfAttorney: "وكالة كاتب عدل",
  HousingCooperativeAllocation: "جمعية سكنية / تخصص",
  KeyMoneyTenancy: "فروغ",
};

export const finishingLevelLabels: Record<string, string> = {
  Unfinished: "على العظم",
  PartiallyFinished: "إكساء جزئي",
  Standard: "إكساء عادي",
  Deluxe: "ديلوكس",
  SuperDeluxe: "سوبر ديلوكس",
};

export const featureLabels: Record<string, string> = {
  FittedKitchen: "مطبخ مجهز",
  Elevator: "مصعد",
  BalconyOrTerrace: "شرفة / تراس",
  GuestToilet: "حمام ضيوف",
  GardenOrSharedGardenUse: "حديقة",
  Basement: "قبو",
  StepFreeAccess: "مدخل بدون درج",
  SuitableForVacationRental: "مناسب للإيجار السياحي",
  HistoricMonument: "بناء تراثي",
  GarageOrParkingSpace: "كراج / موقف",
  SolarPowerSystem: "طاقة شمسية",
  GeneratorSubscription: "اشتراك أمبير",
  WaterTank: "خزان مياه",
  WaterWell: "بئر",
  CentralHeating: "تدفئة مركزية",
  AirConditioning: "تكييف",
  Furnished: "مفروش",
  SecurityGuard: "حارس",
};

export const constructionStatusLabels: Record<string, string> = {
  Planned: "مخطط",
  UnderConstruction: "قيد الإنشاء",
  NearCompletion: "قرب الانتهاء",
};

export const areaUnitLabels: Record<string, string> = {
  SquareMeter: "م²",
  SquareFoot: "قدم²",
  Hectare: "هكتار",
  Dunum: "دونم",
};

export function label(map: Record<string, string>, value: string | null | undefined): string {
  if (!value) return "";
  return map[value] ?? value;
}
