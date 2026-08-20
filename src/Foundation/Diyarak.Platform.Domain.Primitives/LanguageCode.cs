using System.Text.RegularExpressions;

namespace Diyarak.Platform.Domain.Primitives;

public sealed record LanguageCode
{
    private static readonly Regex Pattern = new("^[A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8})*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private LanguageCode(string value) => Value = value;
    public string Value { get; }
    public static LanguageCode Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        string normalized = value.Trim();
        return Pattern.IsMatch(normalized) ? new LanguageCode(normalized) : throw new ArgumentException("Invalid language code.", nameof(value));
    }
    public override string ToString() => Value;
}
