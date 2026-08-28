using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Property;

public sealed class Property : AggregateRoot<Guid>
{
    public Property(Guid id, PropertyCategory category)
        : base(id)
    {
        if (!Enum.IsDefined(category))
            throw new ArgumentOutOfRangeException(nameof(category), category, "Unsupported property category.");

        Category = category;
    }

    public PropertyCategory Category { get; }
}
