using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Models;
using PlantHomie.API.Services;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;
using System.IO;
using System.Text.Json;

namespace PlantHomie.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlantController : ControllerBase
    {
        private readonly PlantHomieContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly RandomSensorDataService _sensorDataService;
        private readonly NotificationService _notificationService;

        public PlantController(
            PlantHomieContext context, 
            IWebHostEnvironment env, 
            RandomSensorDataService sensorDataService,
            NotificationService notificationService)
        {
            _context = context;
            _env = env;
            _sensorDataService = sensorDataService;
            _notificationService = notificationService;
        }

        // GET: api/plant
        [Authorize]
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                    return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

                var plants = _context.Plants
                            .Where(p => p.User_ID == userId)
                            .OrderBy(p => p.Plant_Name)
                            .ToList();

                // Returner altid listen, også selvom den er tom
                return Ok(plants);
            }
            catch (Exception)
            {
                // Log undtagelsen hvis logning er konfigureret
                return StatusCode(500, "An error occurred while retrieving plants.");
            }
        }

        // GET: api/plant/latest
        [Authorize]
        [HttpGet("latest")]
        public IActionResult GetLatest()
        {
            try
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                    return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

                var latest = _context.Plants
                                   .Where(p => p.User_ID == userId)
                                   .OrderByDescending(p => p.Plant_ID)
                                   .FirstOrDefault();

                if (latest is null)
                {
                    // Returner standard planteobjekt når ingen planter eksisterer endnu
                    return Ok(new { 
                        plant_ID = 0,
                        plant_Name = "No plants yet",
                        plant_type = "Default",
                        user_ID = userId
                    });
                }

                return Ok(latest);
            }
            catch (Exception)
            {
                // Log undtagelsen hvis logning er konfigureret
                return StatusCode(500, "An error occurred while retrieving the latest plant.");
            }
        }

        // POST: api/plant  (multipart/form-data)
        [Authorize]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "Creates a plant with optional image")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] PlantCreateRequest data)
        {
            if (data == null)
                return BadRequest("Data is missing.");

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

            if (data.User_ID != 0 && data.User_ID != userId)
                return BadRequest("Cannot create plants for other users. User_ID from token will be used.");

            // Check if plant name is unique for this user
            if (await _context.Plants.AnyAsync(p => p.User_ID == userId && p.Plant_Name.ToLower() == data.Plant_Name.Trim().ToLower()))
            {
                return BadRequest("You already have a plant with this name. Please choose a different name.");
            }

            string? imageUrl = null;
            if (data.Image is { Length: > 0 })
            {
                try
                {
                    // Create the uploads directory in wwwroot
                    var wwwrootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadDir = Path.Combine(wwwrootPath, "uploads");
                    
                    if (!Directory.Exists(wwwrootPath))
                        Directory.CreateDirectory(wwwrootPath);
                        
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                    // Generate a unique filename
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(data.Image.FileName)}";
                var filePath = Path.Combine(uploadDir, fileName);

                    // Copy the image to the uploads directory
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await data.Image.CopyToAsync(stream);
                }

                    // Store the URL path that will be used to serve the image
                imageUrl = $"/uploads/{fileName}";
                    Console.WriteLine($"Image saved successfully at {filePath}, URL: {imageUrl}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving image: {ex.Message}");
                    // Don't throw - continue without an image if there's an error
                }
            }

            var plant = new Plant
            {
                Plant_Name = data.Plant_Name.Trim(),
                Plant_type = data.Plant_type.Trim(),
                ImageUrl = imageUrl,
                User_ID = userId
            };

            _context.Plants.Add(plant);
            await _context.SaveChangesAsync();

            // Generate random sensor data for the new plant
            var (moisture, humidity, temperature) = _sensorDataService.GenerateRandomSensorData();

            // Create a plant log with the generated sensor data
            var plantLog = new PlantLog
            {
                Plant_ID = plant.Plant_ID,
                Dato_Tid = DateTime.UtcNow,
                TemperatureLevel = temperature,
                LightLevel = 500, // Default light level
                WaterLevel = moisture,
                AirHumidityLevel = humidity
            };

            _context.PlantLogs.Add(plantLog);
            await _context.SaveChangesAsync();

            // Get user details for notification
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                // Check if sensor data requires notification
                await _notificationService.CheckAndSendNotificationAsync(plant, user, temperature, moisture, humidity);
            }

            return Ok(new
            {
                plant = plant,
                sensorData = new
                {
                    moisture = moisture,
                    humidity = humidity,
                    temperature = temperature
                }
            });
        }

        // DELETE: api/plant/{id}
        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                    return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

                var plant = await _context.Plants.FindAsync(id);
                if (plant is null) return NotFound($"Plant with ID {id} not found");

                if (plant.User_ID != userId)
                    return Forbid("You do not have permission to delete this plant. The plant belongs to another user.");

                Console.WriteLine($"Starting deletion of plant {id} for user {userId}");

                // Manually delete related records to avoid constraint violations
                try 
                {
                    // 1. First remove all notifications for this plant
                    Console.WriteLine($"Deleting notifications for plant {id}");
                    var notifications = _context.Notifications.Where(n => n.Plant_ID == id).ToList();
                    Console.WriteLine($"Found {notifications.Count} notifications to delete");
                    
                    foreach (var notification in notifications)
                    {
                        _context.Notifications.Remove(notification);
                    }
                    await _context.SaveChangesAsync();
                    Console.WriteLine("Notifications deleted successfully");
                    
                    // 2. Remove all plant logs for this plant
                    Console.WriteLine($"Deleting plant logs for plant {id}");
                    var plantLogs = _context.PlantLogs.Where(pl => pl.Plant_ID == id).ToList();
                    Console.WriteLine($"Found {plantLogs.Count} plant logs to delete");
                    
                    foreach (var log in plantLogs)
                    {
                        _context.PlantLogs.Remove(log);
                    }
                    await _context.SaveChangesAsync();
                    Console.WriteLine("Plant logs deleted successfully");
                    
                    // 3. Finally remove the plant itself
                    Console.WriteLine($"Deleting plant {id}");
                    _context.Plants.Remove(plant);
                    await _context.SaveChangesAsync();
                    Console.WriteLine("Plant deleted successfully");
                    
                    // 4. Delete the image file if it exists
                    if (!string.IsNullOrEmpty(plant.ImageUrl))
                    {
                        try
                        {
                            var wwwrootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                            var path = Path.Combine(wwwrootPath, plant.ImageUrl.TrimStart('/'));
                            Console.WriteLine($"Checking for image file at: {path}");
                            
                            if (System.IO.File.Exists(path))
                            {
                                Console.WriteLine($"Deleting image file: {path}");
                                System.IO.File.Delete(path);
                                Console.WriteLine("Image file deleted successfully");
                            }
                            else
                            {
                                Console.WriteLine("Image file not found");
                            }
                        }
                        catch (IOException ex)
                        {
                            // Just log the error but continue with the delete operation
                            Console.WriteLine($"Failed to delete image file: {ex.Message}");
                        }
                    }
                    
                    return Ok(new { message = $"Plant with ID {id} was deleted successfully" });
                }
                catch (Exception ex)
                {
                    var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                    Console.WriteLine($"Error in delete operation: {ex.Message}");
                    Console.WriteLine($"Inner exception: {innerMessage}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    // Fallback to raw SQL as a last resort
                    try 
                    {
                        Console.WriteLine("Attempting to delete with raw SQL commands...");
                        
                        // Delete notifications first (respect foreign key constraints)
                        await _context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM [Notification] WHERE Plant_ID = {0}", id);
                        Console.WriteLine("Deleted notifications with SQL");
                        
                        // Delete plant logs
                        await _context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM [PlantLog] WHERE Plant_ID = {0}", id);
                        Console.WriteLine("Deleted plant logs with SQL");
                        
                        // Delete the plant itself
                        await _context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM [Plant] WHERE Plant_ID = {0}", id);
                        Console.WriteLine("Deleted plant with SQL");
                        
                        return Ok(new { message = $"Plant with ID {id} was deleted successfully (using SQL fallback)" });
                    }
                    catch (Exception sqlEx)
                    {
                        Console.WriteLine($"SQL fallback deletion also failed: {sqlEx.Message}");
                        throw; // Re-throw to be caught by outer try/catch
                    }
                }
            }
            catch (Exception ex) 
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                Console.WriteLine($"Overall error deleting plant: {ex.Message}");
                Console.WriteLine($"Inner exception: {innerMessage}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return StatusCode(500, new { error = "Database error", message = $"Failed to delete plant: {ex.Message}" });
            }
        }
    }
}
