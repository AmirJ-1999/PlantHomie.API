using PlantHomie.API.Models;

namespace PlantHomie.API.Services
{
    public class RandomSensorDataService
    {
        private readonly Random _random = new Random();
        
        // Generate ranges for normal, needs attention, and critical status
        private enum StatusRange
        {
            Normal,
            NeedsAttention,
            Critical
        }
        
        public (double moisture, double humidity, double temperature) GenerateRandomSensorData()
        {
            // Randomly select a status for this plant (with bias toward normal)
            StatusRange statusRange = GetRandomStatus();
            
            // Generate values based on the selected status
            var data = statusRange switch
            {
                StatusRange.Normal => GenerateNormalValues(),
                StatusRange.NeedsAttention => GenerateNeedsAttentionValues(),
                StatusRange.Critical => GenerateCriticalValues(),
                _ => GenerateNormalValues()
            };
            
            Console.WriteLine($"Generated sensor data: Moisture: {data.moisture}%, Humidity: {data.humidity}%, Temperature: {data.temperature}°C with status: {statusRange}");
            return data;
        }
        
        private StatusRange GetRandomStatus()
        {
            // 60% normal, 30% needs attention, 10% critical
            int value = _random.Next(1, 101);
            if (value <= 60)
                return StatusRange.Normal;
            else if (value <= 90)
                return StatusRange.NeedsAttention;
            else
                return StatusRange.Critical;
        }
        
        private (double moisture, double humidity, double temperature) GenerateNormalValues()
        {
            // Normal ranges based on NotificationService thresholds
            double moisture = _random.Next(20, 81); // 20-80%
            double humidity = _random.Next(30, 71); // 30-70%
            double temperature = _random.Next(10, 31); // 10-30°C
            
            return (moisture, humidity, temperature);
        }
        
        private (double moisture, double humidity, double temperature) GenerateNeedsAttentionValues()
        {
            // Values outside normal but not critical
            bool lowMoisture = _random.Next(2) == 0;
            double moisture = lowMoisture ? 
                _random.Next(10, 20) : // 10-19%
                _random.Next(81, 91);  // 81-90%
                
            bool lowHumidity = _random.Next(2) == 0;
            double humidity = lowHumidity ?
                _random.Next(20, 30) : // 20-29%
                _random.Next(71, 81);  // 71-80%
                
            bool lowTemperature = _random.Next(2) == 0;
            double temperature = lowTemperature ?
                _random.Next(5, 10) :  // 5-9°C
                _random.Next(31, 36);  // 31-35°C
                
            return (moisture, humidity, temperature);
        }
        
        private (double moisture, double humidity, double temperature) GenerateCriticalValues()
        {
            // Critical values
            bool lowMoisture = _random.Next(2) == 0;
            double moisture = lowMoisture ?
                _random.Next(1, 10) :  // 1-9%
                _random.Next(91, 101); // 91-100%
                
            bool lowHumidity = _random.Next(2) == 0;
            double humidity = lowHumidity ?
                _random.Next(1, 20) :  // 1-19%
                _random.Next(81, 101); // 81-100%
                
            bool lowTemperature = _random.Next(2) == 0;
            double temperature = lowTemperature ?
                _random.Next(-5, 5) :   // -5-4°C
                _random.Next(36, 45);   // 36-44°C
                
            return (moisture, humidity, temperature);
        }
    }
} 
