using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Models;

namespace PlantHomie.API.Data
{
    public class PlantHomieContext : DbContext
    {
        public PlantHomieContext(DbContextOptions<PlantHomieContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Plant> Plants { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PlantLog> PlantLogs { get; set; }

        // Overskriver OnModelCreating-metoden for at konfigurere modelrelationer og tabeller i databasen.  
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);



            // Konfigurerer 'Plant'-entiteten til at matche 'Plant'-tabellen i databasen.

            modelBuilder.Entity<Plant>().ToTable("Plant");



            // Konfigurerer 'PlantLog'-entiteten til at matche 'PlantLog'-tabellen i databasen.

            modelBuilder.Entity<PlantLog>().ToTable("PlantLog");

        }
    }
}