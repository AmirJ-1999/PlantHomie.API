using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<SensorBackgroundService> _logger;

        public SensorBackgroundService(IServiceProvider services, ILogger<SensorBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SensorBackgroundService starter.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("SensorBackgroundService: Udfører AddSensorData.");
                    await AddSensorData(stoppingToken);

                    _logger.LogInformation("SensorBackgroundService: Venter 6 timer til næste logning.");
                    await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("SensorBackgroundService stopper (OperationCanceledException).");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SensorBackgroundService: Uventet fejl. Venter 5 minutter før genforsøg.");

                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("SensorBackgroundService stopper under retry delay (OperationCanceledException).");
                        break;
                    }
                }
            }
            _logger.LogInformation("SensorBackgroundService er stoppet.");
        }

        private async Task AddSensorData(CancellationToken stoppingToken)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PlantHomieContext>();

            //Her hentes NotificationService, for at tjekke om en notifikation skal sendes
            var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

            // Tjekker først om der overhovedet er planter i databasen at logge data for.
            var anyPlants = await context.Plants.AnyAsync(stoppingToken);
            if (!anyPlants)
            {
                _logger.LogWarning("SensorBackgroundService: Ingen planter fundet i databasen. Kan ikke logge sensordata.");
                return; // Afslutter tidligt hvis ingen planter findes
            }

            // Henter alle Plant_IDs for at kunne vælge en tilfældig.
            var plantIds = await context.Plants.Select(p => p.Plant_ID).ToListAsync(stoppingToken);
            var randomPlantId = plantIds[_rnd.Next(plantIds.Count)]; // Vælger et tilfældigt ID fra listen

            // Opretter et nyt PlantLog objekt med tilfældige værdier for sensor-niveauer.
            var newLog = new PlantLog
            {
                Plant_ID = randomPlantId,
                Dato_Tid = DateTime.UtcNow,
                TemperatureLevel = Math.Round(_rnd.NextDouble() * 35 + 5, 1),
                LightLevel = Math.Round(_rnd.NextDouble() * 1000, 0),
                WaterLevel = Math.Round(_rnd.NextDouble() * 100, 0),
                AirHumidityLevel = Math.Round(_rnd.NextDouble() * 80 + 20, 0)
            };

            // Målingen gemmes i databasen
            await context.PlantLogs.AddAsync(newLog, stoppingToken);
            await context.SaveChangesAsync(stoppingToken);

            _logger.LogInformation("SensorBackgroundService: Logget sensordata for PlantID {PlantId}. Temp: {Temp}, Lys: {Lys}, Vand: {Vand}, Fugt: {Fugt}",
                randomPlantId, newLog.TemperatureLevel, newLog.LightLevel, newLog.WaterLevel, newLog.AirHumidityLevel);

            // Henter den plante og bruger som er forbundet til målingen
            var plant = await context.Plants.FirstOrDefaultAsync(p => p.Plant_ID == newLog.Plant_ID);

            User? user = null;
            if (plant != null)
            {
                user = await context.Users.FirstOrDefaultAsync(u => u.User_ID == plant.User_ID);
            }

            // Her kaldes NotificationService, som vurderer om sensorværdierne er uden for det normale, og opretter en notifikation i databasen hvis nødvendigt
            if (plant != null && user != null &&
                newLog.TemperatureLevel.HasValue &&
                newLog.WaterLevel.HasValue &&
                newLog.AirHumidityLevel.HasValue)
            {
                await notificationService.CheckAndSendNotificationAsync(
                    plant,
                    user,
                    newLog.TemperatureLevel.Value,
                    newLog.WaterLevel.Value,
                    newLog.AirHumidityLevel.Value
                );
            }
        }
    }
}
