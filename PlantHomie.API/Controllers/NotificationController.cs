using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Models;
using System.Linq;

namespace PlantHomie.API.Controllers //test
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
        [HttpGet]
        public IActionResult GetAll()
        {
            var notifications = _context.Notifications
                .Include(n => n.Plant)
                .Include(n => n.User)
                .OrderByDescending(n => n.Dato_Tid)
                .ToList();

            if (!notifications.Any())
                return NotFound("Ingen notifikationer fundet.");

            return Ok(notifications);
        }

        // POST: api/notification
        [HttpPost]
        public IActionResult Create([FromBody] Notification notification)
        {
            if (notification == null)
                return BadRequest("Data mangler.");

            if (!_context.Plants.Any(p => p.Plant_ID == notification.Plant_ID))
                return BadRequest("Plant_ID findes ikke i databasen.");

            if (!_context.Users.Any(u => u.User_ID == notification.User_ID))
                return BadRequest("User_ID findes ikke i databasen.");

            notification.Dato_Tid = DateTime.UtcNow;

            _context.Notifications.Add(notification);
            _context.SaveChanges();

            var savedNotification = _context.Notifications
                .Include(n => n.Plant)
                .Include(n => n.User)
                .FirstOrDefault(n => n.Notification_ID == notification.Notification_ID);

            return Ok(new { message = "Notifikation oprettet", notification = savedNotification });
        }

        // GET: api/notification/latest
        [HttpGet("latest")]
        public IActionResult GetLatest()
        {
            var latestNotification = _context.Notifications
                .OrderByDescending(n => n.Dato_Tid)
                .FirstOrDefault();

            if (latestNotification == null)
                return NotFound("Ingen notifikationer fundet.");

            return Ok(latestNotification);
        }
    }
}



