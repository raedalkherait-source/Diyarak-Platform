using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Identity;
using Diyarak.Platform.Identity.Application;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Identity;

internal sealed class PostgreSqlPhoneAccountRepository
    : IPhoneAccountRepository
{
    private readonly PlatformDbContext _context;

    public PostgreSqlPhoneAccountRepository(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<PhoneAccount?> FindAsync(
        PhoneNumber phoneNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);

        PhoneAccountRecord? record =
            await _context.PhoneAccounts
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    account => account.PhoneNumber == phoneNumber.Value,
                    cancellationToken);

        return record is null
            ? null
            : new PhoneAccount(
                PhoneNumber.Create(record.PhoneNumber),
                record.UserId,
                record.CreatedAt);
    }

    public Task AddAsync(
        PhoneAccount account,
        ExternalIdentity tokenIdentity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(tokenIdentity);

        _context.PhoneAccounts.Add(
            new PhoneAccountRecord
            {
                PhoneNumber = account.PhoneNumber.Value,
                UserId = account.UserId,
                CreatedAt = account.CreatedAt,
            });

        _context.ExternalIdentityMappings.Add(
            new ExternalIdentityMappingRecord
            {
                Issuer = tokenIdentity.Issuer,
                Subject = tokenIdentity.Subject,
                UserId = account.UserId,
            });

        return Task.CompletedTask;
    }
}
