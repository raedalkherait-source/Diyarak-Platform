using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application.Tests;

internal sealed class StubMarketTransactionRunner
    : IMarketTransactionRunner
{
    public int CallCount { get; private set; }

    public async Task<Result> ExecuteAsync(
        Func<CancellationToken, Task<Result>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        CallCount++;
        return await operation(cancellationToken);
    }

    public async Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        CallCount++;
        return await operation(cancellationToken);
    }
}
