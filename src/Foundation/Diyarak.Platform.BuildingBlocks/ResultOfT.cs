namespace Diyarak.Platform.BuildingBlocks;

public sealed class Result<T> : Result
{
    private readonly T? _value;
    private Result(T? value, bool isSuccess, Error error) : base(isSuccess, error) => _value = value;
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("A failed result has no value.");
    internal static Result<T> CreateSuccess(T value) => new(value, true, Error.None);
    internal static Result<T> CreateFailure(Error error) => new(default, false, error);
}
