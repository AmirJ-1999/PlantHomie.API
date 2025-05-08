using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Services; // <- Husk at du skal have din SensorBackgroundService her

var builder = WebApplication.CreateBuilder(args);

// Tilføj database context med connection string fra appsettings.json
builder.Services.AddDbContext<PlantHomieContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Tilføj controllere (API-ruter)
builder.Services.AddControllers();

// Tilføj baggrundsservice (kan fx sende dummy-sensor-data eller overvåge noget)
builder.Services.AddHostedService<SensorBackgroundService>();

// Tilføj Swagger (API-dokumentation og testværktøj)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Tilføj CORS-politik som tillader alle domæner (for Vue frontend adgang)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Swagger: vis kun i udviklingsmiljø
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PlantHomie API V1");
        c.RoutePrefix = ""; // Swagger starter direkte på localhost:5000/
    });
}

// Brug CORS-politikken (tillader at Vue frontend må kontakte backend)
app.UseCors("AllowAll");

// Brug HTTPS redirect
app.UseHttpsRedirection();

// Brug autorisation (kan aktiveres senere, fx hvis I har brugere)
app.UseAuthorization();

// Kortlæg controllere til endpoints
app.MapControllers();

// Start hele web-API'en
app.Run();
