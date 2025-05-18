using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Models;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;
using System.IO;
using System.Text.Json;

namespace PlantHomie.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlantController : ControllerBase
    {
        private readonly PlantHomieContext _context;
        private readonly IWebHostEnvironment _env;

        public PlantController(PlantHomieContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: api/plant
        [Authorize]
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                    return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

                var plants = _context.Plants
                            .Where(p => p.User_ID == userId)
                            .OrderBy(p => p.Plant_Name)
                            .ToList();

                // Returner altid listen, også selvom den er tom
                return Ok(plants);
            }
            catch (Exception)
            {
                // Log undtagelsen hvis logning er konfigureret
                return StatusCode(500, "An error occurred while retrieving plants.");
            }
        }

        // GET: api/plant/latest
        [Authorize]
        [HttpGet("latest")]
        public IActionResult GetLatest()
        {
            try
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                    return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

                var latest = _context.Plants
                                   .Where(p => p.User_ID == userId)
                                   .OrderByDescending(p => p.Plant_ID)
                                   .FirstOrDefault();

                if (latest is null)
                {
                    // Returner standard planteobjekt når ingen planter eksisterer endnu
                    return Ok(new { 
                        plant_ID = 0,
                        plant_Name = "No plants yet",
                        plant_type = "Default",
                        user_ID = userId
                    });
                }

                return Ok(latest);
            }
            catch (Exception)
            {
                // Log undtagelsen hvis logning er konfigureret
                return StatusCode(500, "An error occurred while retrieving the latest plant.");
            }
        }

        // POST: api/plant  (multipart/form-data)
        [Authorize]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "Creates a plant with optional image")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] PlantCreateRequest data)
        {
            if (data == null)
                return BadRequest("Data is missing.");

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

            if (data.User_ID != 0 && data.User_ID != userId)
                return BadRequest("Cannot create plants for other users. User_ID from token will be used.");

            string? imageUrl = null;
            if (data.Image is { Length: > 0 })
            {
                var uploadDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(data.Image.FileName)}";
                var filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await data.Image.CopyToAsync(stream);
                }

                imageUrl = $"/uploads/{fileName}";
            }

            var plant = new Plant
            {
                Plant_Name = data.Plant_Name.Trim(),
                Plant_type = data.Plant_type.Trim(),
                ImageUrl = imageUrl,
                User_ID = userId
            };

            _context.Plants.Add(plant);
            await _context.SaveChangesAsync();

            return Ok(plant);
        }

        // DELETE: api/plant/{id}
        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

            var plant = await _context.Plants.FindAsync(id);
            if (plant is null) return NotFound();

            if (plant.User_ID != userId)
                return Forbid("You do not have permission to delete this plant. The plant belongs to another user.");

            if (!string.IsNullOrEmpty(plant.ImageUrl))
            {
                var path = Path.Combine(_env.WebRootPath ?? "wwwroot", plant.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

            // Fjern afhængige PlantLog-poster
            var plantLogs = _context.PlantLogs.Where(pl => pl.Plant_ID == id).ToList();
            _context.PlantLogs.RemoveRange(plantLogs);

            // Fjerner planten
            _context.Plants.Remove(plant);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Plant with ID {id} was deleted" });
        }
    }
}