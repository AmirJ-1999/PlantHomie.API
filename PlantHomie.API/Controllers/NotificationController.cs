using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace PlantHomie.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly PlantHomieContext _context;

        public NotificationController(PlantHomieContext context)
        {
            _context = context;
        }

        // GET: api/notification
        [Authorize]
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                    return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

                // Check if user has any notifications
                var userHasNotifications = _context.Notifications.Any(n => n.User_ID == userId);
                
                // For new users, provide sample notification data
                if (!userHasNotifications)
                {
                    var sampleNotification = new
                    {
                        notification_ID = 0,
                        plant_ID = 1,
                        user_ID = userId,
                        dato_Tid = DateTime.UtcNow.AddHours(-1),
                        type = "System",
                        message = "Welcome to PlantHomie! This is your notification center.",
                        plant = new
                        {
                            plant_ID = 1,
                            plant_Name = "My First Plant",
                            plant_type = "Default Plant",
                            user_ID = userId
                        }
                    };
                    
                    return Ok(new[] { sampleNotification });
                }

                // Brug projektion for at undgå at EF prøver at tilgå Message-feltet
                var notifications = _context.Notifications
                    .Where(n => n.User_ID == userId)
                    .OrderByDescending(n => n.Dato_Tid)
                    .Select(n => new 
                    {
                        n.Notification_ID,
                        n.Plant_ID,
                        n.User_ID,
                        n.Dato_Tid,
                        n.Plant_Type,
                        Plant = n.Plant
                    })
                    .AsNoTracking()
                    .ToList();

                // Format the response with consistent property names and handle null values safely
                var result = notifications.Select(n => new
                {
                    notification_ID = n.Notification_ID,
                    plant_ID = n.Plant_ID,
                    user_ID = n.User_ID,
                    dato_Tid = n.Dato_Tid,
                    type = n.Plant_Type ?? "System",
                    // Brug en hardcoded besked i stedet for at hente fra databasen
                    message = $"Notification for {n.Plant?.Plant_Name ?? "Unknown Plant"}",
                    plant = n.Plant != null ? new
                    {
                        plant_ID = n.Plant.Plant_ID,
                        plant_Name = n.Plant.Plant_Name ?? "Unknown Plant",
                        plant_type = n.Plant.Plant_type ?? "Unknown",
                        user_ID = n.Plant.User_ID
                    } : new
                    {
                        plant_ID = 0,
                        plant_Name = "Unknown Plant",
                        plant_type = "Unknown",
                        user_ID = userId
                    }
                }).ToList();

                if (!result.Any())
                {
                    // Return empty list with 200 OK instead of throwing an error
                    return Ok(new List<object>());
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log exception and return error message
                return StatusCode(500, new { error = "Database error", message = "An error occurred while retrieving notifications." });
            }
        }

        // POST: api/notification
        [Authorize]
        [HttpPost]
        public IActionResult Create([FromBody] Notification notification)
        {
            try
            {
                if (notification == null)
                    return BadRequest(new { error = "Bad request", message = "Data is missing." });

                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                    return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

                // Handle the case where plant doesn't exist yet
                var plant = _context.Plants.FirstOrDefault(p => p.Plant_ID == notification.Plant_ID);
                if (plant == null)
                {
                    // If this is a new user without plants, create a default plant
                    plant = new Plant
                    {
                        Plant_Name = "My First Plant",
                        Plant_type = "Default Plant",
                        User_ID = userId
                    };
                    _context.Plants.Add(plant);
                    _context.SaveChanges();
                    
                    // Update the notification with the new plant ID
                    notification.Plant_ID = plant.Plant_ID;
                }
                else if (plant.User_ID != userId)
                {
                    return Forbid("You do not have permission to create notifications for this plant.");
                }

                notification.User_ID = userId;
                notification.Dato_Tid = DateTime.UtcNow;
                
                // Ensure the notification has a type
                if (string.IsNullOrEmpty(notification.Plant_Type))
                {
                    notification.Plant_Type = "System";
                }

                _context.Notifications.Add(notification);
                _context.SaveChanges();

                var plant_name = plant?.Plant_Name ?? "Unknown Plant";
                var plant_type = plant?.Plant_type ?? "Unknown";

                // Format the response with consistent property names
                var result = new
                {
                    message = "Notification created",
                    notification = new
                    {
                        notification_ID = notification.Notification_ID,
                        plant_ID = notification.Plant_ID,
                        user_ID = notification.User_ID,
                        dato_Tid = notification.Dato_Tid,
                        type = notification.Plant_Type ?? "System",
                        // Use a hardcoded message instead of accessing from database
                        message = $"Notification for {plant_name}",
                        plant = plant != null ? new
                        {
                            plant_ID = plant.Plant_ID,
                            plant_Name = plant_name,
                            plant_type = plant_type
                        } : new
                        {
                            plant_ID = 0,
                            plant_Name = "Unknown Plant",
                            plant_type = "Unknown"
                        }
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Database error", message = "An error occurred while creating the notification." });
            }
        }

        // GET: api/notification/latest
        [Authorize]
        [HttpGet("latest")]
        public IActionResult GetLatest()
        {
            try
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                    return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");

                // Check if user has any notifications
                var userHasNotifications = _context.Notifications.Any(n => n.User_ID == userId);
                
                // For new users, provide sample notification data
                if (!userHasNotifications)
                {
                    var sampleNotification = new
                    {
                        notification_ID = 0,
                        plant_ID = 1,
                        user_ID = userId,
                        dato_Tid = DateTime.UtcNow.AddHours(-1),
                        type = "System",
                        message = "Welcome to PlantHomie! Your plants will send notifications here.",
                        plant = new
                        {
                            plant_ID = 1,
                            plant_Name = "My First Plant",
                            plant_type = "Default Plant",
                            user_ID = userId
                        }
                    };
                    
                    return Ok(sampleNotification);
                }

                // Brug projektion for at undgå at EF prøver at tilgå Message-feltet
                var latest = _context.Notifications
                    .Where(n => n.User_ID == userId)
                    .OrderByDescending(n => n.Dato_Tid)
                    .Select(n => new 
                    {
                        n.Notification_ID,
                        n.Plant_ID,
                        n.User_ID,
                        n.Dato_Tid,
                        n.Plant_Type,
                        Plant = n.Plant
                    })
                    .AsNoTracking()
                    .FirstOrDefault();

                if (latest == null)
                {
                    // Return empty notification instead of 404
                    return Ok(new
                    {
                        notification_ID = 0,
                        plant_ID = 0,
                        user_ID = userId,
                        dato_Tid = DateTime.UtcNow,
                        type = "System",
                        message = "No notifications yet",
                        plant = new
                        {
                            plant_ID = 0,
                            plant_Name = "Unknown Plant",
                            plant_type = "Unknown",
                            user_ID = userId
                        }
                    });
                }

                // Format the response with consistent property names
                var result = new
                {
                    notification_ID = latest.Notification_ID,
                    plant_ID = latest.Plant_ID,
                    user_ID = latest.User_ID,
                    dato_Tid = latest.Dato_Tid,
                    type = latest.Plant_Type ?? "System",
                    // Use a hardcoded message instead of accessing from database
                    message = $"Notification for {latest.Plant?.Plant_Name ?? "your plant"}",
                    plant = latest.Plant != null ? new
                    {
                        plant_ID = latest.Plant.Plant_ID,
                        plant_Name = latest.Plant.Plant_Name ?? "Unknown Plant",
                        plant_type = latest.Plant.Plant_type ?? "Unknown",
                        user_ID = latest.Plant.User_ID
                    } : new
                    {
                        plant_ID = 0,
                        plant_Name = "Unknown Plant",
                        plant_type = "Unknown",
                        user_ID = userId
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Database error", message = "An error occurred while retrieving the latest notification." });
            }
        }
    }
}
