using Microsoft.IdentityModel.Tokens;
using PlantHomie.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PlantHomie.API.Services
{
    public class JwtService
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public JwtService(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
        }

        // Metode til at generere et JWT token baseret på brugerinformation
        public string GenerateToken(User user)
        {
            var securityKeyString = _config["Jwt:Key"] ?? "PlantHomieDefaultSecretKey12345678";
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKeyString));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.User_ID.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("subscription", user.Subscription)
            };

            // Set issuer and audience based on environment
            var issuer = _env.IsDevelopment() 
                ? "http://localhost:5000" 
                : (_config["Jwt:Issuer"] ?? "PlantHomieAPI");
                
            var audience = _env.IsDevelopment() 
                ? "http://localhost:8080" 
                : (_config["Jwt:Audience"] ?? "PlantHomieWebApp");

            Console.WriteLine($"Generating token with issuer: {issuer}, audience: {audience}");

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),  // 30 day expiration
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}