using Application.Common.Results;
using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPersonService
    {
        Task<Result<List<PersonDto>>> GetAllPeopleAsync();

        Task<Result<PersonDto>> GetPersonByIdAsync(int id);

        Task<Result<PersonDto>> GetPersonByNationalNoAsync(string nationalNo);

        Task<Result<int>> AddPersonAsync(PersonCreateUpdateDto personDto);

        Task<Result> UpdatePersonAsync(int id, PersonCreateUpdateDto personDto);

        Task<Result> DeletePersonAsync(int id);

        Task<bool> IsPersonExistsAsync(int id);
    }
}