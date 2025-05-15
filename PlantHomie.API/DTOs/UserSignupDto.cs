// DTOs/UserSignupDto.cs
namespace PlantHomie.API.DTOs;
public record UserSignupDto(
    string UserName,
    string Password,
    string Subscription   // Free | Premium_…
);