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
}