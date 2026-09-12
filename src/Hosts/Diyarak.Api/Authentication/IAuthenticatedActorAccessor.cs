namespace Diyarak.Api.Authentication;

public interface IAuthenticatedActorAccessor
{
    public Guid? UserId { get; }
}
