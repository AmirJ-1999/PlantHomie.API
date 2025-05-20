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
        public IActionResult GetAll([FromQuery] int? plantId = null)
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

                // Use a query to filter notifications
                var query = _context.Notifications
                    .Where(n => n.User_ID == userId);
                    
                // Add plant ID filter if provided
                if (plantId.HasValue)
                {
                    query = query.Where(n => n.Plant_ID == plantId.Value);
                }
                    
                // Complete the query with ordering and projection
                var notifications = query
                    .OrderByDescending(n => n.Dato_Tid)
                    .Select(n => new 
                    {
                        n.Notification_ID,
                        n.Plant_ID,
                        n.User_ID,
                        n.Dato_Tid,
                        n.Type,
                        n.NotificationType,
                        n.IsRead,
                        Plant = n.Plant
                    })
                    .AsNoTracking()
                    .ToList();

                // Get the actual notifications to use for messages
                var notificationIds = notifications.Select(n => n.Notification_ID).ToList();
                var actualNotifications = _context.Notifications
                    .Include(n => n.Plant)
                    .Where(n => notificationIds.Contains(n.Notification_ID))
                    .ToDictionary(n => n.Notification_ID);

                // Format the response with consistent property names and handle null values safely
                var result = notifications.Select(n => 
                {
                    string message;
                    if (actualNotifications.TryGetValue(n.Notification_ID, out var actualNotification))
                    {
                        message = !string.IsNullOrEmpty(actualNotification.Message)
                            ? actualNotification.Message
                            : $"Notification for {n.Plant?.Plant_Name ?? "Unknown Plant"}";
                    }
                    else
                    {
                        message = $"Notification for {n.Plant?.Plant_Name ?? "Unknown Plant"}";
                    }

                    return new
                {
                    notification_ID = n.Notification_ID,
                    plant_ID = n.Plant_ID,
                    user_ID = n.User_ID,
                    dato_Tid = n.Dato_Tid,
                        type = n.Type ?? "System",
                        message = message,
                        notificationType = n.NotificationType,
                        isRead = n.IsRead,
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
                    };
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
                Console.WriteLine($"Received notification data: {JsonSerializer.Serialize(notification)}");
                
                if (notification == null)
                {
                    Console.WriteLine("Error: Notification object is null");
                    return BadRequest(new { error = "Bad request", message = "Data is missing." });
                }

                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                {
                    Console.WriteLine("Error: Invalid user ID in token");
                    return Unauthorized("Invalid or missing authentication token. Please login first to obtain a valid JWT token and include it in the Authorization header.");
                }

                // Handle the case where plant doesn't exist yet
                var plant = _context.Plants.FirstOrDefault(p => p.Plant_ID == notification.Plant_ID);
                if (plant == null)
                {
                    Console.WriteLine($"Plant not found with ID: {notification.Plant_ID}, creating default plant");
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
                    Console.WriteLine($"Authorization error: User {userId} doesn't own plant {notification.Plant_ID}");
                    return Forbid("You do not have permission to create notifications for this plant.");
                }

                // Set required properties
                notification.User_ID = userId;
                notification.Dato_Tid = DateTime.UtcNow;
                
                // Ensure the notification has a type
                if (string.IsNullOrEmpty(notification.Type))
                {
                    notification.Type = "System";
                }

                // Log before saving
                Console.WriteLine($"Saving notification: Plant_ID={notification.Plant_ID}, User_ID={notification.User_ID}, Type={notification.Type}, Message={notification.Message}");
                
                _context.Notifications.Add(notification);
                _context.SaveChanges();
                
                Console.WriteLine($"Notification saved successfully with ID: {notification.Notification_ID}");

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
                        type = notification.Type ?? "System",
                        message = notification.Message ?? $"Notification for {plant_name}",
                        notificationType = notification.NotificationType,
                        isRead = notification.IsRead,
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
                Console.WriteLine($"Error creating notification: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null) 
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                return StatusCode(500, new { error = "Database error", message = $"An error occurred while creating the notification: {ex.Message}" });
            }
        }

        // GET: api/notification/latest
        [Authorize]
        [HttpGet("latest")]
        public IActionResult GetLatest([FromQuery] int? plantId = null)
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

                // Build query to filter by user ID and optionally plant ID
                var query = _context.Notifications
                    .Where(n => n.User_ID == userId);
                    
                // Add plant ID filter if provided
                if (plantId.HasValue)
                {
                    query = query.Where(n => n.Plant_ID == plantId.Value);
                }

                // Complete query with ordering and projection
                var latest = query
                    .OrderByDescending(n => n.Dato_Tid)
                    .Select(n => new 
                    {
                        n.Notification_ID,
                        n.Plant_ID,
                        n.User_ID,
                        n.Dato_Tid,
                        n.Type,
                        n.NotificationType,
                        n.IsRead,
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
                // Get the actual notification to use with GetNotificationMessage
                var actualNotification = _context.Notifications
                    .Include(n => n.Plant)
                    .FirstOrDefault(n => n.Notification_ID == latest.Notification_ID);
                
                var message = !string.IsNullOrEmpty(actualNotification?.Message) 
                    ? actualNotification.Message 
                    : $"Notification for {latest.Plant?.Plant_Name ?? "Unknown Plant"}";
                
                var result = new
                {
                    notification_ID = latest.Notification_ID,
                    plant_ID = latest.Plant_ID,
                    user_ID = latest.User_ID,
                    dato_Tid = latest.Dato_Tid,
                    type = latest.Type ?? "System",
                    message = message,
                    notificationType = latest.NotificationType,
                    isRead = latest.IsRead,
                    plant = latest.Plant != null ? new
                    {
                        plant_ID = latest.Plant.Plant_ID,
                        plant_Name = latest.Plant.Plant_Name ?? "your plant",
                        plant_type = latest.Plant.Plant_type ?? "Unknown"
                    } : new
                    {
                        plant_ID = 0,
                        plant_Name = "Unknown Plant",
                        plant_type = "Unknown"
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Database error", message = "An error occurred while retrieving the latest notification." });
            }
        }

        // GET: api/notification/unread-count
        [Authorize]
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                    return Unauthorized("Invalid or missing authentication token.");

                // Count unread notifications for the user
                var count = await _context.Notifications
                    .Where(n => n.User_ID == userId && !n.IsRead)
                    .CountAsync();
                    
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Database error", message = "An error occurred while counting unread notifications." });
            }
        }

        // PUT: api/notification/mark-read/{id}
        [Authorize]
        [HttpPut("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                    return Unauthorized("Invalid or missing authentication token.");

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Notification_ID == id && n.User_ID == userId);
                    
                if (notification == null)
                    return NotFound("Notification not found or does not belong to the current user.");
                    
                notification.IsRead = true;
                await _context.SaveChangesAsync();
                
                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Database error", message = "An error occurred while updating the notification." });
            }
        }

        // PUT: api/notification/mark-all-read
        [Authorize]
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                    return Unauthorized("Invalid or missing authentication token.");

                var unreadNotifications = await _context.Notifications
                    .Where(n => n.User_ID == userId && !n.IsRead)
                    .ToListAsync();
                    
                if (unreadNotifications.Any())
                {
                    foreach (var notification in unreadNotifications)
                    {
                        notification.IsRead = true;
                    }
                    
                    await _context.SaveChangesAsync();
                }
                
                return Ok(new { message = $"Marked {unreadNotifications.Count} notifications as read" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Database error", message = "An error occurred while updating notifications." });
            }
        }

        private string GetNotificationMessage(Notification notification)
        {
            // Return the actual message if available
            if (!string.IsNullOrEmpty(notification.Message))
            {
                return notification.Message;
            }
            
            // Fallback to a default message
            return $"Notification for {notification.Plant?.Plant_Name ?? "Unknown Plant"}";
        }
    }
}
