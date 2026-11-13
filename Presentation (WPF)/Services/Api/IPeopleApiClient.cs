using Presentation.Services.Results;
using ContractPerson = DVLD.Contracts.Person;

namespace Presentation.Services.Api;

public interface IPeopleApiClient
{
    Task<ApiResult<List<ContractPerson.PersonListResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<ContractPerson.PersonResponse>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ApiResult<ContractPerson.PersonResponse>> GetByNationalNoAsync(
        string nationalNo,
        CancellationToken cancellationToken = default);

    Task<ApiResult<int>> CreateAsync(
        ContractPerson.CreatePersonRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult> UpdateAsync(
        int id,
        ContractPerson.UpdatePersonRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ApiResult> UploadPhotoAsync(
        int id,
        string filePath,
        CancellationToken cancellationToken = default);

    Task<ApiResult<byte[]>> GetPhotoAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ApiResult> DeletePhotoAsync(
        int id,
        CancellationToken cancellationToken = default);
}
