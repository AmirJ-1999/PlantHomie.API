using System.ComponentModel.DataAnnotations;

namespace PlantHomie.API.Models
{
    public class User
    {
        [Key]
        public int User_ID { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Email { get; set; } = string.Empty;

        public string? Information { get; set; } // -- der er ikke NOT NULL her

        [StringLength(20)]
        public string Subscription { get; set; } = "Free";

        public int? Plants_amount { get; set; }
        
        // Navigeringsegenskaber
        public ICollection<Plant> Plants { get; set; } = new List<Plant>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
