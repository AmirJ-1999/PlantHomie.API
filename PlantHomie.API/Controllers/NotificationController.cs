using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized("Invalid token or missing User ID claim.");
                
            var notifications = _context.Notifications
                .Include(n => n.Plant)
                .Include(n => n.User)
                .Where(n => n.User_ID == userId)
                .OrderByDescending(n => n.Dato_Tid)
                .ToList();

            if (!notifications.Any())
                return NotFound("No notifications found.");

            return Ok(notifications);
        }

        // POST: api/notification
        [Authorize]
        [HttpPost]
        public IActionResult Create([FromBody] Notification notification)
        {
            if (notification == null)
                return BadRequest("Data is missing.");
                
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized("Invalid token or missing User ID claim.");

            if (!_context.Plants.Any(p => p.Plant_ID == notification.Plant_ID))
                return BadRequest("Plant_ID not found in database.");

            // Check if user owns this plant
            var plant = _context.Plants.FirstOrDefault(p => p.Plant_ID == notification.Plant_ID);
            if (plant == null)
                return BadRequest("Plant not found.");
                
            if (plant.User_ID != userId)
                return Forbid("You do not have permission to create notifications for this plant.");

            notification.User_ID = userId;
            notification.Dato_Tid = DateTime.UtcNow;

            _context.Notifications.Add(notification);
            _context.SaveChanges();

            var savedNotification = _context.Notifications
                .Include(n => n.Plant)
                .Include(n => n.User)
                .FirstOrDefault(n => n.Notification_ID == notification.Notification_ID);

            return Ok(new { message = "Notification created", notification = savedNotification });
        }

        // GET: api/notification/latest
        [Authorize]
        [HttpGet("latest")]
        public IActionResult GetLatest()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized("Invalid token or missing User ID claim.");
                
            var latestNotification = _context.Notifications
                .Where(n => n.User_ID == userId)
                .OrderByDescending(n => n.Dato_Tid)
                .FirstOrDefault();

            if (latestNotification == null)
                return NotFound("No notifications found.");

            return Ok(latestNotification);
        }
    }
}



