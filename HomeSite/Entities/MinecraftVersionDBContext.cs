using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeSite.Entities
{
    public class MinecraftVersionDBContext : DbContext
    {
        public MinecraftVersionDBContext() : base() { }

        public DbSet<MinecraftVersion> Versions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=db;Port=5432;Database=hs_db;Username=postgres;Password=postgres");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        { }
    }
}

