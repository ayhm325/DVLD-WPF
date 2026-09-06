using Application.Common.Results;
using Application.DTOs.PersonDTO;

namespace Presentation.Services.Api;

public sealed class PeopleApiClient(
    IApiClient apiClient) : IPeopleApiClient
{
    private readonly IApiClient _apiClient =
        apiClient
        ?? throw new ArgumentNullException(nameof(apiClient));

    public Task<Result<List<PersonDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return _apiClient.GetAsync<List<PersonDto>>(
            "api/people",
            cancellationToken);
    }

    public Task<Result<PersonDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Task.FromResult(
                Result<PersonDto>.FromValidationFailure(
                    "Person ID must be greater than zero."));
        }

        return _apiClient.GetAsync<PersonDto>(
            $"api/people/{id}",
            cancellationToken);
    }

    public Task<Result<PersonDto>> GetByNationalNoAsync(
        string nationalNo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nationalNo))
        {
            return Task.FromResult(
                Result<PersonDto>.FromValidationFailure(
                    "National number is required."));
        }

        return _apiClient.GetAsync<PersonDto>(
            $"api/people/national/{Uri.EscapeDataString(nationalNo.Trim())}",
            cancellationToken);
    }

    public async Task<Result<int>> CreateAsync(
        PersonCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var result =
            await _apiClient.PostAsync<
                PersonCreateDto,
                PersonCreateResponse>(
                "api/people",
                dto,
                cancellationToken);

        if (result.IsFailure)
            return Result<int>.FromResult(result);

        if (result.Value is null ||
            result.Value.PersonId <= 0)
        {
            return Result<int>.FromFailure(
                "The API returned an invalid person ID.");
        }

        return Result<int>.Success(
            result.Value.PersonId);
    }

    public Task<Result> UpdateAsync(
        int id,
        PersonUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (id <= 0)
        {
            return Task.FromResult(
                Result.ValidationFailure(
                    "Person ID must be greater than zero."));
        }

        return _apiClient.PutAsync(
            $"api/people/{id}",
            dto,
            cancellationToken);
    }

    public Task<Result> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Task.FromResult(
                Result.ValidationFailure(
                    "Person ID must be greater than zero."));
        }

        return _apiClient.DeleteAsync(
            $"api/people/{id}",
            cancellationToken);
    }

    private sealed class PersonCreateResponse
    {
        public int PersonId { get; init; }
    }
}