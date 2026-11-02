using Application.Common.Results;
using Application.DTOs.PersonDTO;

namespace Application.Interfaces;

public interface IPersonService
{
    Task<Result<List<PersonDto>>> GetAllPeopleAsync();

    Task<Result<PersonDto>> GetPersonByIdAsync(
        int id);

    Task<Result<PersonDto>> GetPersonByNationalNoAsync(
        string nationalNo);

    Task<Result<int>> AddPersonAsync(
        PersonCreateDto personDto);

    Task<Result> UpdatePersonAsync(
        int id,
        PersonUpdateDto personDto);

    Task<Result> DeletePersonAsync(
        int id);

    Task<bool> IsPersonExistsAsync(
        int id);

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
