using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Diyarak.Platform.Domain.Primitives;

public sealed record Currency
{
    private static readonly Regex CodePattern = new("^[A-Z]{3}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private Currency(string code) => Code = code;

    public string Code { get; }

    public static Currency Syp { get; } = new("SYP");
    public static Currency Usd { get; } = new("USD");
    public static Currency Eur { get; } = new("EUR");

    public static Currency Create(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        string normalized = code.Trim().ToUpperInvariant();
        return CodePattern.IsMatch(normalized)
            ? new Currency(normalized)
            : throw new ArgumentException("Currency must be a three-letter alphabetic code.", nameof(code));
    }

    public static bool TryCreate(string? code, [NotNullWhen(true)] out Currency? currency)
    {
        currency = null;
        if (string.IsNullOrWhiteSpace(code)) return false;
        string normalized = code.Trim().ToUpperInvariant();
        if (!CodePattern.IsMatch(normalized)) return false;
        currency = new Currency(normalized);
        return true;
    }

    public override string ToString() => Code;
}
