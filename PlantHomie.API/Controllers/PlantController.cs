using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Models;
using Swashbuckle.AspNetCore.Annotations;

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

        // ------------------------------------------------------
        // GET: api/plant
        // ------------------------------------------------------
        [HttpGet]
        public IActionResult GetAll()
            => Ok(_context.Plants.OrderBy(p => p.Plant_Name).ToList());

        // ------------------------------------------------------
        // GET: api/plant/latest
        // ------------------------------------------------------
        [HttpGet("latest")]
        public IActionResult GetLatest()
        {
            var latest = _context.Plants
                                 .OrderByDescending(p => p.Plant_ID)
                                 .FirstOrDefault();
            return latest is null ? NotFound() : Ok(latest);
        }

        // ------------------------------------------------------
        // POST: api/plant  (multipart/form-data)
        // ------------------------------------------------------
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "Opretter en plante med valgfrit billede")]
        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Create([FromForm] PlantCreateRequest data)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Gem billede hvis et er vedhæftet
            string? imageUrl = null;
            if (data.Image is { Length: > 0 })
            {
                var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(data.Image.FileName)}";
                var path = Path.Combine(uploadsDir, fileName);

                await using var stream = System.IO.File.Create(path);
                await data.Image.CopyToAsync(stream);

                imageUrl = $"/uploads/{fileName}";
            }

            var plant = new Plant
            {
                Plant_Name = data.Plant_Name.Trim(),
                Plant_type = data.Plant_type.Trim(),
                ImageUrl = imageUrl
            };

            _context.Plants.Add(plant);
            await _context.SaveChangesAsync();

            return Ok(plant);
        }

        // ------------------------------------------------------
        // DELETE: api/plant/{id}
        // ------------------------------------------------------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var plant = await _context.Plants.FindAsync(id);
            if (plant is null) return NotFound();

            // Slet billedefil fra disk hvis den findes
            if (!string.IsNullOrEmpty(plant.ImageUrl))
            {
                var path = Path.Combine(_env.WebRootPath ?? "wwwroot",
                                        plant.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

            _context.Plants.Remove(plant);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
