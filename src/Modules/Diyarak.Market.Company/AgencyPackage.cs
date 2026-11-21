using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Company;

/// <summary>
/// A listing quota package assigned to an agency by a moderator (ADR-0072).
/// Each package grants a maximum number of concurrently active (Published + Paused) listings
/// until it expires. When no package is active the platform-wide free quota applies.
/// </summary>
public sealed class AgencyPackage : AggregateRoot<Guid>
{
    public const int MaximumNoteLength = 500;
    public const int DefaultFreeQuota = 5;

    private AgencyPackage(Guid id, Guid agencyId, int listingQuota, DateTimeOffset expiresAt, string? note, DateTimeOffset createdAt)
        : base(id)
    {
        if (agencyId == Guid.Empty)
            throw new ArgumentException("A package needs an agency.", nameof(agencyId));

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(listingQuota);

        if (expiresAt <= createdAt)
            throw new ArgumentException("Expiry must be after creation.", nameof(expiresAt));

        AgencyId = agencyId;
        ListingQuota = listingQuota;
        ExpiresAt = expiresAt;
        Note = Sanitize(note);
        CreatedAt = createdAt;
        IsRevoked = false;
    }

    public Guid AgencyId { get; }

    public int ListingQuota { get; }

    public DateTimeOffset ExpiresAt { get; }

    public string? Note { get; }

    public DateTimeOffset CreatedAt { get; }

    public bool IsRevoked { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive(DateTimeOffset now) => !IsRevoked && ExpiresAt > now;

    public static AgencyPackage Create(Guid id, Guid agencyId, int listingQuota, DateTimeOffset expiresAt, string? note, DateTimeOffset now)
    {
        return new AgencyPackage(id, agencyId, listingQuota, expiresAt, note, now);
    }

    public static AgencyPackage Restore(
        Guid id, Guid agencyId, int listingQuota, DateTimeOffset expiresAt, string? note,
        DateTimeOffset createdAt, bool isRevoked, DateTimeOffset? revokedAt)
    {
        var package = new AgencyPackage(id, agencyId, listingQuota, expiresAt, note, createdAt)
        {
            IsRevoked = isRevoked,
            RevokedAt = revokedAt,
        };
        return package;
    }

    public void Revoke(DateTimeOffset now)
    {
        if (IsRevoked)
            throw new InvalidOperationException("The package is already revoked.");

        IsRevoked = true;
        RevokedAt = now;
    }

    private static string? Sanitize(string? value)
    {
        string? trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        if (trimmed.Length > MaximumNoteLength)
            throw new ArgumentException($"The note exceeds {MaximumNoteLength} characters.", nameof(value));

        if (trimmed.Any(character => char.IsControl(character) && character is not '\n' and not '\r'))
            throw new ArgumentException("The note contains control characters.", nameof(value));

        return trimmed;
    }
}
