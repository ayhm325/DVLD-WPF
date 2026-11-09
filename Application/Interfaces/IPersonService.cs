using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs.PersonDTO;

namespace Application.Interfaces;

public interface IPersonService
{
    Task<Result<PagedResult<PersonDto>>> GetAllPeopleAsync(
        PersonQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<PersonDto>>> GetPeopleAvailableForUserAsync(
        PersonQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<PersonDto>>> GetAllPeopleAsync(
        PaginationRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<List<PersonDto>>> GetAllPeopleAsync(
        CancellationToken cancellationToken = default);

    Task<Result<PersonStatisticsDto>> GetStatisticsAsync(
        CancellationToken cancellationToken = default);

    Task<Result<PersonDto>> GetPersonByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Result<PersonDto>> GetPersonByNationalNoAsync(
        string nationalNo,
        CancellationToken cancellationToken = default);

    Task<Result<int>> AddPersonAsync(
        PersonCreateDto personDto,
        CancellationToken cancellationToken = default);

    Task<Result> UpdatePersonAsync(
        int id,
        PersonUpdateDto personDto,
        CancellationToken cancellationToken = default);

    Task<Result> DeletePersonAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> IsPersonExistsAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Result<string>> UploadPersonPhotoAsync(
        int personId,
        Stream photoStream,
        long length,
        string? contentType,
        CancellationToken cancellationToken = default);

    Task<Result<(Stream Stream, string ContentType, string StorageKey)>> GetPersonPhotoAsync(
        int personId,
        CancellationToken cancellationToken = default);

    Task<Result> DeletePersonPhotoAsync(
        int personId,
        CancellationToken cancellationToken = default);
}
