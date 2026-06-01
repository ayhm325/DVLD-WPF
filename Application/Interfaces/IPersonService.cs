using Application.DTOs;
using DVLD.Domain.Entities;

namespace Application.Interfaces
{
    public interface IPersonService
    {
        List<PersonDto> GetAllPeople();
        PersonDto? GetPersonById(int id);
        PersonDto? GetPersonByNationalNo(string nationalNo);

        int AddPerson(Person person);
        bool UpdatePerson(Person person);
        bool DeletePerson(int id);
        bool IsPersonExists(int id);
    }
}
