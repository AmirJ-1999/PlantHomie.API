using Microsoft.AspNetCore.Mvc; // er til at lave API controller
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
        public async Task<IActionResult> PostUser([FromBody] User user)
        {
            if (user == null)
                return BadRequest("Data mangler.");

            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                return BadRequest("En bruger med denne email eksisterer allerede.");

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Bruger oprettet", user });
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
