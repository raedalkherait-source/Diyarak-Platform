using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Property;

public sealed class Property : AggregateRoot<Guid>
{
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
        int? lastModernizationYear = null)
        : base(id)
    {
        if (!Enum.IsDefined(category))
            throw new ArgumentOutOfRangeException(nameof(category), category, "Unsupported property category.");

        ArgumentNullException.ThrowIfNull(address);

        if (totalRooms is < 0m)
            throw new ArgumentOutOfRangeException(nameof(totalRooms));

        if (bedroomCount is < 0)
            throw new ArgumentOutOfRangeException(nameof(bedroomCount));

        if (bathroomCount is < 0)
            throw new ArgumentOutOfRangeException(nameof(bathroomCount));

        if (furnishingQuality is { } quality && !Enum.IsDefined(quality))
            throw new ArgumentOutOfRangeException(nameof(furnishingQuality), furnishingQuality, "Unsupported furnishing quality.");

        if (constructionYear is < 1)
            throw new ArgumentOutOfRangeException(nameof(constructionYear));

        if (lastModernizationYear is < 1)
            throw new ArgumentOutOfRangeException(nameof(lastModernizationYear));

        HashSet<PropertyFeature> featureSet = features is null
            ? []
            : [.. features];

        foreach (PropertyFeature feature in featureSet)
        {
            if (!Enum.IsDefined(feature))
                throw new ArgumentOutOfRangeException(nameof(features), feature, "Unsupported property feature.");
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
    }

    public PropertyCategory Category { get; }

    public PropertyAddress Address { get; }

    public Area? LivingArea { get; }

    public Area? UsableArea { get; }

    public decimal? TotalRooms { get; }

    public int? BedroomCount { get; }

    public int? BathroomCount { get; }

    public FurnishingQuality? FurnishingQuality { get; }

    public IReadOnlyCollection<PropertyFeature> Features { get; }

    public int? ConstructionYear { get; }

    public int? LastModernizationYear { get; }
}
