using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.DTOs;
using PlantHomie.API.Models;
using PlantHomie.API.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PlantHomie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly PlantHomieContext _ctx;
    private readonly ILogger<UserController> _log;
    private readonly JwtService _jwtService;

    public UserController(PlantHomieContext ctx, ILogger<UserController> log, JwtService jwtService)
    {
        _ctx = ctx;
        _log = log;
        _jwtService = jwtService;
    }

    // OPRET BRUGER
    [HttpPost("signup")] // HTTP POST endpoint: api/user/signup
    public async Task<IActionResult> Signup(UserSignupDto dto)
    {
        // Tjekker asynkront om brugernavnet allerede eksisterer i databasen
        if (await _ctx.Users.AnyAsync(u => u.UserName == dto.UserName))
            return Conflict("Brugernavnet er allerede taget."); // Returnerer HTTP 409 Conflict hvis navnet er optaget

        var user = new User
        {
            UserName = dto.UserName,
            PasswordHash = Hash(dto.Password), // Hasher den modtagne adgangskode vha. SHA-256
            Subscription = dto.Subscription,
            // Sætter antal planter baseret på abonnementstype. Default er 10 for "Free".
            Plants_amount = dto.Subscription switch
            {
                "Premium_Silver" => 30,
                "Premium_Gold" => 50,
                "Premium_Plat" => 100,
                _ => 10 // Default for "Free" eller enhver anden/ukendt subscription string
            }
        };

        _ctx.Users.Add(user); // Tilføjer den nye bruger til DbContext (tracking)
        await _ctx.SaveChangesAsync(); // Persisterer ændringer (den nye bruger) til databasen

        return Ok(new { message = "Konto oprettet" }); // Returnerer HTTP 200 OK med en bekræftelsesbesked
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
        
        // Validerer at User ID claim findes og kan parses til et heltal
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
                     .Select(u => new // Projekterer til et anonymt objekt for at undgå at sende PasswordHash
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