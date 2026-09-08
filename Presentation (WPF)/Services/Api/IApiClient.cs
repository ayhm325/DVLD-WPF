using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface IApiClient
{
    Task<ApiResult<T>> GetAsync<T>(
        string requestUri,
        CancellationToken cancellationToken = default);

    Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult> PostAsync(
        string requestUri,
        CancellationToken cancellationToken = default);

    Task<ApiResult> PutAsync<TRequest>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteAsync(
        string requestUri,
        CancellationToken cancellationToken = default);
}