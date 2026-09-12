using Diyarak.Platform.Identity;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Identity;

internal sealed class PostgreSqlExternalIdentityResolver
    : IExternalIdentityResolver
{
    private readonly PlatformDbContext _context;

    public PostgreSqlExternalIdentityResolver(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<Guid?> ResolveUserIdAsync(
        ExternalIdentity identity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);

        Guid? userId =
            await _context.ExternalIdentityMappings
                .AsNoTracking()
                .Where(
                    mapping =>
                        mapping.Issuer == identity.Issuer &&
                        mapping.Subject == identity.Subject)
                .Select(mapping => (Guid?)mapping.UserId)
                .SingleOrDefaultAsync(cancellationToken);

        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "An external identity mapping resolved to an empty internal user identifier.");
        }

        return userId;
    }
}
