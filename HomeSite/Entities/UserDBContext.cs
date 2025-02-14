using Microsoft.EntityFrameworkCore;

namespace HomeSite.Entities
{
	public class UserDBContext : DbContext
	{
        public UserDBContext(DbContextOptions<UserDBContext> options) : base(options)
        {
            
        }

        public DbSet<UserAccount> UserAccounts { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
		}
	}
}
