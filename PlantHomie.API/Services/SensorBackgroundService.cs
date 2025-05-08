using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlantHomie.API.Data;
using PlantHomie.API.Models;

namespace PlantHomie.API.Services
{
    // Denne klasse laver en baggrundsservice, der automatisk indsætter sensordata i databasen
    public class SensorBackgroundService : BackgroundService
    {
        // Dependency injection af PlanHomieContext.
        // Bruges til at få adgang til services som som databasen.
        private readonly IServiceProvider _services;

        public SensorBackgroundService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Løkke der kører indtil applikationen stoppes.
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<PlantHomieContext>();

                // Opretter en ny Plantlog med tilfældige værdier for temperatur, luftfugtighed og hjordfugtighed.
                var newLog = new PlantLog
                {
                    Plant_ID = 1, // her kan vi ændre ID, hvis du vil måle på andre planter
                    Dato_Tid = DateTime.UtcNow, // Tidspunkt for målingen 
                    Temperaturelevel = new Random().NextDouble() * 40,
                    AirHumidityLevel = new Random().NextDouble() * 100,
                    WaterLevel = new Random().NextDouble() * 100
                };
                // Gemmer målingen i databasen
                await context.PlantLogs.AddAsync(newLog, stoppingToken);
                await context.SaveChangesAsync(stoppingToken);

                // Venter en time før næste måling
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
