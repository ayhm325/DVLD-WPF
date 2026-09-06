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

    public static Result<T> FromFailure(string error)
        => new(
            false,
            default,
            error,
            ErrorType.Failure);

    public static Result<T> FromResult(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            throw new ArgumentException(
                "Cannot convert a successful result to a failure result.",
                nameof(result));
        }

        return new(
            false,
            default,
            result.Error,
            result.ErrorType);
    }

    public static Result<T> FromValidationFailure(string error)
        => new(
            false,
            default,
            error,
            ErrorType.Validation);

    public static Result<T> FromNotFound(string error)
        => new(
            false,
            default,
            error,
            ErrorType.NotFound);

    public static Result<T> FromConflict(string error)
        => new(
            false,
            default,
            error,
            ErrorType.Conflict);

    public static Result<T> FromForbidden(string error)
        => new(
            false,
            default,
            error,
            ErrorType.Forbidden);
}