using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HomeSite.Entities
{
    public class ServerDBContext : DbContext
    {
        public ServerDBContext() : base()
        {

        }

        public DbSet<Server> Servers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = "Host=db;Port=5432;Database=hs_db;Username=postgres;Password=postgres";

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();

            optionsBuilder.UseNpgsql(dataSource);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Server>()
                .Property(b => b.PortMappings)
                .HasColumnType("jsonb");
            base.OnModelCreating(modelBuilder);
        }
    }
}
