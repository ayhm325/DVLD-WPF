using Application.Common.Results;

namespace Presentation.Services.Api;

public interface IApiClient
{
    Task<Result<T>> GetAsync<T>(
        string requestUri,
        CancellationToken cancellationToken = default);

    Task<Result<TResponse>> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> PutAsync<TRequest>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        string requestUri,
        CancellationToken cancellationToken = default);
}