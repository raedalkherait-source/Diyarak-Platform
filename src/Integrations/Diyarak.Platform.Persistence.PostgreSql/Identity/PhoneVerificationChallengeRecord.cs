namespace Diyarak.Platform.Persistence.PostgreSql.Identity;

internal sealed class PhoneVerificationChallengeRecord
{
    public Guid Id { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public string CodeHash { get; set; } = string.Empty;

    public DateTimeOffset IssuedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public int FailedAttempts { get; set; }

    public DateTimeOffset? ConsumedAt { get; set; }
}
