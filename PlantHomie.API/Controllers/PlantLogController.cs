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
        [Authorize] // Endpoint kræver autentifikation (gyldigt JWT token)
        [HttpGet] // HTTP GET endpoint: api/plantlog
        public IActionResult Index()
        {
            // Henter User ID fra JWT tokenets claims for at sikre, at kun brugerens egne logs hentes.
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized("Invalid token or missing User ID claim."); // HTTP 401 hvis token/claim er ugyldigt

            var logs = _context.PlantLogs
                               .Include(p => p.Plant) // Inkluderer relaterede Plant-objekter (for at få plantenavn etc.)
                               .Where(p => p.Plant != null && p.Plant.User_ID == userId) // Filtrerer på User_ID via den relaterede Plant
                               .OrderByDescending(p => p.Dato_Tid) // Sorterer nyeste logs først
                               .ToList(); // Udfører forespørgslen og returnerer en liste

            return logs.Any() ? Ok(logs) : NotFound("No data found."); // HTTP 200 med logs, eller 404 hvis ingen logs findes
        }

        // POST: api/plantlog
        [Authorize] // Kræver autentifikation
        [HttpPost] // HTTP POST endpoint: api/plantlog
        public IActionResult PostLog([FromBody] PlantLog log) // Modtager PlantLog data fra request body
        {
            if (log == null) // Grundlæggende validering af input
                return BadRequest("Missing data."); // HTTP 400 hvis request body er tom

            // Henter User ID fra token
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized("Invalid token or missing User ID claim.");

            // Verificerer at den angivne Plant_ID eksisterer og tilhører den autentificerede bruger
            var plant = _context.Plants.FirstOrDefault(p => p.Plant_ID == log.Plant_ID);
            if (plant == null)
                return BadRequest("Plant ID not found in database."); // HTTP 400 hvis planten ikke findes

            if (plant.User_ID != userId) // Autorisationstjek: Bruger må kun logge på egne planter
                return Forbid("You do not have permission to log data for this plant. It belongs to another user."); // HTTP 403

            // Sætter Dato_Tid til aktuel UTC tid. Klienten bør ikke sætte dette.
            log.Dato_Tid = DateTime.UtcNow;

            _context.PlantLogs.Add(log); // Tilføjer log til DbContext
            _context.SaveChanges(); // Gemmer til databasen

            // Henter den gemte log igen, inklusiv relaterede data, for at returnere et komplet objekt
            var saved = _context.PlantLogs
                                .Include(p => p.Plant)
                                .First(p => p.PlantLog_ID == log.PlantLog_ID);

            return Ok(new { message = "Log saved", log = saved }); // HTTP 200 med bekræftelse og den gemte log
        }

        // GET: api/plantlog/latest?plantId=1
        [Authorize] // Kræver autentifikation
        [HttpGet("latest")] // HTTP GET endpoint: api/plantlog/latest
        public IActionResult GetLatest(int plantId) // plantId modtages fra query parameter
        {
            // Henter User ID fra token
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized("Invalid token or missing User ID claim.");

            // Verificerer at planten eksisterer og tilhører den autentificerede bruger
            var plant = _context.Plants.FirstOrDefault(p => p.Plant_ID == plantId);
            if (plant == null)
                return NotFound("Plant not found"); // HTTP 404 hvis planten ikke findes

            if (plant.User_ID != userId) // Autorisationstjek
                return Forbid("You do not have permission to access this plant. It belongs to another user."); // HTTP 403

            var latest = _context.PlantLogs
                                 .Where(p => p.Plant_ID == plantId) // Filtrerer på den specifikke Plant_ID
                                 .OrderByDescending(p => p.Dato_Tid) // Nyeste først
                                 .FirstOrDefault(); // Henter den seneste log eller null

            return latest is null
                ? NotFound("No data found for this plant.") // HTTP 404 hvis ingen logs for denne plante
                : Ok(latest); // HTTP 200 med den seneste log
        }

        // GET: api/plantlog/temperature/1
        [Authorize]
        [HttpGet("temperature/{plantId}")] // Route parameter {plantId}
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

        // Privat helper-metode til at hente den seneste værdi for et specifikt sensor-felt (fx temperatur)
        private IActionResult GetSingleValue(
            int plantId,
            Func<PlantLog, double?> selector, // En funktion (lambda) der specificerer hvilket felt der skal hentes (fx log => log.TemperatureLevel)
            string label) // Bruges til fejlbeskeden hvis ingen data findes
        {
            // Henter User ID fra token for autorisation
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized("Invalid token or missing User ID claim.");

            // Verificerer at planten eksisterer og tilhører den autentificerede bruger
            var plant = _context.Plants.FirstOrDefault(p => p.Plant_ID == plantId);
            if (plant == null)
                return NotFound("Plant not found");

            if (plant.User_ID != userId) // Autorisationstjek
                return Forbid("You do not have permission to access this plant. It belongs to another user.");

            var value = _context.PlantLogs
                                .Where(p => p.Plant_ID == plantId) // Filtrer på Plant_ID
                                .OrderByDescending(p => p.Dato_Tid) // Nyeste først
                                .Select(selector) // Vælger det specifikke felt vha. selector-funktionen
                                .FirstOrDefault(); // Henter den seneste værdi eller null

            return value.HasValue // Tjekker om værdien er fundet (ikke null)
                ? Ok(value.Value) // HTTP 200 med værdien
                : NotFound($"No {label} data found"); // HTTP 404 hvis ingen data for det specifikke felt
        }
    }
}