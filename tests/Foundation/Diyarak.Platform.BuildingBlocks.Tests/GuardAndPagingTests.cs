namespace Diyarak.Platform.BuildingBlocks.Tests;

public sealed class GuardAndPagingTests
{
    [Fact] public void Guard_rejects_null() => Assert.Throws<ArgumentNullException>(() => Guard.NotNull<object>(null, "value"));
    [Fact] public void Page_request_calculates_skip() => Assert.Equal(40, new PageRequest(3, 20).Skip);
    [Fact] public void Page_request_enforces_maximum() => Assert.Throws<ArgumentOutOfRangeException>(() => new PageRequest(1, 201));
    [Fact] public void Paged_result_calculates_pages() { PagedResult<int> result = new([1, 2], 2, 2, 5); Assert.Equal(3, result.TotalPages); Assert.True(result.HasPreviousPage); Assert.True(result.HasNextPage); }
    [Fact] public void Empty_paged_result_has_zero_pages() => Assert.Equal(0, new PagedResult<int>([], 1, 20, 0).TotalPages);
}
