namespace Application.Common.Results;

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(
        bool success,
        T? value,
        string error)
        : base(success, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value)
        => new(true, value, string.Empty);

    public static Result<T> Fail(string error)
        => new(false, default, error);
}