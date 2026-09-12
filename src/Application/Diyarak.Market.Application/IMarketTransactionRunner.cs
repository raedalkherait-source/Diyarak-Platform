using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Market.Application;

public interface IMarketTransactionRunner
{
    public Task<Result> ExecuteAsync(
        Func<CancellationToken, Task<Result>> operation,
        CancellationToken cancellationToken = default);

    public Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken = default);
}
