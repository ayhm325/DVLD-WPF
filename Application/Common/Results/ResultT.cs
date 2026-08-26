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


    // =========================
    // SUCCESS
    // =========================

    public static Result<T> Success(T value)
        => new(
            true,
            value,
            string.Empty,
            ErrorType.None);


    // =========================
    // FAILURE
    // =========================

    public static Result<T> FromFailure(
        string error)
        => new(
            false,
            default,
            error,
            ErrorType.Failure);


    // =========================
    // VALIDATION
    // =========================

    public static Result<T> FromValidationFailure(
        string error)
        => new(
            false,
            default,
            error,
            ErrorType.Validation);


    // =========================
    // NOT FOUND
    // =========================

    public static Result<T> FromNotFound(
        string error)
        => new(
            false,
            default,
            error,
            ErrorType.NotFound);


    // =========================
    // CONFLICT
    // =========================

    public static Result<T> FromConflict(
        string error)
        => new(
            false,
            default,
            error,
            ErrorType.Conflict);
}