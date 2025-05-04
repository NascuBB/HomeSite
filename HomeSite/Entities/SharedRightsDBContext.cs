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
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=just1x;Username=postgres;Password=postgres");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SharedRights>()
                .HasKey(sr => new { sr.userid, sr.serverid });
            base.OnModelCreating(modelBuilder);
        }
    }
}
