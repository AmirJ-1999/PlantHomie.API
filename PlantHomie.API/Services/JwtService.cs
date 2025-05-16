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
            // Henter JWT nøglen fra konfigurationen (_config["Jwt:Key"]) og opretter en SymmetricSecurityKey.
            // SymmetricSecurityKey bruges til både at signere og validere tokens med den samme nøgle.
            var securityKeyString = _config["Jwt:Key"] ?? "PlantHomieDefaultSecretKey12345678"; // Fallback key
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKeyString));
            
            // Opretter SigningCredentials med HMAC SHA256 algoritmen.
            // HMACSHA256 er en udbredt og sikker algoritme til at signere JWT tokens.
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Definerer de claims (informationer), der skal inkluderes i tokenet.
            // Claims er typisk bruger-specifik information, der kan bruges til autorisation.
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.User_ID.ToString()), // Standardiseret claim type for brugerens unikke ID.
                new Claim(ClaimTypes.Name, user.UserName), // Standardiseret claim type for brugernavnet.
                new Claim("subscription", user.Subscription) // Custom claim for brugerens abonnementstype.
            };

            // Opretter selve JwtSecurityToken objektet med de nødvendige parametre.
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"], // Udstederen af tokenet (typisk API'ets domæne), læses fra config.
                audience: _config["Jwt:Audience"], // Modtageren/målgruppen for tokenet (typisk frontend app), læses fra config.
                claims: claims, // De definerede claims der skal indlejres i tokenet.
                expires: DateTime.UtcNow.AddDays(7), // Tokenets udløbstidspunkt. Sættes til UTC for at undgå tidszone-problemer.
                signingCredentials: credentials // De credentials (nøgle + algoritme) der bruges til at signere tokenet.
            );

            // Serialiserer JwtSecurityToken objektet til dets kompakte string-repræsentation (header.payload.signature).
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
} 