namespace Application.Common.Results;

public sealed class Result<T> : Result
{
    public T? Value { get; }


    private Result(
        T value
    ) : base(true, string.Empty)
    {
        Value = value;
    }


    private Result(
        string error
    ) : base(false, error)
    {
        Value = default;
    }


    public static Result<T> Success(T value)
    {
        return new Result<T>(value);
    }


    public static Result<T> Fail(string error)
    {
        return new Result<T>(error);
    }
}