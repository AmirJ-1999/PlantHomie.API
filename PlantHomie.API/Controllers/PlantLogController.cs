using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Models;
using System.Linq;

namespace PlantHomie.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlantLogController : ControllerBase
    {
        private readonly PlantHomieContext _context;

        public PlantLogController(PlantHomieContext context)
        {
            _context = context;
        }

        // GET: api/plantlog
        [HttpGet]
        public IActionResult Index()
        {
            var logs = _context.PlantLogs
                .Include(p => p.Plant)
                .OrderByDescending(p => p.Dato_Tid)
                .ToList();

            if (!logs.Any())
                return NotFound("Ingen data fundet.");

            return Ok(logs);
        }

        // POST: api/plantlog
        [HttpPost]
        public IActionResult PostLog([FromBody] PlantLog log)
        {
            if (log == null)
                return BadRequest("Data mangler.");

            if (!_context.Plants.Any(p => p.Plant_ID == log.Plant_ID))
                return BadRequest("Plant_ID does not exist in the database.");

            log.Dato_Tid = DateTime.UtcNow;

            _context.PlantLogs.Add(log);
            _context.SaveChanges();

            var savedLog = _context.PlantLogs
                .Include(p => p.Plant)
                .FirstOrDefault(p => p.PlantLog_ID == log.PlantLog_ID);

            return Ok(new { message = "Log gemt", log = savedLog });
        }

        // GET: api/plantlog/latest?plantId=1
        [HttpGet("latest")]
        public IActionResult GetLatest(int plantId)
        {
            var latest = _context.PlantLogs
                .Where(p => p.Plant_ID == plantId)
                .OrderByDescending(p => p.Dato_Tid)
                .FirstOrDefault();

            if (latest == null)
                return NotFound("Ingen data fundet for den plante.");

            return Ok(latest);
        }

        // Dette er GET Metoden for at hente den nyeste temperaturmåling fra databasen.
        // Du siger hvilken plante med "PlantID"
        [HttpGet("temperature/{plantId}")]
        public IActionResult GetTemperature(int plantId)
        {
            var data = _context.PlantLogs
                .Where(p => p.Plant_ID == plantId)
                .OrderByDescending(p => p.Dato_Tid)
                .FirstOrDefault();

            // Hvis der ikke er en måling, så returner en fejlbesked
            // hvor der står "ingen temperaturdata fundet"
            return data != null
                ? Ok(data.Temperaturelevel)
                : NotFound("Ingen temperaturdata fundet");
        }


        // Dette er GET Metoden for at hente den nyeste luftfugtighedsmåling fra databasen.
        // Du siger hvilken plante med "PlantID"
        [HttpGet("airhumidity/{plantId}")]
        public IActionResult GetAirHumidity(int plantId)
        {
            var data = _context.PlantLogs
                .Where(p => p.Plant_ID == plantId)
                .OrderByDescending(p => p.Dato_Tid)
                .FirstOrDefault();
            // Hvis der ikke er en måling, så returner en fejlbesked
            // hvor der står "ingen luftfugtighedsdata fundet"
            return data != null
                ? Ok(data.AirHumidityLevel)
                : NotFound("Ingen luftfugtighedsdata fundet");
        }

        // Dette er GET Metoden for at hente den nyeste jordfugtighedsmåling fra databasen. 
        // Du siger hvilken plante med "PlantID"
        [HttpGet("soilmoisture/{plantId}")]
        public IActionResult GetSoilMoisture(int plantId)
        {
            var data = _context.PlantLogs
                .Where(p => p.Plant_ID == plantId)
                .OrderByDescending(p => p.Dato_Tid)
                .FirstOrDefault();
            // Hvis der ikke er en måling, så returner en fejlbesked
            // hvor der står "ingen jordfugtighedsdata fundet"
            return data != null
                ? Ok(data.WaterLevel)
                : NotFound("Ingen jordfugtighedsdata fundet");
        }
    }
}