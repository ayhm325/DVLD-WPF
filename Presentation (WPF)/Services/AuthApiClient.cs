using DVLD.Contracts.Auth;
using Presentation.Services.Results;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Presentation.Services;

public sealed class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ICurrentUserSession _currentUser;

    public AuthApiClient(
        HttpClient httpClient,
        ICurrentUserSession currentUser)
    {
        _httpClient = httpClient
            ?? throw new ArgumentNullException(nameof(httpClient));

        _currentUser = currentUser
            ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public async Task<ApiResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/login",
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result =
                    await response.Content.ReadFromJsonAsync<LoginResponse>(
                        cancellationToken);

                return result is null
                    ? ApiResult<LoginResponse>.Failure(
                        "The API returned an empty response.")
                    : ApiResult<LoginResponse>.Success(result);
            }

            var error = await ExtractErrorMessageAsync(response);

            return ApiResult<LoginResponse>.Failure(error);
        }
        catch (HttpRequestException)
        {
            return ApiResult<LoginResponse>.Failure(
                "Unable to connect to the DVLD API.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return ApiResult<LoginResponse>.Failure(
                "The request to the DVLD API timed out.");
        }
    }

    public async Task<ApiResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            using var httpRequest =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "api/auth/change-password");

            httpRequest.Content =
                JsonContent.Create(request);

            if (!string.IsNullOrWhiteSpace(
                    _currentUser.AccessToken))
            {
                httpRequest.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        _currentUser.AccessToken);
            }

            using var response =
                await _httpClient.SendAsync(
                    httpRequest,
                    cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return ApiResult.Success();
            }

            var error =
                await ExtractErrorMessageAsync(response);

            return ApiResult.Failure(error);
        }
        catch (HttpRequestException)
        {
            return ApiResult.Failure(
                "Unable to connect to the DVLD API.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return ApiResult.Failure(
                "The request to the DVLD API timed out.");
        }
    }

    private static async Task<string> ExtractErrorMessageAsync(
        HttpResponseMessage response)
    {
        var content =
            await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(content))
        {
            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    "Authentication failed.",

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
            // Return the raw response when it is not valid JSON.
        }

        return content;
    }
}