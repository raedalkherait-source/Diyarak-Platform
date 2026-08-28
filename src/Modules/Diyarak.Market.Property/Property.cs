using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Property;

public sealed class Property : AggregateRoot<Guid>
{
    public Property(
        Guid id,
        PropertyCategory category,
        PropertyAddress address)
        : base(id)
    {
        if (!Enum.IsDefined(category))
            throw new ArgumentOutOfRangeException(nameof(category), category, "Unsupported property category.");

        ArgumentNullException.ThrowIfNull(address);

        Category = category;
        Address = address;
    }

    public PropertyCategory Category { get; }

    public PropertyAddress Address { get; }
}
