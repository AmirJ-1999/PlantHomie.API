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
    [HttpPost("signup")]
    public async Task<IActionResult> Signup(UserSignupDto dto)
    {
        if (await _ctx.Users.AnyAsync(u => u.UserName == dto.UserName))
            return Conflict("Brugernavnet er allerede taget.");

        var user = new User
        {
            UserName = dto.UserName,
            PasswordHash = Hash(dto.Password),          // simpelt SHA-256-hash
            Subscription = dto.Subscription,
            Plants_amount = dto.Subscription switch
            {
                "Premium_Silver" => 30,
                "Premium_Gold" => 50,
                "Premium_Plat" => 100,
                _ => 10                    // Free
            }
        };

        _ctx.Users.Add(user);
        await _ctx.SaveChangesAsync();

        return Ok(new { message = "Konto oprettet" });
    }

    // LOGIN
    [HttpPost("login")]
    public async Task<IActionResult> Login(UserLoginDto dto)
    {
        var user = await _ctx.Users
                             .FirstOrDefaultAsync(u => u.UserName == dto.UserName);

        if (user is null || user.PasswordHash != Hash(dto.Password))
            return Unauthorized("Ugyldige loginoplysninger.");

        // Generer JWT token
        var token = _jwtService.GenerateToken(user);
        
        return Ok(new { 
            token = token, 
            role = "user",
            userId = user.User_ID,
            subscription = user.Subscription
        });
    }

    // HENT BRUGERPROFIL
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        // Hent bruger-ID fra token-claims
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
            return Unauthorized("Ugyldig token");
            
        var user = await _ctx.Users.FindAsync(id);
        if (user == null)
            return NotFound("Bruger ikke fundet");
            
        return Ok(new {
            user.User_ID,
            user.UserName,
            user.Subscription,
            user.Plants_amount
        });
    }

    // LISTE (admin)
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _ctx.Users
                     .Select(u => new
                     {
                         u.User_ID,
                         u.UserName,
                         u.Subscription,
                         u.Plants_amount
                     })
                     .ToListAsync());

    // Kode til at hash'e adgangskoder
    private static string Hash(string text)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
    }
}