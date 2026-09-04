using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Listing;

public sealed class ListingAvailableFromDate : ValueObject
{
    public ListingAvailableFromDate(DateOnly value)
    {
        Value = value;
    }

    public DateOnly Value { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString("yyyy-MM-dd");
}
