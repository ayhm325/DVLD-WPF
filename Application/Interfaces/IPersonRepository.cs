using Application.Common.Pagination;
using Application.DTOs.PersonDTO;
using Domain.Entities;

namespace Application.Interfaces;

public interface IPersonRepository
{
    Task<PagedResult<Person>> GetAllAsync(
        PersonQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Person>> GetPeopleAvailableForUserAsync(
        PersonQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Person>> GetAllAsync(
        PaginationRequest request,
        CancellationToken cancellationToken = default);

    Task<Person?> GetPersonByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Person?> GetPersonByNationalNoAsync(
        string nationalNo,
        CancellationToken cancellationToken = default);

    Task<PersonStatisticsDto> GetStatisticsAsync(
        CancellationToken cancellationToken = default);

    Task<List<Person>> GetAllPersonsAsync(
        CancellationToken cancellationToken = default);

    Task<Person?> GetPersonForUpdateAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> IsPersonExistsByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> IsNationalNoDuplicatedAsync(
        string nationalNo,
        int personId,
        CancellationToken cancellationToken = default);

    Task<bool> HasApplicationsAsync(
        int personId,
        CancellationToken cancellationToken = default);

    Task AddPersonAsync(
        Person person,
        CancellationToken cancellationToken = default);

    Task<bool> DeletePersonAsync(
        int id,
        CancellationToken cancellationToken = default);
}
