using System.ComponentModel.DataAnnotations;

namespace PlantHomie.API.Models
{
    public class Plant
    {
        [Key]
        public int Plant_ID { get; set; }

        [StringLength(50)]
        public string? Plant_Name { get; set; }

        [StringLength(50)]
        public string? Plant_type { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; } // <- Tilføjet manuelt
    }
}