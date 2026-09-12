namespace Diyarak.Platform.Persistence.PostgreSql.Identity;

internal sealed class ExternalIdentityMappingRecord
{
    public string Issuer { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public Guid UserId { get; set; }
}
