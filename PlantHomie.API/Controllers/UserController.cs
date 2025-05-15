using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.DTOs;
using PlantHomie.API.Models;
using System.Security.Cryptography;
using System.Text;

namespace PlantHomie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly PlantHomieContext _ctx;
    private readonly ILogger<UserController> _log;

    public UserController(PlantHomieContext ctx, ILogger<UserController> log)
    {
        _ctx = ctx;
        _log = log;
    }

    /* ---------- SIGN-UP ---------- */
    [HttpPost("signup")]
    public async Task<IActionResult> Signup(UserSignupDto dto)
    {
        if (await _ctx.Users.AnyAsync(u => u.UserName == dto.UserName))
            return Conflict("Username already taken.");

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

        return Ok(new { message = "Account created" });
    }

    /* ---------- LOGIN ---------- */
    [HttpPost("login")]
    public async Task<IActionResult> Login(UserLoginDto dto)
    {
        var user = await _ctx.Users
                             .FirstOrDefaultAsync(u => u.UserName == dto.UserName);

        if (user is null || user.PasswordHash != Hash(dto.Password))
            return Unauthorized("Invalid credentials.");

        // Returnér evt. rigtigt JWT-token – her blot dummy-token
        return Ok(new { token = "mock-token", role = "user" });
    }

    /* ---------- LIST (admin ---------- */
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

    // koden til at hash'e passwords
    private static string Hash(string text)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
    }
}