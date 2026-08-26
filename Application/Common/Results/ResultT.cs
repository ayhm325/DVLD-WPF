namespace Application.Common.Results;

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(
        bool success,
        T? value,
        string error,
        ErrorType errorType)
        : base(success, error, errorType)
    {
        Value = value;
    }

    public static Result<T> Success(T value)
        => new(
            true,
            value,
            string.Empty,
            ErrorType.None);

    public static Result<T> FromFailure(
        string error)
        => new(
            false,
            default,
            error,
            ErrorType.Failure);

    public static Result<T> ValidationFailure(
        string error)
        => new(
            false,
            default,
            error,
            ErrorType.Validation);

    public static Result<T> NotFound(
        string error)
        => new(
            false,
            default,
            error,
            ErrorType.NotFound);

    public static Result<T> Conflict(
        string error)
        => new(
            false,
            default,
            error,
            ErrorType.Conflict);
}