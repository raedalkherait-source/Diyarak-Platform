using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Diyarak.Platform.Domain.Primitives;

public sealed record PhoneNumber
{
    private static readonly Regex E164Pattern = new("^\\+[1-9][0-9]{7,14}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private PhoneNumber(string value) => Value = value;
    public string Value { get; }

    public static PhoneNumber Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        string normalized = value.Trim();
        return E164Pattern.IsMatch(normalized)
            ? new PhoneNumber(normalized)
            : throw new ArgumentException("Phone number must use E.164 format.", nameof(value));
    }

    public static bool TryCreate(string? value, [NotNullWhen(true)] out PhoneNumber? phone)
    {
        phone = null;
        if (string.IsNullOrWhiteSpace(value)) return false;
        string normalized = value.Trim();
        if (!E164Pattern.IsMatch(normalized)) return false;
        phone = new PhoneNumber(normalized);
        return true;
    }

    public override string ToString() => Value;
}
