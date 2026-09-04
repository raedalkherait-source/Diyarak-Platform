using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Listing;

public sealed class ListingPrice : ValueObject
{
    private ListingPrice(Money? amount)
    {
        Amount = amount;
    }

    public Money? Amount { get; }

    public bool IsOnRequest => Amount is null;

    public static ListingPrice Known(Money amount)
    {
        if (amount.Amount < 0m)
            throw new ArgumentOutOfRangeException(nameof(amount), amount.Amount, "Listing price cannot be negative.");

        return new ListingPrice(amount);
    }

    public static ListingPrice OnRequest() => new(null);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return IsOnRequest;
    }
}
