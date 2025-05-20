using PlantHomie.API.Data;
using PlantHomie.API.Models;

namespace PlantHomie.API.Services
{
    public class NotificationService
    {
        private readonly PlantHomieContext _context;

        public NotificationService(PlantHomieContext context)
        {
            _context = context;
        }

        public async Task CheckAndSendNotificationAsync(Plant plant, User user, double temp, double soil, double humidity)
        {
            // Check ranges for different status levels
            bool outsideNormal =
                temp < 10 || temp > 30 ||
                soil < 20 || soil > 80 ||
                humidity < 30 || humidity > 70;

            bool critical =
                temp < 5 || temp > 35 ||
                soil < 10 || soil > 90 ||
                humidity < 20 || humidity > 80;

            // Get appropriate notification message
            string message = GetNotificationMessage(plant?.Plant_Name, temp, soil, humidity, critical);
            
            // Decide on notification type
            string notificationType = GetNotificationType(temp, soil, humidity);
            
            // Determine severity
            string severity = critical ? "Critical" : (outsideNormal ? "Warning" : null);
            
            // Send notifications for both critical and outside normal range
            if (critical || outsideNormal)
            {
                var notification = new Notification
                {
                    User_ID = user.User_ID,
                    Plant_ID = plant.Plant_ID,
                    Dato_Tid = DateTime.UtcNow,
                    Plant_Type = plant.Plant_type,
                    Type = notificationType,
                    Message = message,
                    NotificationType = severity,
                    IsRead = false
                };

                // Save notification to database
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"Created notification for {plant.Plant_Name}: {message}");
            }
        }
        
        private string GetNotificationMessage(string? plantName, double temp, double soil, double humidity, bool isCritical)
        {
            // Use null coalescing operator to provide a default value if plantName is null
            plantName = plantName ?? "Your plant";  // Default value if plantName is null

            List<string> issues = new List<string>();
            List<string> sensorValues = new List<string>();
            string prefix = isCritical ? "Critical: " : "Warning: ";
            
            // Add all sensor values in a consistent format
            sensorValues.Add($"temperature ({temp:F1}°C)");
            sensorValues.Add($"soil ({soil:F1}%)");
            sensorValues.Add($"humidity ({humidity:F1}%)");
            
            // Check temperature
            if (temp < 5)
                issues.Add($"{plantName} has critically low temperature ({temp:F1}°C). Move to a warmer location immediately!");
            else if (temp < 10)
                issues.Add($"{plantName} has low temperature ({temp:F1}°C). Consider moving to a warmer spot.");
            else if (temp > 35)
                issues.Add($"{plantName} has critically high temperature ({temp:F1}°C). Move away from heat sources immediately!");
            else if (temp > 30)
                issues.Add($"{plantName} has high temperature ({temp:F1}°C). Consider moving to a cooler spot.");
            
            // Check soil moisture
            if (soil < 10)
                issues.Add($"{plantName} has critically dry soil ({soil:F1}%). Water immediately!");
            else if (soil < 20)
                issues.Add($"{plantName} has dry soil ({soil:F1}%). Plant needs water soon.");
            else if (soil > 90)
                issues.Add($"{plantName} is critically over-watered ({soil:F1}%). Reduce watering immediately!");
            else if (soil > 80)
                issues.Add($"{plantName} is getting over-watered ({soil:F1}%). Consider letting the soil dry out.");
            
            // Check humidity
            if (humidity < 20)
                issues.Add($"Air humidity around {plantName} is critically low ({humidity:F1}%). Use a humidifier immediately!");
            else if (humidity < 30)
                issues.Add($"Air humidity around {plantName} is low ({humidity:F1}%). Consider misting the plant.");
            else if (humidity > 90)
                issues.Add($"Air humidity around {plantName} is critically high ({humidity:F1}%). Improve air circulation immediately!");
            else if (humidity > 80)
                issues.Add($"Air humidity around {plantName} is high ({humidity:F1}%). Consider improving air circulation.");
            
            // Format the message
            string message;
            if (issues.Count == 0)
                message = $"{plantName} needs attention";
            else if (issues.Count == 1)
                message = $"{prefix}{issues[0]}";
            else
                message = $"{prefix}{string.Join(". ", issues)}";
                
            // Append current readings if there's an actual issue
            if (issues.Count > 0) {
                message += $" Current readings: {string.Join(", ", sensorValues)}";
            }
                
            return message;
        }
        
        private string GetNotificationType(double temp, double soil, double humidity)
        {
            // Determine the most severe issue to categorize the notification
            
            // Check for critical conditions first
            if (temp < 5 || temp > 35)
                return "Temperature";
            if (soil < 10 || soil > 90)
                return "Moisture";
            if (humidity < 20 || humidity > 80)
                return "Humidity";
            
            // Then check for warning conditions
            if (temp < 10 || temp > 30)
                return "Temperature";
            if (soil < 20 || soil > 80)
                return "Moisture";
            if (humidity < 30 || humidity > 70)
                return "Humidity";
                
            return "System";
        }
    }
}
