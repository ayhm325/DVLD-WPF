using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using DVLD.Domain.Entities;
using DVLD.Domain.Enums;
using Infrastructure.Repositories;

namespace DVLD.Application.Services
{
    public class PersonService : IPersonService
    {
        private readonly PersonRepository _personRepository;

        public PersonService(PersonRepository personRepository)
        {
            _personRepository = personRepository;
        }

        public List<PersonDto> GetAllPeople()
        {
            var people = _personRepository.GetAllPersons();

            return people.Select(p => new PersonDto
            {
                PersonId = p.PersonId,
                NationalNo = p.NationalNo,

                FullName = string.Join(" ",
                    new[] { p.FirstName, p.SecondName, p.ThirdName, p.LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x))),

                DateOfBirth = p.DateOfBirth,

                Gender = p.Gender == Gender.Male ? "Male" : "Female",

                Address = p.Address,
                Phone = p.Phone,
                Email = p.Email,
                CountryName = p.Country?.CountryName ?? "Unknown",
                ImagePath = p.ImagePath
            }).ToList();
        }
        //public List<PersonDto> GetAllPeople()
        //{
        //    var people = _personRepository.GetAllPersons();
        //    return people.Select(p => new PersonDto
        //    {
        //        PersonId = p.PersonId,
        //        NationalNo = p.NationalNo,
        //        FullName = $"{p.FirstName} {p.SecondName} {p.ThirdName} {p.LastName}",
        //        DateOfBirth = p.DateOfBirth,
        //        Gender = p.Gender == Gender.Male ? "Male" : "Female",
        //        Address = p.Address,
        //        Phone = p.Phone,
        //        CountryName = p.Country?.CountryName ?? string.Empty
        //    }).ToList();

        //}

        public PersonDto? GetPersonById(int id)
        {
            var p = _personRepository.GetPersonById(id);

            if (p == null) return null;

            return new PersonDto
            {
                PersonId = p.PersonId,
                NationalNo = p.NationalNo,
                FullName = $"{p.FirstName} {p.SecondName} {p.ThirdName} {p.LastName}",
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender == Gender.Male ? "Male" : "Female",
                Address = p.Address,
                Phone = p.Phone,
                CountryName = p.Country?.CountryName ?? "",
                ImagePath = p.ImagePath
            };
        }

        public PersonDto? GetPersonByNationalNo(string nationalNo)
        {
            var p = _personRepository.GetPersonByNationalNo(nationalNo);

            if (p == null) return null;

            return new PersonDto
            {
                PersonId = p.PersonId,
                NationalNo = p.NationalNo,
                FullName = $"{p.FirstName} {p.SecondName} {p.ThirdName} {p.LastName}",
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender == Gender.Male ? "Male" : "Female",
                Address = p.Address,
                Phone = p.Phone,
                CountryName = p.Country?.CountryName ?? "",
                ImagePath = p.ImagePath
            };
        }

        public List<Person> GetAllPersons()
        {
            return _personRepository.GetAllPersons();
        }

        public bool IsPersonExists(int id)
        {
            return _personRepository.IsPersonExistsById(id);
        }

        public int AddPerson(Person person)
        {
            if (!PersonValidator.Validate(person, out _))
                return -1;

            if (_personRepository.IsNationalNoDuplicated(person.NationalNo, 0))
                return -1;

            return _personRepository.AddPerson(person);
        }

        public bool UpdatePerson(Person person)
        {
            if (person.PersonId <= 0)
                return false;

            if (_personRepository.IsNationalNoDuplicated(person.NationalNo, person.PersonId))
                return false;

            return _personRepository.UpdatePerson(person);
        }

        public bool DeletePerson(int id)
        {
            if (!_personRepository.IsPersonExistsById(id))
                return false;

            return _personRepository.DeletePerson(id);
        }
    }
}