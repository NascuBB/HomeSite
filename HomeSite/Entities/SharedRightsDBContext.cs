using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeSite.Entities
{
    public class SharedRightsDBContext : DbContext
    {
        public SharedRightsDBContext() : base() { }

        public DbSet<SharedRights> SharedRights { get; set; }

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
            modelBuilder.Entity<SharedRights>()
                .HasKey(sr => new { sr.UserId, sr.ServerId });
            base.OnModelCreating(modelBuilder);
        }
    }
}
