using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Presentation.Services;

public sealed class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient;

    public AuthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient
            ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(
        LoginRequestDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            using var response =
                await _httpClient.PostAsJsonAsync(
                    "api/auth/login",
                    dto);

            if (response.IsSuccessStatusCode)
            {
                var result =
                    await response.Content
                        .ReadFromJsonAsync<LoginResponseDto>();

                return result is null
                    ? Result<LoginResponseDto>.FromFailure(
                        "The API returned an empty response.")
                    : Result<LoginResponseDto>.Success(result);
            }

            var error =
                await ExtractErrorMessageAsync(response);

            return response.StatusCode switch
            {
                HttpStatusCode.BadRequest =>
                    Result<LoginResponseDto>.FromValidationFailure(error),

                HttpStatusCode.Forbidden =>
                    Result<LoginResponseDto>.FromForbidden(error),

                HttpStatusCode.NotFound =>
                    Result<LoginResponseDto>.FromNotFound(error),

                HttpStatusCode.Conflict =>
                    Result<LoginResponseDto>.FromConflict(error),

                _ =>
                    Result<LoginResponseDto>.FromFailure(error)
            };
        }
        catch (HttpRequestException)
        {
            return Result<LoginResponseDto>.FromFailure(
                "Unable to connect to the DVLD API.");
        }
        catch (TaskCanceledException)
        {
            return Result<LoginResponseDto>.FromFailure(
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