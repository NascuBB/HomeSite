using Microsoft.EntityFrameworkCore;

namespace HomeSite.Entities
{
	public class UserDBContext : DbContext
	{
        //DbContextOptions<UserDBContext> options
        //options
        public UserDBContext() : base() { }

        public DbSet<UserAccount> UserAccounts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            optionsBuilder.UseNpgsql("Host=localhost;Port=5008;Database=just1x;Username=postgres;Password=postgres");
#else
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=just1x;Username=postgres;Password=postgres");
#endif
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
		}
	}
}
