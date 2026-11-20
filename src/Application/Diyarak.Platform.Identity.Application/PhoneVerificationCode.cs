using System.Security.Cryptography;
using System.Text;

namespace Diyarak.Platform.Identity.Application;

internal static class PhoneVerificationCode
{
    internal const int Length = 6;

    internal static string Generate()
    {
        int value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static bool IsWellFormed(string? code) =>
        code is { Length: Length } && code.All(char.IsAsciiDigit);

    /// <summary>Hashes the code bound to its challenge so hashes cannot be reused across challenges.</summary>
    internal static string Hash(Guid challengeId, string code)
    {
        byte[] input = Encoding.UTF8.GetBytes($"{challengeId:N}:{code}");
        return Convert.ToHexString(SHA256.HashData(input));
    }

    internal static bool Matches(Guid challengeId, string code, string expectedHash)
    {
        byte[] actual = Encoding.ASCII.GetBytes(Hash(challengeId, code));
        byte[] expected = Encoding.ASCII.GetBytes(expectedHash);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
