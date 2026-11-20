using System.Security.Claims;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Identity.Application;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Diyarak.Api.Authentication.Phone;

internal sealed class JwtAccessTokenIssuer(
    PhoneAuthenticationOptions options,
    IClock clock)
    : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    public string Issuer => options.Issuer;

    public IssuedAccessToken Issue(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User identifier cannot be empty.",
                nameof(userId));
        }

        DateTimeOffset now = clock.UtcNow;
        DateTimeOffset expiresAt = now.Add(options.AccessTokenLifetime);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            TokenType = "at+jwt",
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString("D")),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            ]),
            SigningCredentials = new SigningCredentials(
                CreateSigningKey(options),
                SecurityAlgorithms.HmacSha256),
        };

        return new IssuedAccessToken(
            _handler.CreateToken(descriptor),
            expiresAt);
    }

    internal static SymmetricSecurityKey CreateSigningKey(
        PhoneAuthenticationOptions options) =>
        new(options.SigningKeyBytes);
}
