namespace Diyarak.Platform.Contracts;

public sealed record ApiResponse<T>(T? Data, ProblemContract? Problem, string? CorrelationId)
{
    public bool IsSuccess => Problem is null;
}

public static class ApiResponse
{
    public static ApiResponse<T> Success<T>(T data, string? correlationId = null) => new(data, null, correlationId);

    public static ApiResponse<T> Failure<T>(ProblemContract problem, string? correlationId = null)
    {
        ArgumentNullException.ThrowIfNull(problem);
        return new ApiResponse<T>(default, problem, correlationId);
    }
}
