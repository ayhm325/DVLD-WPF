using Application.Common.Results;
using Application.DTOs.PersonDTO;

namespace Presentation.Services.Api;

public interface IPeopleApiClient
{
    Task<Result<List<PersonDto>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Result<PersonDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Result<PersonDto>> GetByNationalNoAsync(
        string nationalNo,
        CancellationToken cancellationToken = default);

    Task<Result<int>> CreateAsync(
        PersonCreateDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(
        int id,
        PersonUpdateDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}