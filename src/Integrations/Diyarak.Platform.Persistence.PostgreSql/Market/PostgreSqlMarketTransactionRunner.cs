using System.Data;
using Diyarak.Market.Application;
using Diyarak.Platform.BuildingBlocks;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlMarketTransactionRunner
    : IMarketTransactionRunner
{
    private readonly PlatformDbContext _context;

    public PostgreSqlMarketTransactionRunner(
        PlatformDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    public Task<Result> ExecuteAsync(
        Func<CancellationToken, Task<Result>> operation,
        CancellationToken cancellationToken = default)
    {
        return ExecuteCoreAsync(
            operation,
            cancellationToken);
    }

    public Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        return ExecuteCoreAsync(
            operation,
            cancellationToken);
    }

    private async Task<TResult> ExecuteCoreAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
        where TResult : Result
    {
        ArgumentNullException.ThrowIfNull(operation);

        var executionStrategy =
            _context.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(
            async () =>
            {
                await using var transaction =
                    await _context.Database.BeginTransactionAsync(
                        IsolationLevel.ReadCommitted,
                        cancellationToken);

                try
                {
                    TResult result =
                        await operation(cancellationToken);

                    if (result.IsSuccess)
                    {
                        await transaction.CommitAsync(
                            cancellationToken);
                    }
                    else
                    {
                        await transaction.RollbackAsync(
                            CancellationToken.None);
                    }

                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(
                        CancellationToken.None);

                    throw;
                }
            });
    }
}
