using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class PersonService : IPersonService
    {
        private readonly IPersonRepository _personRepository;

        public PersonService(IPersonRepository personRepository)
        {
            _personRepository = personRepository;
        }

        // ================= GET ALL =================
        public async Task<Result<List<PersonDto>>> GetAllPeopleAsync()
        {
            var people = await _personRepository.GetAllPersonsAsync();

            return Result<List<PersonDto>>.Success(
                people.Select(MapToDto).ToList());
        }

        // ================= GET BY ID =================
        public async Task<Result<PersonDto>> GetPersonByIdAsync(int id)
        {
            var p = await _personRepository.GetPersonByIdAsync(id);

            if (p == null)
                return Result<PersonDto>.Fail("الشخص غير موجود.");

            return Result<PersonDto>.Success(MapToDto(p));
        }

        // ================= GET BY NATIONAL NO =================
        public async Task<Result<PersonDto>> GetPersonByNationalNoAsync(string nationalNo)
        {
            if (string.IsNullOrWhiteSpace(nationalNo))
                return Result<PersonDto>.Fail("الرقم الوطني مطلوب.");

            var p = await _personRepository.GetPersonByNationalNoAsync(nationalNo);

            if (p == null)
                return Result<PersonDto>.Fail("لا يوجد شخص بهذا الرقم الوطني.");

            return Result<PersonDto>.Success(MapToDto(p));
        }

        // ================= EXISTS =================
        public async Task<bool> IsPersonExistsAsync(int id)
        {
            return await _personRepository.IsPersonExistsByIdAsync(id);
        }

        // ================= ADD =================
        public async Task<Result<int>> AddPersonAsync(PersonCreateUpdateDto dto)
        {
            if (dto == null)
                return Result<int>.Fail("بيانات الشخص مطلوبة.");

            var person = MapToEntity(dto);

            var validation = PersonValidator.Validate(person);

            if (!validation.IsValid)
            {
                return Result<int>.Fail(string.Join(" | ", validation.Errors));
            }

            if (await _personRepository.IsNationalNoDuplicatedAsync(person.NationalNo, 0))
                return Result<int>.Fail("الرقم الوطني مسجل مسبقاً.");

            int id = await _personRepository.AddPersonAsync(person);

            return Result<int>.Success(id);
        }

        // ================= UPDATE =================
        public async Task<Result> UpdatePersonAsync(int id, PersonCreateUpdateDto dto)
        {
            if (id <= 0)
                return Result.Failure("معرف الشخص غير صالح.");

            if (dto == null)
                return Result.Failure("بيانات الشخص مطلوبة.");

            if (!await _personRepository.IsPersonExistsByIdAsync(id))
                return Result.Failure("الشخص غير موجود.");

            var person = MapToEntity(dto);
            person.PersonId = id;

            var validation = PersonValidator.Validate(person);

            if (!validation.IsValid)
            {
                var errors = string.Join(" | ", validation.Errors);
                return Result.Failure(errors);
            }

            if (await _personRepository.IsNationalNoDuplicatedAsync(person.NationalNo, id))
                return Result.Failure("الرقم الوطني مسجل مسبقاً لشخص آخر.");

            var isSuccess = await _personRepository.UpdatePersonAsync(person);

            return isSuccess
                ? Result.Success()
                : Result.Failure("فشل في تحديث بيانات الشخص.");
        }

        // ================= DELETE =================
        public async Task<Result> DeletePersonAsync(int id)
        {
            if (!await _personRepository.IsPersonExistsByIdAsync(id))
                return Result.Failure("الشخص غير موجود.");

            var isSuccess = await _personRepository.DeletePersonAsync(id);

            return isSuccess
                ? Result.Success()
                : Result.Failure("فشل في حذف الشخص.");
        }

        // ================= MAPPING =================
        private PersonDto MapToDto(Person p)
        {
            return new PersonDto
            {
                PersonId = p.PersonId,
                NationalNo = p.NationalNo,
                FullName = p.FullName,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender,
                Address = p.Address,
                Phone = p.Phone,
                Email = p.Email,
                CountryName = p.Country?.CountryName ?? "Unknown",
                ImagePath = p.ImagePath
            };
        }

        private Person MapToEntity(PersonCreateUpdateDto dto)
        {
            return new Person
            {
                NationalNo = dto.NationalNo,
                FirstName = dto.FirstName,
                SecondName = dto.SecondName,
                ThirdName = dto.ThirdName,
                LastName = dto.LastName,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                Address = dto.Address,
                Phone = dto.Phone,
                Email = dto.Email,
                NationalityCountryID = dto.NationalityCountryID,
                ImagePath = dto.ImagePath
            };
        }
    }
}