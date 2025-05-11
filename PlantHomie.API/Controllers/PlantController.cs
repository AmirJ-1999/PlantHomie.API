using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Models;
using System.Linq;

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
        [HttpGet]
        public IActionResult GetAll()
        {
            var plants = _context.Plants
                .OrderBy(p => p.Plant_Name)
                .ToList();

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
        public IActionResult Create([FromBody] Plant plant)
        {
            if (plant == null)
                return BadRequest("Data mangler.");

            _context.Plants.Add(plant);
            _context.SaveChanges();

            return Ok(new { message = "Plante oprettet", plant });
        }

        // DELETE: api/plant/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var plant = _context.Plants.FirstOrDefault(p => p.Plant_ID == id);

            if (plant == null)
                return NotFound($"Ingen plante fundet med ID {id}");

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
