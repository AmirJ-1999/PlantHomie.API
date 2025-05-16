using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Models;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace PlantHomie.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlantController : ControllerBase
    {
        private readonly PlantHomieContext _context;

        public PlantController(PlantHomieContext context)
        {
            _context = context;
        }

        // GET: api/plant
        [Authorize]
        [HttpGet]
        public IActionResult GetAll()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized("Ugyldig token eller manglende User ID claim.");
                
            return Ok(_context.Plants
                        .Where(p => p.User_ID == userId)
                        .OrderBy(p => p.Plant_Name)
                        .ToList());
        }

        // GET: api/plant/latest
        [Authorize]
        [HttpGet("latest")]
        public IActionResult GetLatest()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized("Ugyldig token eller manglende User ID claim.");
                
            var latest = _context.Plants
                               .Where(p => p.User_ID == userId)
                                 .OrderByDescending(p => p.Plant_ID)
                                 .FirstOrDefault();
            return latest is null ? NotFound() : Ok(latest);
        }

        // POST: api/plant  (multipart/form-data)
        [Authorize]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "Creates a plant with optional image")]
        [HttpPost]
        public IActionResult Create([FromBody] Plant plant)
        {
            if (plant == null)
                return BadRequest("Data mangler.");

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized("Ugyldig token eller manglende User ID claim.");
                
            if (data.User_ID != 0 && data.User_ID != userId)
                return BadRequest("Kan ikke oprette planter for andre brugere. User_ID fra token vil blive brugt.");

            string? imageUrl = null;
            if (data.Image is { Length: > 0 })
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
                return Unauthorized("Ugyldig token eller manglende User ID claim.");
                
            var plant = await _context.Plants.FindAsync(id);
            if (plant is null) return NotFound();

            if (plant.User_ID != userId)
                return Forbid("Du har ikke tilladelse til at slette denne plante. Planten tilhører en anden bruger.");

            if (!string.IsNullOrEmpty(plant.ImageUrl))
            {
                var path = Path.Combine(_env.WebRootPath ?? "wwwroot",
                                        plant.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

            // Fjern afhængige PlantLog-poster
            var plantLogs = _context.PlantLogs.Where(pl => pl.Plant_ID == id).ToList();
            _context.PlantLogs.RemoveRange(plantLogs);

            // Fjerner planten
            _context.Plants.Remove(plant);
            _context.SaveChanges();

            return Ok(new { message = $"Plante med ID {id} blev slettet" });
        }
    }
}
