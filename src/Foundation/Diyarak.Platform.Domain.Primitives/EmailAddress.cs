using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;

namespace Diyarak.Platform.Domain.Primitives;

public sealed record EmailAddress
{
    private EmailAddress(string value) => Value = value;
    public string Value { get; }

    public static EmailAddress Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        string normalized = value.Trim().ToLowerInvariant();
        if (!MailAddress.TryCreate(normalized, out MailAddress? parsed) ||
            !string.Equals(parsed.Address, normalized, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid email address.", nameof(value));
        return new EmailAddress(normalized);
    }

    public static bool TryCreate(string? value, [NotNullWhen(true)] out EmailAddress? email)
    {
        email = null;
        if (string.IsNullOrWhiteSpace(value)) return false;
        string normalized = value.Trim().ToLowerInvariant();
        if (!MailAddress.TryCreate(normalized, out MailAddress? parsed) ||
            !string.Equals(parsed.Address, normalized, StringComparison.OrdinalIgnoreCase)) return false;
        email = new EmailAddress(normalized);
        return true;
    }

    public override string ToString() => Value;
}
