using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Listing;

/// <summary>
/// How interested people may contact the publisher. The phone number itself is never stored on
/// the Listing; it is the publisher's verified phone number, revealed on request.
/// </summary>
public sealed class ListingContactOptions : ValueObject
{
    public ListingContactOptions(bool allowCalls, bool allowWhatsApp)
    {
        if (!allowCalls && !allowWhatsApp)
        {
            throw new ArgumentException(
                "At least one contact channel must be allowed.");
        }

        AllowCalls = allowCalls;
        AllowWhatsApp = allowWhatsApp;
    }

    public bool AllowCalls { get; }

    public bool AllowWhatsApp { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AllowCalls;
        yield return AllowWhatsApp;
    }
}
