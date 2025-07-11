using CEApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CEApi.Data
{
    public class MsSqlDatabaseContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }

        public MsSqlDatabaseContext(DbContextOptions<MsSqlDatabaseContext> options)
            : base(options)
        {
        }
    }
}
