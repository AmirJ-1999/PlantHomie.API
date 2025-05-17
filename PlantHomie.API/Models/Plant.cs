using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlantHomie.API.Models
{
    public class Plant
    {
        [Key]
        public int Plant_ID { get; set; }

        [Required]
        [StringLength(50)]
        public string Plant_Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string Plant_type { get; set; } = string.Empty;

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        // Påkrævet for frontend kode
        public int User_ID { get; set; }

        [ForeignKey("User_ID")]
        public User? User { get; set; }

        // Navigeringsegenskab for PlantLogs
        public ICollection<PlantLog> PlantLogs { get; set; } = new List<PlantLog>();

        // Navigeringsegenskab for Notifications
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
