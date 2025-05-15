// Models/User.cs
using System.ComponentModel.DataAnnotations;

namespace PlantHomie.API.Models;

public class User
{
    [Key] public int User_ID { get; set; }

    [Required, StringLength(50)]
    public string UserName { get; set; } = default!;

    [Required, StringLength(200)]
    public string PasswordHash { get; set; } = default!;

    [Required, StringLength(20)]
    public string Subscription { get; set; } = "Free";

    /* Valgfire felt­er
    [StringLength(50)] public string? Name { get; set; }
    [StringLength(50)] public string? Email { get; set; }
    */
    public int Plants_amount { get; set; }
}