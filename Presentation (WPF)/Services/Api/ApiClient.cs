using Application.Common.Results;
using Application.Interfaces;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Presentation.Services.Api;

public sealed class ApiClient(
    HttpClient httpClient,
    ICurrentUserService currentUser) : IApiClient
{
    private readonly HttpClient _httpClient =
        httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    private readonly ICurrentUserService _currentUser =
        currentUser ?? throw new ArgumentNullException(nameof(currentUser));

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public Task<Result<T>> GetAsync<T>(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(
            () => CreateRequest(HttpMethod.Get, requestUri),
            cancellationToken);
    }

    public Task<Result<TResponse>> PostAsync<TRequest, TResponse>(
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

    public async Task<Result> PutAsync<TRequest>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<object?>(
            () => CreateRequest(
                HttpMethod.Put,
                requestUri,
                request),
            cancellationToken);

        return result.IsSuccess
            ? Result.Success()
            : Result.FromFailure(result);
    }

    public async Task<Result> DeleteAsync(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<object?>(
            () => CreateRequest(
                HttpMethod.Delete,
                requestUri),
            cancellationToken);

        return result.IsSuccess
            ? Result.Success()
            : Result.FromFailure(result);
    }

    private async Task<Result<T>> SendAsync<T>(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = requestFactory();

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return Result<T>.Success(default!);

                var value =
                    await response.Content.ReadFromJsonAsync<T>(
                        JsonOptions,
                        cancellationToken);

                return value is null
                    ? Result<T>.FromFailure(
                        "The API returned an empty response.")
                    : Result<T>.Success(value);
            }

            var error = await ExtractErrorAsync(
                response,
                cancellationToken);

            return CreateFailure<T>(
                response.StatusCode,
                error);
        }
        catch (HttpRequestException)
        {
            return Result<T>.FromFailure(
                "Unable to connect to the DVLD API.");
        }
        catch (TaskCanceledException)
        {
            return Result<T>.FromFailure(
                "The request to the DVLD API timed out.");
        }
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string requestUri)
    {
        var request = new HttpRequestMessage(
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
        var request = CreateRequest(
            method,
            requestUri);

        request.Content = JsonContent.Create(
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

    private static Result<T> CreateFailure<T>(
        HttpStatusCode statusCode,
        string error)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest =>
                Result<T>.FromValidationFailure(error),

            HttpStatusCode.Unauthorized =>
                Result<T>.FromFailure(error),

            HttpStatusCode.Forbidden =>
                Result<T>.FromForbidden(error),

            HttpStatusCode.NotFound =>
                Result<T>.FromNotFound(error),

            HttpStatusCode.Conflict =>
                Result<T>.FromConflict(error),

            _ =>
                Result<T>.FromFailure(error)
        };
    }

    private static async Task<string> ExtractErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content =
            await response.Content.ReadAsStringAsync(
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
        }

        return content;
    }
}