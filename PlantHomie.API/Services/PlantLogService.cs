using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Models;
using PlantHomie.API.Services;

namespace PlantHomie.API.Services
{
    public class PlantLogService
    {
        private readonly PlantHomieContext _context;
        private readonly NotificationService _notificationService;

        public PlantLogService(PlantHomieContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<object> GetLatestReadingAsync(int plantId, int userId)
        {
            // Check if plant belongs to user
            var plant = await _context.Plants
                .FirstOrDefaultAsync(p => p.Plant_ID == plantId && p.User_ID == userId);
                
            if (plant == null)
            {
                // For new users or missing plants, return sample data
                return new
                {
                    plantLog_ID = 0,
                    plant_ID = plantId,
                    dato_Tid = DateTime.UtcNow,
                    temperatureLevel = 22.5,
                    lightLevel = 500.0,
                    waterLevel = 50.0,
                    airHumidityLevel = 50.0,
                    plantStatus = "Normal",
                    plant = new
                    {
                        plant_ID = plantId,
                        plant_Name = "Default Plant",
                        plant_type = "Default Plant",
                        user_ID = userId
                    }
                };
            }
            
            // Get the latest log for the plant
            var latest = await _context.PlantLogs
                .Where(p => p.Plant_ID == plantId)
                .OrderByDescending(p => p.Dato_Tid)
                .FirstOrDefaultAsync();
                
            if (latest == null)
            {
                // No logs yet, return default values with plant info
                return new
                {
                    plantLog_ID = 0,
                    plant_ID = plantId,
                    dato_Tid = DateTime.UtcNow,
                    temperatureLevel = 22.5,
                    lightLevel = 500.0,
                    waterLevel = 50.0,
                    airHumidityLevel = 50.0,
                    plantStatus = "Normal",
                    plant = new
                    {
                        plant_ID = plant.Plant_ID,
                        plant_Name = plant.Plant_Name,
                        plant_type = plant.Plant_type,
                        user_ID = plant.User_ID
                    }
                };
            }
            
            // Evaluate the sensor readings based on NotificationService thresholds
            string plantStatus = EvaluatePlantStatus(latest);
            
            // Return the latest reading with plant info and status
            return new
            {
                plantLog_ID = latest.PlantLog_ID,
                plant_ID = latest.Plant_ID,
                dato_Tid = latest.Dato_Tid,
                temperatureLevel = latest.TemperatureLevel,
                lightLevel = latest.LightLevel,
                waterLevel = latest.WaterLevel,
                airHumidityLevel = latest.AirHumidityLevel,
                plantStatus = plantStatus,
                plant = new
                {
                    plant_ID = plant.Plant_ID,
                    plant_Name = plant.Plant_Name,
                    plant_type = plant.Plant_type,
                    user_ID = plant.User_ID
                }
            };
        }

        public string EvaluatePlantStatus(PlantLog log)
        {
            double temp = log.TemperatureLevel ?? 22.5;
            double soil = log.WaterLevel ?? 50.0;
            double humidity = log.AirHumidityLevel ?? 50.0;

            // Using the same thresholds as NotificationService
            bool outsideNormal =
                temp < 10 || temp > 30 ||
                soil < 20 || soil > 80 ||
                humidity < 30 || humidity > 70;

            bool critical =
                temp < 5 || temp > 35 ||
                soil < 10 || soil > 90 ||
                humidity < 20 || humidity > 80;

            if (critical)
            {
                return "Critical";
            }
            else if (outsideNormal)
            {
                return "Needs Attention";
            }
            else
            {
                return "Normal";
            }
        }
    }
}
