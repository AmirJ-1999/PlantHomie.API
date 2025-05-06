using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;

var builder = WebApplication.CreateBuilder(args);

// Tilføj Database Context
builder.Services.AddDbContext<PlantHomieContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Tilføj controllere
builder.Services.AddControllers();

// Tilføj CORS (tillad frontend at kalde API)
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

// Brug CORS-politik
app.UseCors("AllowAll");

// Brug HTTPS redirect og API-routing
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
