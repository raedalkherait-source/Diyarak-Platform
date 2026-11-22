import Link from "next/link";
import { governorateLabels, label } from "@/lib/labels";

interface NeighborhoodInfoProps {
  governorate: string | null;
  district: string | null;
  transactionIntent: string;
  propertyCategory: string;
}

const governorateDescriptions: Record<string, string> = {
  Damascus: "العاصمة السورية — مركز تجاري وثقافي، أسعار عقارية مرتفعة نسبياً مع تنوع في الأحياء من الشعبية إلى الراقية.",
  RifDimashq: "محافظة واسعة تحيط بدمشق — أسعار أقل من العاصمة مع خيارات سكنية متنوعة ومشاريع جديدة.",
  Aleppo: "أكبر مدينة سورية صناعياً — سوق عقاري نشط مع إعادة إعمار وفرص استثمارية.",
  Homs: "وسط سوريا — أسعار معتدلة وموقع استراتيجي بين المحافظات.",
  Latakia: "على الساحل السوري — طلب مرتفع على العقارات السياحية والسكنية.",
  Tartus: "ساحلية هادئة — أسعار أقل من اللاذقية مع نمو عقاري مطّرد.",
  Hama: "مدينة تاريخية — سوق عقاري مستقر بأسعار معقولة.",
};

export function NeighborhoodInfo({ governorate, district, transactionIntent, propertyCategory }: NeighborhoodInfoProps) {
  if (!governorate) return null;
  const govLabel = label(governorateLabels, governorate);
  const description = governorateDescriptions[governorate];

  return (
    <section className="panel neighborhood-info">
      <h2>عن المنطقة</h2>
      {description ? <p>{description}</p> : null}
      <div className="neighborhood-links">
        <Link href={`/search?governorate=${encodeURIComponent(governorate)}&transactionIntent=${transactionIntent}`}>
          إعلانات أخرى في {govLabel}
        </Link>
        {district ? (
          <Link href={`/search?governorate=${encodeURIComponent(governorate)}&district=${encodeURIComponent(district)}&transactionIntent=${transactionIntent}`}>
            إعلانات في {district}
          </Link>
        ) : null}
        <Link href={`/prices?governorate=${encodeURIComponent(governorate)}`}>
          أسعار العقارات في {govLabel}
        </Link>
        <Link href={`/tools/estimator`}>
          تقدير قيمة عقار في {govLabel}
        </Link>
      </div>
    </section>
  );
}
