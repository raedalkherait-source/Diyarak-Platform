namespace Diyarak.Platform.Identity;

public interface IExternalIdentityResolver
{
    public Task<Guid?> ResolveUserIdAsync(
        ExternalIdentity identity,
        CancellationToken cancellationToken = default);
}
