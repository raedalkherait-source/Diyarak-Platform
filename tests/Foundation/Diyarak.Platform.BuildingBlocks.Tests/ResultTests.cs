namespace Diyarak.Platform.BuildingBlocks.Tests;

public sealed class ResultTests
{
    [Fact] public void Success_has_no_error() { Result result = Result.Success(); Assert.True(result.IsSuccess); Assert.Equal(Error.None, result.Error); }
    [Fact] public void Failure_has_error() { Error error = Error.Validation("code", "description"); Result result = Result.Failure(error); Assert.True(result.IsFailure); Assert.Equal(error, result.Error); }
    [Fact] public void Generic_success_exposes_value() => Assert.Equal(42, Result.Success(42).Value);
    [Fact] public void Generic_failure_value_throws() { Result<int> result = Result.Failure<int>(Error.Failure("x", "y")); Assert.Throws<InvalidOperationException>(() => _ = result.Value); }
}
