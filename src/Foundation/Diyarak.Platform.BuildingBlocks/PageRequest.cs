namespace Diyarak.Platform.BuildingBlocks;

public sealed record PageRequest
{
    public const int MaximumPageSize = 200;
    public PageRequest(int pageNumber = 1, int pageSize = 20)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, MaximumPageSize);
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int Skip => (PageNumber - 1) * PageSize;
}
