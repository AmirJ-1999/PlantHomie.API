using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlantHomie.API.Models
{
    public class PlantLog
    {
        [Key]
        public int PlantLog_ID { get; set; }

        public int Plant_ID { get; set; }
        public DateTime Dato_Tid { get; set; }

        // Sensor data
        public double? TemperatureLevel { get; set; }
        public double? LightLevel { get; set; }
        public double? WaterLevel { get; set; }
        public double? AirHumidityLevel { get; set; }

        [ForeignKey(nameof(Plant_ID))]
        public Plant? Plant { get; set; }
    }
}