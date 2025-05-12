using Microsoft.AspNetCore.Mvc; // er til at lave API controller
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore; // er til at lave API controller
using PlantHomie.API.Data;
using PlantHomie.API.Models;

namespace PlantHomie.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly PlantHomieContext _context;

        public UserController(PlantHomieContext context)
        {
            _context = context;
        }

        // GET: api/user
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .OrderBy(u => u.Name)
                .ToListAsync();

            if (!users.Any())
                return NotFound("Ingen brugere fundet.");

            return Ok(users);
        }

        // POST: api/user
        [HttpPost]
        public IActionResult PostUser([FromBody] User user)
        {
            if (user == null)
                return BadRequest("Data mangler.");

            try
            {
                _context.Users.Add(user);
                _context.SaveChanges();
                return Ok(new { message = "Bruger oprettet", user });
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
        }

        // GET: api/user/latest
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatest()
        {
            var latestUser = await _context.Users
                .OrderByDescending(u => u.User_ID)
                .FirstOrDefaultAsync();

            if (latestUser == null)
                return NotFound("Ingen brugere fundet.");

            return Ok(latestUser);
        }
    }
}
