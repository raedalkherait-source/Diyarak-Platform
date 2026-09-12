namespace Diyarak.Platform.Identity;

public sealed record ExternalIdentity
{
    public ExternalIdentity(
        string issuer,
        string subject)
    {
        if (string.IsNullOrWhiteSpace(issuer))
            throw new ArgumentException(
                "Issuer must be non-empty.",
                nameof(issuer));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException(
                "Subject must be non-empty.",
                nameof(subject));

        Issuer = issuer;
        Subject = subject;
    }

    public string Issuer { get; }

    public string Subject { get; }
}
