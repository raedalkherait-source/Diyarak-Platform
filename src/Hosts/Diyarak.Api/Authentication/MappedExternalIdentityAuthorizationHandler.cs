using Diyarak.Platform.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Diyarak.Api.Authentication;

internal sealed class MappedExternalIdentityAuthorizationHandler(
    IExternalIdentityResolver identityResolver)
    : AuthorizationHandler<MappedExternalIdentityRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MappedExternalIdentityRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
            return;

        if (!context.User.TryGetExternalIdentity(
                out ExternalIdentity? identity) ||
            identity is null)
        {
            return;
        }

        Guid? userId =
            await identityResolver.ResolveUserIdAsync(
                identity,
                httpContext.RequestAborted);

        if (userId is null)
            return;

        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "An authenticated external identity resolved to an empty internal user identifier.");
        }

        httpContext.Features.Set(
            new AuthenticatedActorFeature(userId.Value));

        context.Succeed(requirement);
    }
}
