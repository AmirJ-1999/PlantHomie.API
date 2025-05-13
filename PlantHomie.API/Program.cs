using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Services; // SensorBackgroundService
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Tilføj database context med connection string fra appsettings.json
builder.Services.AddDbContext<PlantHomieContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Tilføj controllere (API-ruter)
builder.Services.AddControllers();

// Tilføj baggrundsservice (fx dummy-sensordata)
builder.Services.AddHostedService<SensorBackgroundService>();

// Tilføj Swagger (API-dokumentation)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations(); // ← nødvendig for Swagger @annotations og [FromForm]/IFormFile
});

// (Valgfrit men anbefalet) Gør Swagger mindre følsom for FormFile-konflikter
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.SuppressConsumesConstraintForFormFileParameters = true;
});

// Tilføj CORS-politik som tillader alle domæner (til Vue)
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

app.UseStaticFiles(); // giver adgang til fx /uploads/billede.jpg

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
