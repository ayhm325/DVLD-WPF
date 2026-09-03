using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
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

                if (result is null)
                {
                    return Result<LoginResponseDto>.FromFailure(
                        "The API returned an empty response.");
                }

                return Result<LoginResponseDto>.Success(result);
            }

            var error =
                await response.Content.ReadAsStringAsync();

            var message = ExtractErrorMessage(error);

            return Result<LoginResponseDto>.FromFailure(message);
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
        catch (Exception ex)
        {
            return Result<LoginResponseDto>.FromFailure(
                $"An error occurred: {ex.Message}");
        }
    }

    private static string ExtractErrorMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "The API request failed.";

        try
        {
            using var document =
                JsonDocument.Parse(content);

            if (document.RootElement.TryGetProperty(
                    "message",
                    out var message))
            {
                return message.GetString()
                    ?? "The API request failed.";
            }
        }
        catch (JsonException)
        {
        }

        return content;
    }
}