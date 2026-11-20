using Diyarak.Market.Application;
using Diyarak.Platform.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Identity;

internal sealed class PostgreSqlPublisherPhoneLookup
    : IPublisherPhoneLookup
{
    private readonly PlatformDbContext _context;

    public PostgreSqlPublisherPhoneLookup(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<PhoneNumber?> FindVerifiedPhoneAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        string? phone =
            await _context.PhoneAccounts
                .AsNoTracking()
                .Where(account => account.UserId == userId)
                .Select(account => account.PhoneNumber)
                .SingleOrDefaultAsync(cancellationToken);

        return phone is null ? null : PhoneNumber.Create(phone);
    }
}
