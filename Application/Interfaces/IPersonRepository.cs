using Domain.Entities;

namespace Application.Interfaces
{
    public interface IPersonRepository
    {
        // =========================
        // GET
        // =========================

        Task<Person?> GetPersonByIdAsync(int id);

        Task<Person?> GetPersonByNationalNoAsync(
            string nationalNo);

        Task<List<Person>> GetAllPersonsAsync();

        // =========================
        // CHECKS
        // =========================

        Task<bool> IsPersonExistsByIdAsync(
            int id);

        Task<bool> IsNationalNoDuplicatedAsync(
            string nationalNo,
            int personId);

        // =========================
        // CREATE
        // =========================

        Task<int> AddPersonAsync(
            Person person);

        // =========================
        // UPDATE
        // =========================

        Task<bool> UpdatePersonAsync(
            Person person);

        // =========================
        // DELETE
        // =========================

        Task<bool> DeletePersonAsync(
            int id);
    }
}