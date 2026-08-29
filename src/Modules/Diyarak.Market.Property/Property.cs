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
        int? bathroomCount = null)
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

        Category = category;
        Address = address;
        LivingArea = livingArea;
        UsableArea = usableArea;
        TotalRooms = totalRooms;
        BedroomCount = bedroomCount;
        BathroomCount = bathroomCount;
    }

    public PropertyCategory Category { get; }

    public PropertyAddress Address { get; }

    public Area? LivingArea { get; }

    public Area? UsableArea { get; }

    public decimal? TotalRooms { get; }

    public int? BedroomCount { get; }

    public int? BathroomCount { get; }
}
