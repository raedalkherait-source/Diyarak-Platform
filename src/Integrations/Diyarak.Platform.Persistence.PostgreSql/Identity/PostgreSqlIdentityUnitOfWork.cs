using Diyarak.Platform.Identity.Application;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Identity;

internal sealed class PostgreSqlIdentityUnitOfWork
    : IIdentityUnitOfWork
{
    private const string UniqueViolationSqlState = "23505";

    private readonly PlatformDbContext _context;

    public PostgreSqlIdentityUnitOfWork(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<bool> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            return false;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is Npgsql.PostgresException { SqlState: UniqueViolationSqlState })
        {
            _context.ChangeTracker.Clear();
            return false;
        }
    }
}
