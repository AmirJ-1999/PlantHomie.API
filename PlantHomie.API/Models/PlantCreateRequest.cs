// Models/PlantCreateRequest.cs
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace PlantHomie.API.Models;

/// <summary>
/// Model til multipart/form-data upload af en plante
/// </summary>
public class PlantCreateRequest
{
    [Required] public string Plant_Name { get; set; } = null!;
    [Required] public string Plant_type { get; set; } = null!;
    public IFormFile? Image { get; set; }
    public int User_ID { get; set; }
}
