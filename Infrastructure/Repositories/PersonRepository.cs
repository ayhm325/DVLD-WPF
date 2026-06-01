using Microsoft.EntityFrameworkCore;
using DVLD.Domain.Entities;

namespace Infrastructure.Repositories
{
    public class PersonRepository
    {
        private readonly DVLDDbContext _context;

        public PersonRepository(DVLDDbContext context)
        {
            _context = context;
        }

        public Person? GetPersonById(int id)
        {            
            return _context.People
                .Include(p => p.Country)
                .FirstOrDefault(p => p.PersonId == id);
        }

        public Person? GetPersonByNationalNo(string nationalNo)
        {
            return _context.People
                .Include(p => p.Country)
                .FirstOrDefault(p => p.NationalNo == nationalNo);
        }
    

        public List<Person> GetAllPersons()
        {
            return _context.People
         .Include(p => p.Country)
         .ToList();
        }
        
        public int AddPerson(Person person)
        {
            _context.People.Add(person);
            _context.SaveChanges();
            return person.PersonId;
        }

        public bool UpdatePerson(Person person)
        {
            var existing = _context.People.FirstOrDefault(p => p.PersonId == person.PersonId);
            if (existing == null)
                return false;

            existing.NationalNo = person.NationalNo;
            existing.FirstName = person.FirstName;
            existing.SecondName = person.SecondName;
            existing.ThirdName = person.ThirdName;
            existing.LastName = person.LastName;
            existing.DateOfBirth = person.DateOfBirth;
            existing.Gender = person.Gender;
            existing.Address = person.Address;
            existing.Phone = person.Phone;
            existing.Email = person.Email;
            existing.NationalityCountryID = person.NationalityCountryID;
            existing.ImagePath = person.ImagePath;
          
            return _context.SaveChanges() > 0;

        }

        public bool IsPersonExistsById(int id)
        {
            return _context.People.Any(p => p.PersonId == id);
        }

        public bool IsNationalNoDuplicated(string nationalNo, int id)
        {
            return _context.People.Any(p =>
                p.NationalNo == nationalNo && p.PersonId != id);
        }


        public bool DeletePerson(int id)
        {
            var person = _context.People.Find(id);

            if (person == null)
                return false;

            _context.People.Remove(person);
            return _context.SaveChanges() > 0;
        }


    }
}
