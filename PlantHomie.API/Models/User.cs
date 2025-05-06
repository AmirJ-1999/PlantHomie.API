using System.ComponentModel.DataAnnotations;

namespace PlantHomie.API.Models
{
    public class User
    {
        [Key]
        public int User_ID { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Email { get; set; }

        public string? Information { get; set; } // -- der er ikke NOT NULL her

        public int Plants_amount { get; set; }
    }
}
