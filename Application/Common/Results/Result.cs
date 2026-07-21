namespace Application.Common.Results;

public class Result
{
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string Error { get; }


    protected Result(
        bool success,
        string error)
    {
        IsSuccess = success;
        Error = error;
    }


    public static Result Success()
        => new(true, string.Empty);


    public static Result Failure(string error)
        => new(false, error);
}