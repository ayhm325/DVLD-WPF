using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using DVLD.Domain.Entities;
using DVLD.Domain.Enums;
using Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class PersonService : IPersonService
    {
        private readonly PersonRepository _personRepository;

        public PersonService(PersonRepository personRepository)
        {
            _personRepository = personRepository;
        }

        // ================= GET ALL =================
        public async Task<List<PersonDto>> GetAllPeopleAsync()
        {
            // استدعاء مباشر لـ Async - لا حاجة لـ Task.Run
            var people = await _personRepository.GetAllPersonsAsync();
            return people.Select(MapToDto).ToList();
        }

        // ================= GET BY ID =================
        public async Task<PersonDto?> GetPersonByIdAsync(int id)
        {
            var p = await _personRepository.GetPersonByIdAsync(id);
            return p == null ? null : MapToDto(p);
        }

        // ================= GET BY NATIONAL NO =================
        public async Task<PersonDto?> GetPersonByNationalNoAsync(string nationalNo)
        {
            var p = await _personRepository.GetPersonByNationalNoAsync(nationalNo);
            return p == null ? null : MapToDto(p);
        }

        // ================= EXISTS =================
        public async Task<bool> IsPersonExistsAsync(int id)
        {
            return await _personRepository.IsPersonExistsByIdAsync(id);
        }

        // ================= ADD =================
        public async Task<int> AddPersonAsync(PersonCreateUpdateDto dto)
        {
            var person = MapToEntity(dto);

            var validation = PersonValidator.Validate(person);
            if (!validation.IsValid) return -1;

            // التأكد من أن الفحص أيضاً Async
            if (await _personRepository.IsNationalNoDuplicatedAsync(person.NationalNo, 0))
                return -1;

            return await _personRepository.AddPersonAsync(person);
        }

        // ================= UPDATE =================
        public async Task<bool> UpdatePersonAsync(int id, PersonCreateUpdateDto dto)
        {
            if (id <= 0) return false;

            var person = MapToEntity(dto);
            person.PersonId = id;

            var validation = PersonValidator.Validate(person);
            if (!validation.IsValid) return false;

            if (await _personRepository.IsNationalNoDuplicatedAsync(person.NationalNo, id))
                return false;

            return await _personRepository.UpdatePersonAsync(person);
        }

        // ================= DELETE =================
        public async Task<bool> DeletePersonAsync(int id)
        {
            if (!await _personRepository.IsPersonExistsByIdAsync(id))
                return false;

            return await _personRepository.DeletePersonAsync(id);
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
                Gender = p.Gender == Gender.Male ? "Male" : "Female",
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

