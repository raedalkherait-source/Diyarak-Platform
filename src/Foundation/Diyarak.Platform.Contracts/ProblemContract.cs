namespace Diyarak.Platform.Contracts;

public sealed record ProblemContract(string Code, string Title, string? Detail = null, IReadOnlyDictionary<string, string[]>? Errors = null);
