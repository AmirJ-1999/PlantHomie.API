using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlantHomie.API.Data;
using PlantHomie.API.Models;

namespace PlantHomie.API.Services
{
    /// <summary>
    /// En baggrundsservice (BackgroundService) der periodisk (hver 6. time) simulerer og gemmer sensordata for en tilfældig plante.
    /// Dette er nyttigt for at have data i systemet til test og demonstration, uden behov for rigtige sensorer.
    /// </summary>
    public class SensorBackgroundService : BackgroundService // Arver fra BackgroundService for korrekt håndtering af levetid og CancellationToken
    {
        private readonly IServiceProvider _services; // Bruges til at oprette et afhængigheds-scope for DbContext
        private readonly Random _rnd = new Random(); // Til generering af tilfældige sensordata
        private readonly ILogger<SensorBackgroundService> _logger; // Standard logningsinterface

        public SensorBackgroundService(IServiceProvider services, ILogger<SensorBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        // Hovedmetoden for BackgroundService. Kører i en løkke indtil CancellationToken anmoder om stop.
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SensorBackgroundService starter.");

            while (!stoppingToken.IsCancellationRequested) // Løkken fortsætter så længe applikationen kører
            {
                try
                {
                    _logger.LogInformation("SensorBackgroundService: Udfører AddSensorData.");
                    await AddSensorData(stoppingToken); // Kalder metoden der logger sensordata
                    
                    // Pauser i 6 timer før næste kørsel. CancellationToken gives videre for at kunne afbryde Task.Delay.
                    _logger.LogInformation("SensorBackgroundService: Venter 6 timer til næste logning.");
                    await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Denne undtagelse fanges, når stoppingToken.IsCancellationRequested bliver true (fx ved app shutdown).
                    // Det er en forventet situation, så vi logger det ikke som en fejl, men bryder løkken.
                    _logger.LogInformation("SensorBackgroundService stopper (OperationCanceledException).");
                    break;
                }
                catch (Exception ex) // Fanger alle andre uventede undtagelser
                {
                    _logger.LogError(ex, "SensorBackgroundService: Uventet fejl. Venter 5 minutter før genforsøg.");
                    
                    // Ved uventet fejl, venter servicen i 5 minutter før den prøver igen.
                    // Dette forhindrer at servicen spammer logs eller crasher appen ved vedvarende fejl.
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Hvis appen stopper mens vi venter efter en fejl, brydes løkken her også.
                        _logger.LogInformation("SensorBackgroundService stopper under retry delay (OperationCanceledException).");
                        break;
                    }
                }
            }
            _logger.LogInformation("SensorBackgroundService er stoppet.");
        }
        
        // Metode til at tilføje en ny PlantLog post med simuleret sensordata
        private async Task AddSensorData(CancellationToken stoppingToken)
        {
            // Opretter et nyt afhængigheds-scope for at hente en DbContext instans.
            // Dette er nødvendigt i en singleton-tjeneste (som BackgroundService er) for at bruge scoped-tjenester som DbContext.
                using var scope = _services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<PlantHomieContext>();

            // Tjekker først om der overhovedet er planter i databasen at logge data for.
            var anyPlants = await context.Plants.AnyAsync(stoppingToken);
            if (!anyPlants)
            {
                _logger.LogWarning("SensorBackgroundService: Ingen planter fundet i databasen. Kan ikke logge sensordata.");
                return; // Afslutter tidligt hvis ingen planter findes
            }
            
            // Henter alle Plant_IDs for at kunne vælge en tilfældig.
            // Overvej en mere effektiv måde at vælge et tilfældigt ID på for store databaser (fx SQL RAND()).
            var plantIds = await context.Plants.Select(p => p.Plant_ID).ToListAsync(stoppingToken);
            var randomPlantId = plantIds[_rnd.Next(plantIds.Count)]; // Vælger et tilfældigt ID fra listen

            // Opretter et nyt PlantLog objekt med tilfældige værdier for sensor-niveauer.
                var newLog = new PlantLog
                {
                Plant_ID = randomPlantId,
                Dato_Tid = DateTime.UtcNow, // Altid UTC tid for konsistens
                TemperatureLevel = Math.Round(_rnd.NextDouble() * 35 + 5, 1),  // Simulerer temp. mellem 5.0 - 40.0 °C, 1 decimal
                LightLevel = Math.Round(_rnd.NextDouble() * 1000, 0),        // Simulerer lys (lux) mellem 0 - 1000, 0 decimaler
                WaterLevel = Math.Round(_rnd.NextDouble() * 100, 0),        // Simulerer jordfugtighed 0 - 100 %, 0 decimaler
                AirHumidityLevel = Math.Round(_rnd.NextDouble() * 80 + 20, 0) // Simulerer luftfugtighed 20 - 100 % RH, 0 decimaler
                };

            await context.PlantLogs.AddAsync(newLog, stoppingToken); // Tilføjer den nye log til DbContext
            await context.SaveChangesAsync(stoppingToken); // Gemmer ændringen til databasen
            
            _logger.LogInformation("SensorBackgroundService: Logget sensordata for PlantID {PlantId}. Temp: {Temp}, Lys: {Lys}, Vand: {Vand}, Fugt: {Fugt}", 
                randomPlantId, newLog.TemperatureLevel, newLog.LightLevel, newLog.WaterLevel, newLog.AirHumidityLevel);
        }
    }
}
