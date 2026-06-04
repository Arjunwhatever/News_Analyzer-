using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vector.Server.Data
{
    public class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
    {
        public UserDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();

            optionsBuilder.UseSqlServer(
                "Server=127.0.0.1,1433;Database=VectorDb;User Id=sa;Password=Your_password123;TrustServerCertificate=true");

            return new UserDbContext(optionsBuilder.Options);
        }
    }
}