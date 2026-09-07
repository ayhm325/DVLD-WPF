namespace Presentation.Services.Results;

public class ApiResult
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    protected ApiResult(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static ApiResult Success()
        => new(true, string.Empty);

    public static ApiResult Failure(string error)
        => new(false, error);
}

public sealed class ApiResult<T> : ApiResult
{
    public T? Value { get; }

    private ApiResult(
        bool isSuccess,
        T? value,
        string error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public static ApiResult<T> Success(T value)
        => new(true, value, string.Empty);

    public static new ApiResult<T> Failure(string error)
        => new(false, default, error);
}