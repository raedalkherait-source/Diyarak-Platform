using System.Security.Claims;
using Diyarak.Platform.Identity;

namespace Diyarak.Api.Authentication;

public static class ClaimsPrincipalExternalIdentityExtensions
{
    public static bool TryGetExternalIdentity(
        this ClaimsPrincipal principal,
        out ExternalIdentity? identity)
    {
        ArgumentNullException.ThrowIfNull(principal);

        identity = null;

        Claim[] issuerClaims =
            principal.FindAll("iss").ToArray();
        Claim[] subjectClaims =
            principal.FindAll("sub").ToArray();

        if (issuerClaims.Length != 1 ||
            subjectClaims.Length != 1)
        {
            return false;
        }

        string issuer = issuerClaims[0].Value;
        string subject = subjectClaims[0].Value;

        if (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(subject))
        {
            return false;
        }

        identity = new ExternalIdentity(
            issuer,
            subject);

        return true;
    }
}
