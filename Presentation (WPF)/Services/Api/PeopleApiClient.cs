using Presentation.Services.Results;
using ContractPerson = DVLD.Contracts.Person;

namespace Presentation.Services.Api;

public sealed class PeopleApiClient(
    IApiClient apiClient) : IPeopleApiClient
{
    private readonly IApiClient _apiClient =
        apiClient
        ?? throw new ArgumentNullException(nameof(apiClient));

    public Task<ApiResult<List<ContractPerson.PersonResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return _apiClient.GetAsync<List<ContractPerson.PersonResponse>>(
            "api/people",
            cancellationToken);
    }

    public Task<ApiResult<ContractPerson.PersonResponse>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Task.FromResult(
                ApiResult<ContractPerson.PersonResponse>.Failure(
                    "Person ID must be greater than zero."));
        }

        return _apiClient.GetAsync<ContractPerson.PersonResponse>(
            $"api/people/{id}",
            cancellationToken);
    }

    public Task<ApiResult<ContractPerson.PersonResponse>> GetByNationalNoAsync(
        string nationalNo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nationalNo))
        {
            return Task.FromResult(
                ApiResult<ContractPerson.PersonResponse>.Failure(
                    "National number is required."));
        }

        return _apiClient.GetAsync<ContractPerson.PersonResponse>(
            $"api/people/national/{Uri.EscapeDataString(nationalNo.Trim())}",
            cancellationToken);
    }

    public async Task<ApiResult<int>> CreateAsync(
        ContractPerson.CreatePersonRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result =
            await _apiClient.PostAsync<
                ContractPerson.CreatePersonRequest,
                ContractPerson.CreatePersonResponse>(
                "api/people",
                request,
                cancellationToken);

        if (result.IsFailure)
            return ApiResult<int>.Failure(result.Error);

        if (result.Value is null ||
            result.Value.PersonId <= 0)
        {
            return ApiResult<int>.Failure(
                "The API returned an invalid person ID.");
        }

        return ApiResult<int>.Success(
            result.Value.PersonId);
    }

    public Task<ApiResult> UpdateAsync(
        int id,
        ContractPerson.UpdatePersonRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (id <= 0)
        {
            return Task.FromResult(
                ApiResult.Failure(
                    "Person ID must be greater than zero."));
        }

        return _apiClient.PutAsync(
            $"api/people/{id}",
            request,
            cancellationToken);
    }

    public Task<ApiResult> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Task.FromResult(
                ApiResult.Failure(
                    "Person ID must be greater than zero."));
        }

        return _apiClient.DeleteAsync(
            $"api/people/{id}",
            cancellationToken);
    }
}