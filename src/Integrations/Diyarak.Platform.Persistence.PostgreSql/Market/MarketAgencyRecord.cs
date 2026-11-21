namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketAgencyRecord
{
    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public long Version { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Governorate { get; set; }

    public string City { get; set; } = string.Empty;

    public int VerificationStatus { get; set; }

    public string? RegistrationNumber { get; set; }

    public string? VerificationNote { get; set; }

    public DateTimeOffset? VerifiedAt { get; set; }
}
