using System.Net;

namespace Presentation.Services.Results;

public class ApiResult
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }
    public HttpStatusCode? StatusCode { get; }

    protected ApiResult(bool isSuccess, string error, HttpStatusCode? statusCode = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        StatusCode = statusCode;
    }

    public static ApiResult Success(HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(true, string.Empty, statusCode);

    public static ApiResult Failure(string error, HttpStatusCode? statusCode = null)
        => new(false, error, statusCode);
}

public sealed class ApiResult<T> : ApiResult
{
    public T? Value { get; }

    private ApiResult(bool isSuccess, T? value, string error, HttpStatusCode? statusCode = null)
        : base(isSuccess, error, statusCode) => Value = value;

    public static ApiResult<T> Success(T value, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(true, value, string.Empty, statusCode);

    public static new ApiResult<T> Failure(string error, HttpStatusCode? statusCode = null)
        => new(false, default, error, statusCode);
}