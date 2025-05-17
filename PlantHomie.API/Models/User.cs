using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [StringLength(20)]
        public string Subscription { get; set; } = "Free";

        public int? Plants_amount { get; set; }

        // Hvis AutoMode er slået til så kan user ikke forstyrres, kun alvorlige notifikationer bliver sendt
        [NotMapped]
        public bool AutoMode { get; set; } = true;

        // Navigeringsegenskaber
        public ICollection<Plant> Plants { get; set; } = new List<Plant>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}