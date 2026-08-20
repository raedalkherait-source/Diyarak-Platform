using System.Text.RegularExpressions;

namespace Diyarak.Platform.Domain.Primitives;

public sealed record CountryCode
{
    private static readonly Regex Pattern = new("^[A-Z]{2}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private CountryCode(string value) => Value = value;
    public string Value { get; }
    public static CountryCode Syria { get; } = new("SY");
    public static CountryCode Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        string normalized = value.Trim().ToUpperInvariant();
        return Pattern.IsMatch(normalized) ? new CountryCode(normalized) : throw new ArgumentException("Country code must contain two letters.", nameof(value));
    }
    public override string ToString() => Value;
}
