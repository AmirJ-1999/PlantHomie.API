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
    }
}
