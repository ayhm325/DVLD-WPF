using Domain.Entities;

namespace Application.Interfaces
{
    public interface IPersonRepository
    {
        Task<Person?> GetPersonByIdAsync(int id);

        Task<Person?> GetPersonByNationalNoAsync(string nationalNo);

        Task<List<Person>> GetAllPersonsAsync();


        Task<bool> IsPersonExistsByIdAsync(int id);

        Task<bool> IsNationalNoDuplicatedAsync(
            string nationalNo,
            int id);


        Task<int> AddPersonAsync(Person person);

        Task<bool> UpdatePersonAsync(Person person);

        Task<bool> DeletePersonAsync(int id);
    }
}