using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Listing;

public sealed class ListingContext : ValueObject
{
    public ListingContext(
        PublishingRole publishingRole,
        TransactionIntent transactionIntent)
    {
        if (!Enum.IsDefined(publishingRole))
            throw new ArgumentOutOfRangeException(nameof(publishingRole), publishingRole, "Unsupported publishing role.");

        if (!Enum.IsDefined(transactionIntent))
            throw new ArgumentOutOfRangeException(nameof(transactionIntent), transactionIntent, "Unsupported transaction intent.");

        PublishingRole = publishingRole;
        TransactionIntent = transactionIntent;
    }

    public PublishingRole PublishingRole { get; }

    public TransactionIntent TransactionIntent { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PublishingRole;
        yield return TransactionIntent;
    }
}
