using Presentation.Services.Results;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Presentation.Services.Api;

public sealed class ApiClient(HttpClient httpClient, ICurrentUserSession currentUser) : IApiClient
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ICurrentUserSession _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ApiResult<T>> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default) =>
        SendAsync<T>(() => CreateRequest(HttpMethod.Get, requestUri), cancellationToken);

    public Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(
        string requestUri, TRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(() => CreateRequest(HttpMethod.Post, requestUri, request), cancellationToken);

    public async Task<ApiResult> PostAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<object?>(() => CreateRequest(HttpMethod.Post, requestUri), cancellationToken);
        return result.IsSuccess
            ? ApiResult.Success(result.StatusCode ?? HttpStatusCode.OK)
            : ApiResult.Failure(result.Error, result.StatusCode);
    }

    public async Task<ApiResult> PutAsync<TRequest>(
        string requestUri, TRequest request, CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<object?>(() => CreateRequest(HttpMethod.Put, requestUri, request), cancellationToken);
        return result.IsSuccess
            ? ApiResult.Success(result.StatusCode ?? HttpStatusCode.OK)
            : ApiResult.Failure(result.Error, result.StatusCode);
    }

    public async Task<ApiResult> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<object?>(() => CreateRequest(HttpMethod.Delete, requestUri), cancellationToken);
        return result.IsSuccess
            ? ApiResult.Success(result.StatusCode ?? HttpStatusCode.OK)
            : ApiResult.Failure(result.Error, result.StatusCode);
    }

    private async Task<ApiResult<T>> SendAsync<T>(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = requestFactory();
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return ApiResult<T>.Success(default!, response.StatusCode);

                var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);

                return value is null
                    ? ApiResult<T>.Failure("The API returned an empty response.", response.StatusCode)
                    : ApiResult<T>.Success(value, response.StatusCode);
            }

            return ApiResult<T>.Failure(
                await ExtractErrorAsync(response, cancellationToken),
                response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.Failure("Unable to connect to the DVLD API.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApiResult<T>.Failure("The request to the DVLD API timed out.");
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);
        AddAuthorizationHeader(request);
        return request;
    }

    private HttpRequestMessage CreateRequest<T>(HttpMethod method, string requestUri, T content)
    {
        var request = CreateRequest(method, requestUri);
        request.Content = JsonContent.Create(content, options: JsonOptions);
        return request;
    }

    private void AddAuthorizationHeader(HttpRequestMessage request)
    {
        if (_currentUser.IsLoggedIn && !string.IsNullOrWhiteSpace(_currentUser.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentUser.AccessToken);
    }

    private static async Task<string> ExtractErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
            return GetDefaultError(response.StatusCode);

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            foreach (var property in new[] { "error", "message", "detail" })
            {
                if (root.TryGetProperty(property, out var value) &&
                    value.ValueKind == JsonValueKind.String)
                    return value.GetString() ?? GetDefaultError(response.StatusCode);
            }
        }
        catch (JsonException)
        {
        }

        return content;
    }

    private static string GetDefaultError(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => "Authentication is required.",
        HttpStatusCode.Forbidden => "You are not authorized to perform this operation.",
        HttpStatusCode.NotFound => "The requested resource was not found.",
        HttpStatusCode.Conflict => "The operation conflicts with the current data.",
        HttpStatusCode.BadRequest => "The request is invalid.",
        _ => "The API request failed."
    };
}