using Application.Common.Pagination;
using Application.DTOs.PersonDTO;
using Domain.Entities;

namespace Application.Interfaces;

public interface IPersonRepository
{
    Task<PagedResult<Person>> GetAllAsync(
        PersonQueryRequest request);

    Task<PagedResult<Person>> GetAllAsync(
        PaginationRequest request);

    Task<Person?> GetPersonByIdAsync(int id);

    Task<Person?> GetPersonByNationalNoAsync(
        string nationalNo);

    Task<PersonStatisticsDto> GetStatisticsAsync();

    Task<List<Person>> GetAllPersonsAsync();

    Task<Person?> GetPersonForUpdateAsync(int id);

    Task<bool> IsPersonExistsByIdAsync(int id);

    Task<bool> IsNationalNoDuplicatedAsync(
        string nationalNo,
        int personId);

    Task<bool> HasApplicationsAsync(int personId);

    Task AddPersonAsync(Person person);

    Task<bool> DeletePersonAsync(int id);
}
