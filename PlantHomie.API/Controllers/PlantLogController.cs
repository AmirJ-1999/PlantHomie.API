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
        // Simpel test for browser
        [HttpGet]
        public IActionResult Index()
        {
            var logs = _context.PlantLogs
                .Include(p => p.Plant) // Inkluder relateret Plant-data
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

            // Hent loggen igen med relateret Plant-data
            var savedLog = _context.PlantLogs
                .Include(p => p.Plant) // Inkluder relateret Plant-data
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
    }
}