namespace Diyarak.Platform.Persistence.PostgreSql.Identity;

internal sealed class PhoneAccountRecord
{
    public string PhoneNumber { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
