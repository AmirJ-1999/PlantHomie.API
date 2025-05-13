using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlantHomie.API.Models
{
    public class Plant
    {
        // >>> Her er de to attributter <<<
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Plant_ID { get; set; }

        [StringLength(50)]
        public string? Plant_Name { get; set; }

        [StringLength(50)]
        public string? Plant_type { get; set; }

        public string? ImageUrl { get; set; }
    }
}