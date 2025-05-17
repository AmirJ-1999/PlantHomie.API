using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Models;
using System.Linq;
using System.Security.Claims;

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
        [Authorize]
        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                    return Unauthorized("Invalid token or missing User ID claim.");

                var logs = _context.PlantLogs
                                 .Include(p => p.Plant)
                                 .Where(p => p.Plant != null && p.Plant.User_ID == userId)
                                 .OrderByDescending(p => p.Dato_Tid)
                                 .ToList();

                // Always return the list, even if empty
                return Ok(logs);
            }
            catch (Exception ex)
            {
                // Log the exception if logging is configured
                return StatusCode(500, "An error occurred while retrieving plant logs.");
            }
        }

        // POST: api/plantlog
        [Authorize]
        [HttpPost]
        public IActionResult PostLog([FromBody] PlantLog log)
        {
            if (log == null)
                return BadRequest("Missing data.");

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized("Invalid token or missing User ID claim.");

            var plant = _context.Plants.FirstOrDefault(p => p.Plant_ID == log.Plant_ID);
            if (plant == null)
                return BadRequest("Plant ID not found in database.");

            if (plant.User_ID != userId)
                return Forbid("You do not have permission to log data for this plant. It belongs to another user.");

            log.Dato_Tid = DateTime.UtcNow;

            _context.PlantLogs.Add(log);
            _context.SaveChanges();

            var saved = _context.PlantLogs
                                .Include(p => p.Plant)
                                .First(p => p.PlantLog_ID == log.PlantLog_ID);

            return Ok(new { message = "Log saved", log = saved });
        }

        // GET: api/plantlog/latest?plantId=1
        [Authorize]
        [HttpGet("latest")]
        public IActionResult GetLatest(int plantId)
        {
            try
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                    return Unauthorized("Invalid token or missing User ID claim.");

                // For new users with no plants yet, provide default data for initial display
                if (plantId == 1 && !_context.Plants.Any(p => p.Plant_ID == plantId))
                {
                    return Ok(new
                    {
                        plantLog_ID = 0,
                        plant_ID = 1,
                        dato_Tid = DateTime.UtcNow,
                        temperatureLevel = 22.5,
                        lightLevel = 500.0,
                        waterLevel = 50.0,
                        airHumidityLevel = 50.0
                    });
                }

                var plant = _context.Plants.FirstOrDefault(p => p.Plant_ID == plantId);
                if (plant == null)
                {
                    return Ok(new
                    {
                        plantLog_ID = 0,
                        plant_ID = plantId,
                        dato_Tid = DateTime.UtcNow,
                        temperatureLevel = 22.5,
                        lightLevel = 500.0,
                        waterLevel = 50.0,
                        airHumidityLevel = 50.0
                    });
                }

                if (plant.User_ID != userId)
                    return Forbid("You do not have permission to access this plant. It belongs to another user.");

                var latest = _context.PlantLogs
                                   .Where(p => p.Plant_ID == plantId)
                                   .OrderByDescending(p => p.Dato_Tid)
                                   .FirstOrDefault();

                if (latest is null)
                {
                    // Create a default log if none exists
                    return Ok(new
                    {
                        plantLog_ID = 0,
                        plant_ID = plantId,
                        dato_Tid = DateTime.UtcNow,
                        temperatureLevel = 22.5,
                        lightLevel = 500.0,
                        waterLevel = 50.0,
                        airHumidityLevel = 50.0
                    });
                }

                return Ok(latest);
            }
            catch (Exception ex)
            {
                // Log the exception if logging is configured
                return StatusCode(500, "An error occurred while retrieving the latest plant log.");
            }
        }

        // GET: api/plantlog/temperature/1
        [Authorize]
        [HttpGet("temperature/{plantId}")]
        public IActionResult GetTemperature(int plantId) =>
            GetSingleValue(plantId, log => log.TemperatureLevel, "temperature");

        // GET: api/plantlog/airhumidity/1
        [Authorize]
        [HttpGet("airhumidity/{plantId}")]
        public IActionResult GetAirHumidity(int plantId) =>
            GetSingleValue(plantId, log => log.AirHumidityLevel, "humidity");

        // GET: api/plantlog/soilmoisture/1
        [Authorize]
        [HttpGet("soilmoisture/{plantId}")]
        public IActionResult GetSoilMoisture(int plantId) =>
            GetSingleValue(plantId, log => log.WaterLevel, "moisture");

        private IActionResult GetSingleValue(
            int plantId,
            Func<PlantLog, double?> selector,
            string label)
        {
            try
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                    return Unauthorized("Invalid token or missing User ID claim.");

                // Check if this is the default plant ID 1 for a new user
                // If no plants exist yet, return a default value
                if (plantId == 1 && !_context.Plants.Any(p => p.Plant_ID == plantId))
                {
                    return Ok(50.0); // Return a default middle value (50%)
                }

                var plant = _context.Plants.FirstOrDefault(p => p.Plant_ID == plantId);
                if (plant == null)
                    return NotFound("Plant not found");

                if (plant.User_ID != userId)
                    return Forbid("You do not have permission to access this plant. It belongs to another user.");

                var value = _context.PlantLogs
                                  .Where(p => p.Plant_ID == plantId)
                                  .OrderByDescending(p => p.Dato_Tid)
                                  .Select(selector)
                                  .FirstOrDefault();

                return value.HasValue
                    ? Ok(value.Value)
                    : Ok(50.0); // Return a default value if no logs exist
            }
            catch (Exception ex)
            {
                // Log the exception if logging is configured
                return StatusCode(500, $"An error occurred while retrieving {label} data.");
            }
        }
    }
}