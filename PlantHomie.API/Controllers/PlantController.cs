using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Models;
using System.IO;

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
        [HttpGet]
        public IActionResult GetAll()
        {
            var plants = _context.Plants.OrderBy(p => p.Plant_Name).ToList();
            if (!plants.Any())
                return NotFound("Ingen planter fundet.");
            return Ok(plants);
        }

        // GET: api/plant/latest
        [HttpGet("latest")]
        public IActionResult GetLatest()
        {
            var latestPlant = _context.Plants
                .OrderByDescending(p => p.Plant_ID)
                .FirstOrDefault();

            if (latestPlant == null)
                return NotFound("Ingen planter fundet.");

            return Ok(latestPlant);
        }

        // POST: api/plant
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] string Plant_Name, [FromForm] string Plant_type, [FromForm] IFormFile? Image)
        {
            if (string.IsNullOrWhiteSpace(Plant_Name))
                return BadRequest("Plant_Name mangler.");

            string? imageUrl = null;

            // Hvis der medfølger billede
            if (Image != null && Image.Length > 0)
            {
                var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsPath); // sikrer mappen findes

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(Image.FileName);
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Image.CopyToAsync(stream);
                }

                imageUrl = $"/uploads/{uniqueFileName}";
            }

            var newPlant = new Plant
            {
                Plant_Name = Plant_Name,
                Plant_type = Plant_type,
                ImageUrl = imageUrl
            };

            try
            {
                _context.Plants.Add(newPlant);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Plante oprettet", plant = newPlant });
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
                {
                    if (sqlEx.Message.Contains("PK__Plant"))
                        return BadRequest("En plante med dette ID eksisterer allerede.");
                    if (sqlEx.Message.Contains("UQ_Plant_Name") || sqlEx.Message.Contains("UQ__Plant__"))
                        return BadRequest("En plante med dette navn eksisterer allerede.");
                }

                throw;
            }
        }

        // DELETE: api/plant/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var plant = _context.Plants.FirstOrDefault(p => p.Plant_ID == id);
            if (plant == null)
                return NotFound($"Ingen plante fundet med ID {id}");

            var plantLogs = _context.PlantLogs.Where(pl => pl.Plant_ID == id).ToList();
            _context.PlantLogs.RemoveRange(plantLogs);
            _context.Plants.Remove(plant);
            _context.SaveChanges();

            return Ok(new { message = $"Plante med ID {id} blev slettet" });
        }
    }
}