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
using System.Text.Json;

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
        [HttpPost("signup")] // HTTP POST slutpunkt: api/user/signup
        public async Task<IActionResult> Signup(UserRegisterDto dto)
        {
            if (dto is null || string.IsNullOrEmpty(dto.UserName) || string.IsNullOrEmpty(dto.Password))
                return BadRequest("Username and password are required.");

            if (await _ctx.Users.AnyAsync(u => u.UserName == dto.UserName))
                return Conflict("This username is already in use.");

            var user = new User
            {
                UserName = dto.UserName,
                PasswordHash = Hash(dto.Password),
                Subscription = dto.Subscription,
                Plants_amount = dto.Subscription switch
                {
                    "Premium_Silver" => 30,
                    "Premium_Gold" => 50,
                    "Premium_Plat" => 100,
                    _ => 10
                }
            };

            try
            {
                _ctx.Users.Add(user);
                await _ctx.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
                {
                    if (sqlEx.Message.Contains("PK__User"))
                        return BadRequest("A user with this ID already exists. Please choose another ID.");
                    if (sqlEx.Message.Contains("UQ__User__Email") || sqlEx.Message.Contains("UQ__User__") || sqlEx.Message.Contains("UQ_User_Email"))
                        return BadRequest("A user with this email already exists. Please choose another email.");
                }
                throw;
            }

            var token = _jwtService.GenerateToken(user);

            return Created(string.Empty, new
            {
                userId = user.User_ID,
                token,
                subscription = user.Subscription
            });
        }

        // LOGIN
        [HttpPost("login")] // HTTP POST slutpunkt: api/user/login
        public async Task<IActionResult> Login(UserLoginDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("Login data is missing.");

                if (string.IsNullOrEmpty(dto.UserName) || string.IsNullOrEmpty(dto.Password))
                    return BadRequest("Username and password are required.");

                var user = await _ctx.Users
                                     .FirstOrDefaultAsync(u => u.UserName == dto.UserName);

                if (user is null)
                    return Unauthorized("Invalid username or password. Please check your credentials and try again.");

                if (user.PasswordHash != Hash(dto.Password))
                    return Unauthorized("Invalid username or password. Please check your credentials and try again.");

                var token = _jwtService.GenerateToken(user);

                return Ok(new
                {
                    token = token,
                    role = "user",
                    userId = user.User_ID,
                    subscription = user.Subscription
                });
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
                {
                    if (sqlEx.Message.Contains("PK__User"))
                        return BadRequest("A user with this ID already exists. Please choose another ID.");
                    if (sqlEx.Message.Contains("UQ__User__Email") || sqlEx.Message.Contains("UQ__User__") || sqlEx.Message.Contains("UQ_User_Email"))
                        return BadRequest("A user with this email already exists. Please choose another email.");
                }
                throw;
            }
            catch (Exception)
            {
                // Log undtagelsen hvis logning er konfigureret
                return StatusCode(500, "An error occurred during login. Please try again later.");
            }
        }

        // HENT BRUGERPROFIL
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int id))
                return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

            var user = await _ctx.Users.FindAsync(id);
            if (user == null)
                return NotFound("User not found.");

            return Ok(new
            {
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

        // OPDATER SUBSCRIPTION - PUT: api/user/{id}
        [Authorize]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSubscription(int id, [FromBody] UserUpdateDto dto)
        {
            try
            {
                // Tjek om brugeren er autoriseret til at opdatere denne bruger
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int currentUserId))
                    return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

                // Kun tillad brugere at opdatere deres egen profil
                if (currentUserId != id)
                    return Forbid("You can only update your own subscription.");

                var user = await _ctx.Users.FindAsync(id);
                if (user == null)
                    return NotFound("User not found.");

                // Opdater abonnement
                if (!string.IsNullOrEmpty(dto.Subscription))
                {
                    // Valider at abonnementstypen er gyldig
                    if (dto.Subscription != "Free" && 
                        dto.Subscription != "Premium_Silver" && 
                        dto.Subscription != "Premium_Gold" && 
                        dto.Subscription != "Premium_Plat")
                    {
                        return BadRequest("Invalid subscription type specified.");
                    }

                    user.Subscription = dto.Subscription;
                    user.Plants_amount = dto.Subscription switch
                    {
                        "Premium_Silver" => 30,
                        "Premium_Gold" => 50,
                        "Premium_Plat" => 100,
                        _ => 10
                    };
                }

                await _ctx.SaveChangesAsync();

                return Ok(new { 
                    message = "User subscription updated successfully", 
                    user_ID = user.User_ID,
                    subscription = user.Subscription,
                    plants_amount = user.Plants_amount
                });
            }
            catch (Exception)
            {
                // Log undtagelsen hvis logning er konfigureret
                return StatusCode(500, "An error occurred while updating the user subscription.");
            }
        }

        // HENT BRUGER VED ID
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var user = await _ctx.Users.FindAsync(id);
                if (user == null)
                    return NotFound("User not found.");

                return Ok(new
                {
                    user.User_ID,
                    user.UserName,
                    user.Subscription,
                    user.Plants_amount
                });
            }
            catch (Exception)
            {
                // Log undtagelsen hvis logning er konfigureret
                return StatusCode(500, "An error occurred while retrieving the user.");
            }
        }

        private static string Hash(string text)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }
    }

    public class UserRegisterDto
    {
        public required string UserName { get; set; }
        public required string Password { get; set; }
        public string Subscription { get; set; } = "Free";
    }

    public class UserLoginDto
    {
        public required string UserName { get; set; }
        public required string Password { get; set; }
    }

    public class UserUpdateDto
    {
        public string? Subscription { get; set; }
    }
}