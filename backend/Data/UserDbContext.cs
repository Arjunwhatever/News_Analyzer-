using Microsoft.EntityFrameworkCore;
using Vector.Server.Entities;

namespace Vector.Server.Data
{
    public class UserDbContext(DbContextOptions<UserDbContext> options)
        : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
    }
}