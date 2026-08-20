using Application.Common.Results;
using Application.DTOs.PersonDTO;

namespace Application.Interfaces
{
    public interface IPersonService
    {
        // =========================
        // GET
        // =========================

        Task<Result<List<PersonDto>>> GetAllPeopleAsync();

        Task<Result<PersonDto>> GetPersonByIdAsync(
            int id);

        Task<Result<PersonDto>> GetPersonByNationalNoAsync(
            string nationalNo);

        // =========================
        // CREATE
        // =========================

        Task<Result<int>> AddPersonAsync(
            PersonCreateUpdateDto personDto);

        // =========================
        // UPDATE
        // =========================

        Task<Result> UpdatePersonAsync(
            int id,
            PersonCreateUpdateDto personDto);

        // =========================
        // DELETE
        // =========================

        Task<Result> DeletePersonAsync(
            int id);

        // =========================
        // CHECKS
        // =========================

        Task<bool> IsPersonExistsAsync(
            int id);
    }
}