using Microsoft.EntityFrameworkCore;

namespace HomeSite.Entities
{
    public class ServerDBContext : DbContext
    {
        //DbContextOptions<UserDBContext> options

        //options
        public ServerDBContext() : base()
        {

        }

        public DbSet<Server> Servers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=just1x;Username=postgres;Password=postgres");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
