using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

            try
            {
                _context.Plants.Add(plant);
                _context.SaveChanges();
                return Ok(new { message = "Plante oprettet", plant });
            }
            catch (DbUpdateException ex) // Håndterer databaseopdateringsfejl

            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627) // 2627 er fejlnummeret for unikke begrænsningsovertrædelser
                {
                    // Tjek hvilken unik begrænsning der blev overtrådt
                    if (sqlEx.Message.Contains("PK__Plant")) // Primær nøglebegrænsning (Plant_ID)
                        return BadRequest("En plante med dette ID eksisterer allerede. Vælg et andet ID.");
                    if (sqlEx.Message.Contains("UQ_Plant_Name") || sqlEx.Message.Contains("UQ__Plant__")) // Unik begrænsning på Plant_Name
                        return BadRequest("En plante med dette navn eksisterer allerede. Vælg et andet navn.");
                }
                throw; // Hvis det ikke er en unik begrænsningsovertrædelse, håndteres det på en anden måde
            }
        }

        // DELETE: api/plant/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var plant = _context.Plants.FirstOrDefault(p => p.Plant_ID == id); // Find planten med det angivne ID

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
