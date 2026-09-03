namespace Application.Common.Results;

public class Result
{
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string Error { get; }

    public ErrorType ErrorType { get; }

    protected Result(
        bool success,
        string error,
        ErrorType errorType)
    {
        IsSuccess = success;
        Error = error;
        ErrorType = errorType;
    }

    public static Result Success()
        => new(
            true,
            string.Empty,
            ErrorType.None);

    public static Result Failure(
        string error)
        => new(
            false,
            error,
            ErrorType.Failure);

    public static Result ValidationFailure(
        string error)
        => new(
            false,
            error,
            ErrorType.Validation);

    public static Result NotFound(
        string error)
        => new(
            false,
            error,
            ErrorType.NotFound);

    public static Result Forbidden(string error)
         => new(false, error, ErrorType.Forbidden);

    public static Result Conflict(
        string error)
        => new(
            false,
            error,
            ErrorType.Conflict);
}