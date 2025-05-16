using Microsoft.AspNetCore.Mvc; // er til at lave API controller
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore; // er til at lave API controller
using PlantHomie.API.Data;
using PlantHomie.API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using PlantHomie.API.Services;

namespace PlantHomie.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly PlantHomieContext _ctx;
        private readonly JwtService _jwtService;

        public UserController(PlantHomieContext context, JwtService jwtService)
        {
            _ctx = context;
            _jwtService = jwtService;
        }

        // REGISTRERING
        [HttpPost("register")] // HTTP POST endpoint: api/user/register
        public async Task<IActionResult> Register(UserRegisterDto dto)
        {
            // Valider at den modtagne krop indeholder et gyldigt UserRegisterDto-objekt
            if (dto is null || string.IsNullOrEmpty(dto.UserName) || string.IsNullOrEmpty(dto.Password))
                return BadRequest("Brugernavn og adgangskode skal udfyldes.");

            // Tjek om brugernavnet allerede findes i databasen
            if (await _ctx.Users.AnyAsync(u => u.UserName == dto.UserName))
                return Conflict("Dette brugernavn er allerede i brug.");

            // Opret ny User med data fra DTO'en
            var user = new User
            {
                UserName = dto.UserName,
                PasswordHash = Hash(dto.Password), // Hasher den modtagne adgangskode vha. SHA-256
                Subscription = dto.Subscription,
                // Sætter antal planter baseret på abonnementstype. Standard er 10 for "Free".
                Plants_amount = dto.Subscription switch
                {
                    "Premium_Silver" => 30,
                    "Premium_Gold" => 50,
                    "Premium_Plat" => 100,
                    _ => 10 // Standard for "Free" eller enhver anden/ukendt subscription string
                }
            };

            try
            {
                _ctx.Users.Add(user); // Tilføjer den nye bruger til DbContext (tracking)
                await _ctx.SaveChangesAsync(); // Gemmer ændringer (den nye bruger) til databasen
            }
            catch (DbUpdateException ex) // Håndterer databaseopdateringsfejl
            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627) // 2627 er fejlnummeret for unikke begrænsningsovertrædelser
                {
                    // Tjek constraint-navn for at give korrekt besked
                    if (sqlEx.Message.Contains("PK__User")) // Primær nøglebegrænsning (User_ID)
                        return BadRequest("En bruger med dette ID eksisterer allerede. Vælg et andet ID.");
                    if (sqlEx.Message.Contains("UQ__User__Email") || sqlEx.Message.Contains("UQ__User__") || sqlEx.Message.Contains("UQ_User_Email")) // Unik begrænsning på Email
                        return BadRequest("En bruger med denne email eksisterer allerede. Vælg en anden email.");
                }
                throw; // Hvis det ikke er en unik begrænsningsovertrædelse, håndteres det på en anden måde
            }

            // Genererer et JWT token til den nyoprettede bruger
            var token = _jwtService.GenerateToken(user);
            
            // Returnerer HTTP 201 Created med bruger-ID, token og abonnementstype
            return Created(string.Empty, new
            {
                userId = user.User_ID,
                token,
                subscription = user.Subscription
            });
        }

        // LOGIN
        [HttpPost("login")] // HTTP POST endpoint: api/user/login
        public async Task<IActionResult> Login(UserLoginDto dto)
        {
            // Forsøger at hente brugeren fra databasen baseret på brugernavn
            var user = await _ctx.Users
                                 .FirstOrDefaultAsync(u => u.UserName == dto.UserName);

            // Hvis brugeren ikke findes eller hashet password ikke matcher, returneres HTTP 401 Unauthorized
            if (user is null || user.PasswordHash != Hash(dto.Password))
                return Unauthorized("Ugyldige loginoplysninger.");

            // Bruger JwtService til at generere et JWT token for den autentificerede bruger
            var token = _jwtService.GenerateToken(user);
            
            // Returnerer HTTP 200 OK med token og brugerinformation
            return Ok(new { 
                token = token, 
                role = "user", // Simpel rolle-angivelse, kan udvides
                userId = user.User_ID,
                subscription = user.Subscription
            });
        }

        // HENT BRUGERPROFIL
        [Authorize] // Kræver gyldigt JWT token for adgang
        [HttpGet("profile")] // HTTP GET endpoint: api/user/profile
        public async Task<IActionResult> GetProfile()
        {
            // Henter User ID fra claims i det medsendte JWT token (NameIdentifier er standard claim type for ID)
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Validerer at User ID claim findes og kan fortolkes til et heltal
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int id))
                return Unauthorized("Ugyldig token eller manglende User ID claim"); // HTTP 401 hvis token er ugyldigt
                
            var user = await _ctx.Users.FindAsync(id); // Henter brugerdata asynkront baseret på ID
            if (user == null)
                return NotFound("Bruger ikke fundet"); // HTTP 404 hvis brugeren ikke findes i DB
                
            // Returnerer HTTP 200 OK med udvalgte brugeroplysninger (undgår at sende PasswordHash)
            return Ok(new {
                user.User_ID,
                user.UserName,
                user.Subscription,
                user.Plants_amount
            });
        }

        // LISTE (admin) - NB: Denne er ikke [Authorize] og bør sikres yderligere i en produktionsapp!
        [HttpGet] // HTTP GET endpoint: api/user
        public async Task<IActionResult> GetAll() =>
            Ok(await _ctx.Users
                         .Select(u => new // Projicerer til et anonymt objekt for at undgå at sende PasswordHash
                         {
                             u.User_ID,
                             u.UserName,
                             u.Subscription,
                             u.Plants_amount
                         })
                         .ToListAsync());

        // Simpel SHA-256 hash funktion til adgangskoder. 
        // Overvej stærkere hashing (fx Argon2, scrypt) og salt i en produktionsapplikation.
        private static string Hash(string text)
        {
            using var sha = SHA256.Create(); // Opretter en SHA256 hash-instans
            // Konverterer input-strengen til bytes, beregner hash, og konverterer hash-bytes til hex-string
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }
    }

    // Data Transfer Objects (DTOs) for brugerregistrering og login
    public class UserRegisterDto
    {
        public required string UserName { get; set; }
        public required string Password { get; set; }
        public string Subscription { get; set; } = "Free"; // Default abonnement
    }

    public class UserLoginDto
    {
        public required string UserName { get; set; }
        public required string Password { get; set; }
    }
}
