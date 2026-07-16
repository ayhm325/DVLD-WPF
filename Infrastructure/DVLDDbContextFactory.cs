using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure
{
    public class DVLDDbContextFactory : IDesignTimeDbContextFactory<DVLDDbContext>
    {
        public DVLDDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DVLDDbContext>();

            optionsBuilder.UseSqlServer(
                "Server=.;Database=DVLDf;Trusted_Connection=True;TrustServerCertificate=True;"
            );

            return new DVLDDbContext(optionsBuilder.Options);
        }
    }
}