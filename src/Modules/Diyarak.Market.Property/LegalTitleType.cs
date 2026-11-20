namespace Diyarak.Market.Property;

/// <summary>
/// The legal ownership or tenure status of a property as commonly declared in the Syrian market.
/// </summary>
public enum LegalTitleType
{
    /// <summary>Regular land-registry title deed (طابو أخضر).</summary>
    GreenTitleDeed = 1,

    /// <summary>Agricultural land-registry title deed (طابو زراعي).</summary>
    AgriculturalTitleDeed = 2,

    /// <summary>Undivided shares registered in a common title deed (طابو أسهم).</summary>
    TitleDeedShares = 3,

    /// <summary>Ownership established by a final court ruling (حكم محكمة).</summary>
    CourtRuling = 4,

    /// <summary>Irrevocable notarized power of attorney (وكالة كاتب عدل).</summary>
    NotarizedPowerOfAttorney = 5,

    /// <summary>Allocation by a housing cooperative or public housing body (جمعية سكنية / تخصص).</summary>
    HousingCooperativeAllocation = 6,

    /// <summary>Commercial key-money tenancy right (فروغ).</summary>
    KeyMoneyTenancy = 7
}
