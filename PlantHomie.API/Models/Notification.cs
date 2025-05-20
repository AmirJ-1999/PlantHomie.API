using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlantHomie.API.Models
{
    public class Notification
    {
        [Key]
        public int Notification_ID { get; set; }

        [Required]
        public int Plant_ID { get; set; }

        [Required]
        public int User_ID { get; set; }

        public DateTime Dato_Tid { get; set; } = DateTime.UtcNow;

        [StringLength(20)]
        public string? Plant_Type { get; set; }

        [StringLength(20)]
        public string? Type { get; set; } = "System";

        [StringLength(250)]
        public string? Message { get; set; }
        
        public bool IsRead { get; set; } = false;
        
        public string? NotificationType { get; set; } = "System";

        // Navigation property der repræsenterer den tilknyttede plante.
        // "?" betyder at denne relation er valgfri (kan være null, fx hvis planten ikke er hentet med fra databasen).
        // ForeignKey-attributten sikrer, at EF Core bruger Plant_ID som forbindelsen til Plant-tabellen.
        [ForeignKey("Plant_ID")]
        public Plant? Plant { get; set; }

        // Navigation property der repræsenterer den tilknyttede bruger.
        // ForeignKey-attributten sikrer, at EF Core bruger User_ID som forbindelsen til User-tabellen.
        [ForeignKey("User_ID")]
        public User? User { get; set; }
    }
}
