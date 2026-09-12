namespace Diyarak.Api.Authentication;

internal sealed class AuthenticatedActorAccessor(
    IHttpContextAccessor httpContextAccessor)
    : IAuthenticatedActorAccessor
{
    public Guid? UserId =>
        httpContextAccessor.HttpContext?
            .Features
            .Get<AuthenticatedActorFeature>()?
            .UserId;
}
