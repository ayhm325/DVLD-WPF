
using DVLD.Domain.Entities;


namespace Infrastructure.Repositories
{
    public class CountryRepository
    {
        private readonly DVLDDbContext _context;

        public CountryRepository(DVLDDbContext context)
        {
            _context = context;
        }

        public List<Country> GetAll()
        {
            return _context.Countries.ToList();
        }
    }
}
