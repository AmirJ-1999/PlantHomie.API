using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Models;
using System;
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

        // ------------------------------------------------------
        // GET: api/plantlog
        // ------------------------------------------------------
        [HttpGet]
        public IActionResult Index()
        {
            var logs = _context.PlantLogs
                               .Include(p => p.Plant)
                               .OrderByDescending(p => p.Dato_Tid)
                               .ToList();

            return logs.Any() ? Ok(logs) : NotFound("Ingen data fundet.");
        }

        // ------------------------------------------------------
        // POST: api/plantlog
        // ------------------------------------------------------
        [HttpPost]
        public IActionResult PostLog([FromBody] PlantLog log)
        {
            if (log == null)
                return BadRequest("Data mangler.");

            if (!_context.Plants.Any(p => p.Plant_ID == log.Plant_ID))
                return BadRequest("Plant_ID findes ikke i databasen.");

            // Sæt tidsstempel til nu
            log.Dato_Tid = DateTime.UtcNow;

            _context.PlantLogs.Add(log);
            _context.SaveChanges();

            var saved = _context.PlantLogs
                                .Include(p => p.Plant)
                                .First(p => p.PlantLog_ID == log.PlantLog_ID);

            return Ok(new { message = "Log gemt", log = saved });
        }

        // ------------------------------------------------------
        // GET: api/plantlog/latest?plantId=1
        // ------------------------------------------------------
        [HttpGet("latest")]
        public IActionResult GetLatest(int plantId)
        {
            var latest = _context.PlantLogs
                                 .Where(p => p.Plant_ID == plantId)
                                 .OrderByDescending(p => p.Dato_Tid)
                                 .FirstOrDefault();

            return latest is null
                ? NotFound("Ingen data fundet for den plante.")
                : Ok(latest);
        }

        // ------------------------------------------------------
        // GET: api/plantlog/temperature/1
        // ------------------------------------------------------
        [HttpGet("temperature/{plantId}")]
        public IActionResult GetTemperature(int plantId) =>
            GetSingleValue(plantId, log => log.TemperatureLevel, "temperatur");

        // ------------------------------------------------------
        // GET: api/plantlog/airhumidity/1
        // ------------------------------------------------------
        [HttpGet("airhumidity/{plantId}")]
        public IActionResult GetAirHumidity(int plantId) =>
            GetSingleValue(plantId, log => log.AirHumidityLevel, "luftfugtighed");

        // ------------------------------------------------------
        // GET: api/plantlog/soilmoisture/1
        // ------------------------------------------------------
        [HttpGet("soilmoisture/{plantId}")]
        public IActionResult GetSoilMoisture(int plantId) =>
            GetSingleValue(plantId, log => log.WaterLevel, "jordfugtighed");

        // ------------------------------------------------------
        // Fælles helper-metode
        // ------------------------------------------------------
        private IActionResult GetSingleValue(
            int plantId,
            Func<PlantLog, double?> selector,
            string label)
        {
            var value = _context.PlantLogs
                                .Where(p => p.Plant_ID == plantId)
                                .OrderByDescending(p => p.Dato_Tid)
                                .Select(selector)
                                .FirstOrDefault();

            return value.HasValue
                ? Ok(value.Value)
                : NotFound($"Ingen {label}data fundet");
        }
    }
}