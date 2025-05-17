using Microsoft.IdentityModel.Tokens;
using PlantHomie.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PlantHomie.API.Services
{
    public class JwtService
    {
        private readonly IConfiguration _config; // IConfiguration til at læse appsettings.json

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        // Metode til at generere et JWT token baseret på brugerinformation
        public string GenerateToken(User user)
        {
            var securityKeyString = _config["Jwt:Key"] ?? "PlantHomieDefaultSecretKey12345678";
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKeyString));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.User_ID.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("subscription", user.Subscription)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddYears(20),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}