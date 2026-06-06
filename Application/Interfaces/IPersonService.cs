using Application.DTOs;


namespace Application.Interfaces
{
    public interface IPersonService
    {
        Task<List<PersonDto>> GetAllPeopleAsync();

        Task<PersonDto?> GetPersonByIdAsync(int id);

        Task<PersonDto?> GetPersonByNationalNoAsync(string nationalNo);

        Task<int> AddPersonAsync(PersonCreateUpdateDto personDto);

        Task<bool> UpdatePersonAsync(int id, PersonCreateUpdateDto personDto);

        Task<bool> DeletePersonAsync(int id);

        Task<bool> IsPersonExistsAsync(int id);
       
    }
}
