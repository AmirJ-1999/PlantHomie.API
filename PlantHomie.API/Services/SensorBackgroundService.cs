using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlantHomie.API.Data;
using PlantHomie.API.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlantHomie.API.Services
{
    /// <summary>
    /// Baggrundsservice som hver 6 timer, gemmer tilfældige sensor-målinger.
    /// </summary>
    public class SensorBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly Random _rnd = new Random();

        public SensorBackgroundService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<PlantHomieContext>();

                var newLog = new PlantLog
                {
                    Plant_ID = 1,                           // evt. skift til dynamisk ID
                    Dato_Tid = DateTime.UtcNow,
                    TemperatureLevel = _rnd.NextDouble() * 40,      // 0-40 °C
                    LightLevel = _rnd.NextDouble() * 100,     // 0-100 % lys
                    WaterLevel = _rnd.NextDouble() * 100,     // 0-100 % jordfugt
                    AirHumidityLevel = _rnd.NextDouble() * 100      // 0-100 % RH
                };

                await context.PlantLogs.AddAsync(newLog, stoppingToken);
                await context.SaveChangesAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }
    }
}