using System.Security.Cryptography;
using System.Text;

namespace Diyarak.Platform.Identity.Application;

/// <summary>
/// Refresh tokens look like <c>{sessionId}.{secret}</c>. The session identifier allows a direct
/// lookup; only the SHA-256 hash of the 256-bit secret is stored.
/// </summary>
internal static class RefreshTokens
{
    internal static (string Token, string Hash) Create(Guid sessionId)
    {
        string secret = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return ($"{sessionId:N}.{secret}", Hash(secret));
    }

    internal static bool TryParse(string? token, out Guid sessionId, out string secret)
    {
        sessionId = Guid.Empty;
        secret = string.Empty;

        if (string.IsNullOrWhiteSpace(token) || token.Length > 200)
            return false;

        int separator = token.IndexOf('.', StringComparison.Ordinal);
        if (separator != 32 || !Guid.TryParseExact(token[..separator], "N", out sessionId))
            return false;

        secret = token[(separator + 1)..];
        return secret.Length >= 32 && secret.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
    }

    internal static string Hash(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(secret)));

    internal static bool HashEquals(string? storedHash, string candidateHash) =>
        storedHash is not null &&
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(storedHash), Encoding.ASCII.GetBytes(candidateHash));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
