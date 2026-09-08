using Presentation.Services;
using Presentation.Services.Results;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Presentation.Services.Api;

public sealed class ApiClient(
    HttpClient httpClient,
    ICurrentUserSession currentUser) : IApiClient
{
    private readonly HttpClient _httpClient =
        httpClient
        ?? throw new ArgumentNullException(nameof(httpClient));

    private readonly ICurrentUserSession _currentUser =
        currentUser
        ?? throw new ArgumentNullException(nameof(currentUser));

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public Task<ApiResult<T>> GetAsync<T>(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(
            () => CreateRequest(
                HttpMethod.Get,
                requestUri),
            cancellationToken);
    }

    public Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(
            () => CreateRequest(
                HttpMethod.Post,
                requestUri,
                request),
            cancellationToken);
    }

    public async Task<ApiResult> PostAsync(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        var result =
            await SendAsync<object?>(
                () => CreateRequest(
                    HttpMethod.Post,
                    requestUri),
                cancellationToken);

        return result.IsSuccess
            ? ApiResult.Success()
            : ApiResult.Failure(result.Error);
    }

    public async Task<ApiResult> PutAsync<TRequest>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var result =
            await SendAsync<object?>(
                () => CreateRequest(
                    HttpMethod.Put,
                    requestUri,
                    request),
                cancellationToken);

        return result.IsSuccess
            ? ApiResult.Success()
            : ApiResult.Failure(result.Error);
    }

    public async Task<ApiResult> DeleteAsync(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        var result =
            await SendAsync<object?>(
                () => CreateRequest(
                    HttpMethod.Delete,
                    requestUri),
                cancellationToken);

        return result.IsSuccess
            ? ApiResult.Success()
            : ApiResult.Failure(result.Error);
    }

    private async Task<ApiResult<T>> SendAsync<T>(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request =
                requestFactory();

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode ==
                    HttpStatusCode.NoContent)
                {
                    return ApiResult<T>.Success(default!);
                }

                var value =
                    await response.Content
                        .ReadFromJsonAsync<T>(
                            JsonOptions,
                            cancellationToken);

                return value is null
                    ? ApiResult<T>.Failure(
                        "The API returned an empty response.")
                    : ApiResult<T>.Success(value);
            }

            var error =
                await ExtractErrorAsync(
                    response,
                    cancellationToken);

            return ApiResult<T>.Failure(error);
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.Failure(
                "Unable to connect to the DVLD API.");
        }
        catch (TaskCanceledException)
        {
            return ApiResult<T>.Failure(
                "The request to the DVLD API timed out.");
        }
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string requestUri)
    {
        var request =
            new HttpRequestMessage(
                method,
                requestUri);

        AddAuthorizationHeader(request);

        return request;
    }

    private HttpRequestMessage CreateRequest<T>(
        HttpMethod method,
        string requestUri,
        T content)
    {
        var request =
            CreateRequest(
                method,
                requestUri);

        request.Content =
            JsonContent.Create(
                content,
                options: JsonOptions);

        return request;
    }

    private void AddAuthorizationHeader(
        HttpRequestMessage request)
    {
        if (!_currentUser.IsLoggedIn)
            return;

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _currentUser.AccessToken);
    }

    private static async Task<string> ExtractErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    "Authentication is required.",

                HttpStatusCode.Forbidden =>
                    "You are not authorized to perform this operation.",

                HttpStatusCode.NotFound =>
                    "The requested resource was not found.",

                HttpStatusCode.Conflict =>
                    "The operation conflicts with the current data.",

                HttpStatusCode.BadRequest =>
                    "The request is invalid.",

                _ =>
                    "The API request failed."
            };
        }

        try
        {
            using var document =
                JsonDocument.Parse(content);

            if (document.RootElement.TryGetProperty(
                    "error",
                    out var error))
            {
                return error.GetString()
                    ?? "The API request failed.";
            }

            if (document.RootElement.TryGetProperty(
                    "message",
                    out var message))
            {
                return message.GetString()
                    ?? "The API request failed.";
            }

            if (document.RootElement.TryGetProperty(
                    "detail",
                    out var detail))
            {
                return detail.GetString()
                    ?? "The API request failed.";
            }
        }
        catch (JsonException)
        {
            // Return the raw response when it is not valid JSON.
        }

        return content;
    }
}