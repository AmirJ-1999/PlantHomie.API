// DTOs/UserLoginDto.cs
namespace PlantHomie.API.DTOs;

public record UserLoginDto(
    string UserName,
    string Password
);