using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Listing;

public sealed class ListingHeadline : ValueObject
{
    public ListingHeadline(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Value = value;
    }

    public string Value { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
