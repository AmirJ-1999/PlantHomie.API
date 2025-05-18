using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Models;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;

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
        public IActionResult Index([FromQuery] int? limit = null)
        {
            try
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                    return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

                // Check if user has plants
                var userHasPlants = _context.Plants.Any(p => p.User_ID == userId);
                
                // If user doesn't have any plants yet, return sample data so the UI has something to display
                if (!userHasPlants)
                {
                    var currentDate = DateTime.UtcNow;
                    var sampleLogs = new List<object>();
                    
                    // Create sample entries for the last 24 hours (one per hour)
                    for (int i = 0; i < (limit.HasValue ? Math.Min(limit.Value, 24) : 24); i++)
                    {
                        var sampleTime = currentDate.AddHours(-i);
                        sampleLogs.Add(new
                        {
                            plantLog_ID = -i,
                            plant_ID = 1,
                            dato_Tid = sampleTime,
                            temperatureLevel = 22.5 + Math.Sin(i * 0.1) * 5,
                            lightLevel = 500.0,
                            waterLevel = 50.0 + Math.Cos(i * 0.1) * 20,
                            airHumidityLevel = 45.0 + Math.Sin(i * 0.2) * 15,
                            plant = new
                            {
                                plant_ID = 1,
                                plant_Name = "My First Plant",
                                plant_type = "Default Plant",
                                user_ID = userId
                            }
                        });
                    }
                    
                    return Ok(sampleLogs);
                }

                // User has plants, proceed with real data
                IQueryable<PlantLog> query = _context.PlantLogs
                                 .Include(p => p.Plant)
                                 .Where(p => p.Plant != null && p.Plant.User_ID == userId)
                            .OrderByDescending(p => p.Dato_Tid);
                
                if (limit.HasValue)
                {
                    query = query.Take(limit.Value);
                }
                
                var logs = query.ToList();

                // Ensure consistent casing of properties for frontend
                var result = logs.Select(log => new
                {
                    plantLog_ID = log.PlantLog_ID,
                    plant_ID = log.Plant_ID,
                    dato_Tid = log.Dato_Tid,
                    temperatureLevel = log.TemperatureLevel,
                    lightLevel = log.LightLevel,
                    waterLevel = log.WaterLevel,
                    airHumidityLevel = log.AirHumidityLevel,
                    // Ensure plant data is properly included
                    plant = log.Plant != null ? new
                    {
                        plant_ID = log.Plant.Plant_ID,
                        plant_Name = log.Plant.Plant_Name,
                        plant_type = log.Plant.Plant_type,
                        user_ID = log.Plant.User_ID
                    } : null
                }).ToList();

                return Ok(result);
            }
            catch (Exception)
            {
                // Log undtagelsen hvis logning er konfigureret
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
                return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

            var plant = _context.Plants.FirstOrDefault(p => p.Plant_ID == log.Plant_ID);
            if (plant == null)
            {
                // If plant doesn't exist but has a valid ID, create a default plant for the user
                if (log.Plant_ID > 0)
                {
                    plant = new Plant
                    {
                        Plant_ID = log.Plant_ID,
                        Plant_Name = "My First Plant",
                        Plant_type = "Default Plant",
                        User_ID = userId
                    };
                    _context.Plants.Add(plant);
                    _context.SaveChanges();
                }
                else
                {
                return BadRequest("Plant ID not found in database.");
                }
            }

            if (plant.User_ID != userId)
                return Forbid("You do not have permission to log data for this plant. It belongs to another user.");

            log.Dato_Tid = DateTime.UtcNow;

            _context.PlantLogs.Add(log);
            _context.SaveChanges();

            var saved = _context.PlantLogs
                                .Include(p => p.Plant)
                                .First(p => p.PlantLog_ID == log.PlantLog_ID);

            return Ok(new { 
                message = "Log saved", 
                log = new
                {
                    plantLog_ID = saved.PlantLog_ID,
                    plant_ID = saved.Plant_ID,
                    dato_Tid = saved.Dato_Tid,
                    temperatureLevel = saved.TemperatureLevel,
                    lightLevel = saved.LightLevel,
                    waterLevel = saved.WaterLevel,
                    airHumidityLevel = saved.AirHumidityLevel,
                    plant = saved.Plant != null ? new
                    {
                        plant_ID = saved.Plant.Plant_ID,
                        plant_Name = saved.Plant.Plant_Name,
                        plant_type = saved.Plant.Plant_type
                    } : null
                }
            });
        }

        // GET: api/plantlog/latest?plantId=1
        [Authorize]
        [HttpGet("latest")]
        public IActionResult GetLatest(int plantId)
        {
            try
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                    return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

                // For nye brugere uden planter endnu, angiv standarddata for indledende visning
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
                        airHumidityLevel = 50.0,
                        plant = new
                        {
                            plant_ID = 1,
                            plant_Name = "My First Plant",
                            plant_type = "Default Plant",
                            user_ID = userId
                        }
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
                        airHumidityLevel = 50.0,
                        plant = new
                        {
                            plant_ID = plantId,
                            plant_Name = "Default Plant",
                            plant_type = "Default Plant",
                            user_ID = userId
                        }
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
                    // Opret en standardlog hvis ingen eksisterer
                    return Ok(new
                    {
                        plantLog_ID = 0,
                        plant_ID = plantId,
                        dato_Tid = DateTime.UtcNow,
                        temperatureLevel = 22.5,
                        lightLevel = 500.0,
                        waterLevel = 50.0,
                        airHumidityLevel = 50.0,
                        plant = new
                        {
                            plant_ID = plant.Plant_ID,
                            plant_Name = plant.Plant_Name,
                            plant_type = plant.Plant_type,
                            user_ID = plant.User_ID
                        }
                    });
                }

                return Ok(new
                {
                    plantLog_ID = latest.PlantLog_ID,
                    plant_ID = latest.Plant_ID,
                    dato_Tid = latest.Dato_Tid,
                    temperatureLevel = latest.TemperatureLevel,
                    lightLevel = latest.LightLevel,
                    waterLevel = latest.WaterLevel,
                    airHumidityLevel = latest.AirHumidityLevel,
                    plant = plant != null ? new
                    {
                        plant_ID = plant.Plant_ID,
                        plant_Name = plant.Plant_Name,
                        plant_type = plant.Plant_type,
                        user_ID = plant.User_ID
                    } : null
                });
            }
            catch (Exception)
            {
                // Log undtagelsen hvis logning er konfigureret
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
                    return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

                // Tjek om dette er standard plante ID 1 for en ny bruger
                // Hvis ingen planter eksisterer endnu, returner en standardværdi
                if (plantId == 1 && !_context.Plants.Any(p => p.Plant_ID == plantId))
                {
                    return Ok(50.0); // Returner en standard mellemværdi (50%)
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
                    : Ok(50.0); // Returner en standardværdi hvis ingen logs eksisterer
            }
            catch (Exception)
            {
                // Log undtagelsen hvis logning er konfigureret
                return StatusCode(500, $"An error occurred while retrieving {label} data.");
            }
        }
    }
}