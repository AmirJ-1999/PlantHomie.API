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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map 'User' entiteten til '[User]' tabellen i databasen
            modelBuilder.Entity<User>().ToTable("User");

            // Map andre entiteter til deres respektive tabeller
            modelBuilder.Entity<Plant>().ToTable("Plant");
            modelBuilder.Entity<PlantLog>().ToTable("PlantLog");
            modelBuilder.Entity<Notification>().ToTable("Notification");
            
            // Configure cascade delete behavior for PlantLog
            modelBuilder.Entity<PlantLog>()
                .HasOne(p => p.Plant)
                .WithMany(p => p.PlantLogs)
                .HasForeignKey(p => p.Plant_ID)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure cascade delete behavior for Notification
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Plant)
                .WithMany(p => p.Notifications)
                .HasForeignKey(n => n.Plant_ID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}