namespace Diyarak.Platform.Contracts.Tests;

public sealed class ContractTests
{
    [Fact] public void Successful_response_has_data() { ApiResponse<int> response = ApiResponse.Success(7, "corr"); Assert.True(response.IsSuccess); Assert.Equal(7, response.Data); Assert.Equal("corr", response.CorrelationId); }
    [Fact] public void Failed_response_has_problem() { ProblemContract problem = new("not_found", "Not found"); ApiResponse<int> response = ApiResponse.Failure<int>(problem); Assert.False(response.IsSuccess); Assert.Equal(problem, response.Problem); }
    [Fact] public void Sort_contract_preserves_direction() => Assert.Equal(SortDirection.Descending, new SortContract("createdAt", SortDirection.Descending).Direction);
}
