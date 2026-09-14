using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Property;

public sealed class Property : AggregateRoot<Guid>
{
    public const long InitialVersion = 1;

    public Property(
        Guid id,
        PropertyCategory category,
        PropertyAddress address,
        Area? livingArea = null,
        Area? usableArea = null,
        decimal? totalRooms = null,
        int? bedroomCount = null,
        int? bathroomCount = null,
        FurnishingQuality? furnishingQuality = null,
        IEnumerable<PropertyFeature>? features = null,
        int? constructionYear = null,
        int? lastModernizationYear = null,
        CommercialPropertySubtype? commercialSubtype = null,
        Area? salesArea = null,
        Area? totalArea = null,
        int? parkingSpaceCount = null,
        Guid? ownerUserId = null)
        : this(
            id,
            category,
            address,
            livingArea,
            usableArea,
            totalRooms,
            bedroomCount,
            bathroomCount,
            furnishingQuality,
            features,
            constructionYear,
            lastModernizationYear,
            commercialSubtype,
            salesArea,
            totalArea,
            parkingSpaceCount,
            ownerUserId,
            InitialVersion)
    {
    }

    private Property(
        Guid id,
        PropertyCategory category,
        PropertyAddress address,
        Area? livingArea,
        Area? usableArea,
        decimal? totalRooms,
        int? bedroomCount,
        int? bathroomCount,
        FurnishingQuality? furnishingQuality,
        IEnumerable<PropertyFeature>? features,
        int? constructionYear,
        int? lastModernizationYear,
        CommercialPropertySubtype? commercialSubtype,
        Area? salesArea,
        Area? totalArea,
        int? parkingSpaceCount,
        Guid? ownerUserId,
        long version)
        : base(id)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Property owner user identifier must be non-empty when assigned.",
                nameof(ownerUserId));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);

        OwnerUserId = ownerUserId;
        Version = version;

        ReplaceDetails(
            category,
            address,
            livingArea,
            usableArea,
            totalRooms,
            bedroomCount,
            bathroomCount,
            furnishingQuality,
            features,
            constructionYear,
            lastModernizationYear,
            commercialSubtype,
            salesArea,
            totalArea,
            parkingSpaceCount);
    }

    public Guid? OwnerUserId { get; }

    public long Version { get; }

    public PropertyCategory Category { get; private set; }

    public PropertyAddress Address { get; private set; } = null!;

    public Area? LivingArea { get; private set; }

    public Area? UsableArea { get; private set; }

    public decimal? TotalRooms { get; private set; }

    public int? BedroomCount { get; private set; }

    public int? BathroomCount { get; private set; }

    public FurnishingQuality? FurnishingQuality { get; private set; }

    public IReadOnlyCollection<PropertyFeature> Features { get; private set; } = [];

    public int? ConstructionYear { get; private set; }

    public int? LastModernizationYear { get; private set; }

    public CommercialPropertySubtype? CommercialSubtype { get; private set; }

    public Area? SalesArea { get; private set; }

    public Area? TotalArea { get; private set; }

    public int? ParkingSpaceCount { get; private set; }

    public static Property Restore(
        Guid id,
        PropertyCategory category,
        PropertyAddress address,
        Area? livingArea,
        Area? usableArea,
        decimal? totalRooms,
        int? bedroomCount,
        int? bathroomCount,
        FurnishingQuality? furnishingQuality,
        IEnumerable<PropertyFeature>? features,
        int? constructionYear,
        int? lastModernizationYear,
        CommercialPropertySubtype? commercialSubtype,
        Area? salesArea,
        Area? totalArea,
        int? parkingSpaceCount,
        Guid? ownerUserId,
        long version) =>
        new(
            id,
            category,
            address,
            livingArea,
            usableArea,
            totalRooms,
            bedroomCount,
            bathroomCount,
            furnishingQuality,
            features,
            constructionYear,
            lastModernizationYear,
            commercialSubtype,
            salesArea,
            totalArea,
            parkingSpaceCount,
            ownerUserId,
            version);

    public void ReplaceDetails(
        PropertyCategory category,
        PropertyAddress address,
        Area? livingArea = null,
        Area? usableArea = null,
        decimal? totalRooms = null,
        int? bedroomCount = null,
        int? bathroomCount = null,
        FurnishingQuality? furnishingQuality = null,
        IEnumerable<PropertyFeature>? features = null,
        int? constructionYear = null,
        int? lastModernizationYear = null,
        CommercialPropertySubtype? commercialSubtype = null,
        Area? salesArea = null,
        Area? totalArea = null,
        int? parkingSpaceCount = null)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Unsupported property category.");
        }

        ArgumentNullException.ThrowIfNull(address);

        if (totalRooms is < 0m)
            throw new ArgumentOutOfRangeException(nameof(totalRooms));

        if (bedroomCount is < 0)
            throw new ArgumentOutOfRangeException(nameof(bedroomCount));

        if (bathroomCount is < 0)
            throw new ArgumentOutOfRangeException(nameof(bathroomCount));

        if (furnishingQuality is { } quality && !Enum.IsDefined(quality))
        {
            throw new ArgumentOutOfRangeException(
                nameof(furnishingQuality),
                furnishingQuality,
                "Unsupported furnishing quality.");
        }

        if (constructionYear is < 1)
            throw new ArgumentOutOfRangeException(nameof(constructionYear));

        if (lastModernizationYear is < 1)
            throw new ArgumentOutOfRangeException(nameof(lastModernizationYear));

        if (parkingSpaceCount is < 0)
            throw new ArgumentOutOfRangeException(nameof(parkingSpaceCount));

        if (commercialSubtype is { } subtype && !Enum.IsDefined(subtype))
        {
            throw new ArgumentOutOfRangeException(
                nameof(commercialSubtype),
                commercialSubtype,
                "Unsupported commercial property subtype.");
        }

        if (commercialSubtype is not null &&
            category != PropertyCategory.CommercialProperty)
        {
            throw new ArgumentException(
                "A commercial subtype can only be assigned to a commercial property.",
                nameof(commercialSubtype));
        }

        HashSet<PropertyFeature> featureSet = features is null
            ? []
            : [.. features];

        foreach (PropertyFeature feature in featureSet)
        {
            if (!Enum.IsDefined(feature))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(features),
                    feature,
                    "Unsupported property feature.");
            }
        }

        Category = category;
        Address = address;
        LivingArea = livingArea;
        UsableArea = usableArea;
        TotalRooms = totalRooms;
        BedroomCount = bedroomCount;
        BathroomCount = bathroomCount;
        FurnishingQuality = furnishingQuality;
        Features = featureSet.ToArray();
        ConstructionYear = constructionYear;
        LastModernizationYear = lastModernizationYear;
        CommercialSubtype = commercialSubtype;
        SalesArea = salesArea;
        TotalArea = totalArea;
        ParkingSpaceCount = parkingSpaceCount;
    }
}
