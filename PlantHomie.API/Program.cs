using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PlantHomie.API.Data;
using PlantHomie.API.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Tilføj database context med connection string fra appsettings.json
builder.Services.AddDbContext<PlantHomieContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT AUTENTIFIKATION
// Sætter JWT Bearer som standard autentifikationsmekanisme
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => // Konfigurerer valideringsparametre for indkommende JWT tokens
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // Skal tokenets udsteder (issuer) valideres?
            ValidateAudience = true, // Skal tokenets modtager (audience) valideres?
            ValidateLifetime = true, // tjekker stadig udløbstid, men den er sat meget langt frem (20 år)
            ValidateIssuerSigningKey = true, // Skal signaturen på tokenet valideres?
            ValidIssuer = builder.Configuration["Jwt:Issuer"], // Forventet udsteder (hentes fra appsettings.json)
            ValidAudience = builder.Configuration["Jwt:Audience"], // Forventet modtager (hentes fra appsettings.json)
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "PlantHomieDefaultSecretKey12345678")), // Hemmelig nøgle til at validere signaturen (alternativ/nødløsning hvis ikke i config)
            // Tillader en meget stor tidsforskydning for at undgå problemer med token-udløb
            ClockSkew = TimeSpan.FromDays(365 * 20) // Meget høj tolerance for tidsforskydning (20 år)
        };
    });

// Registrerer JwtService, så den kan injiceres og bruges til at generere JWT tokens ved login
builder.Services.AddScoped<JwtService>();


// CONTROLLERS  +  BAGGRUNDSSERVICE

builder.Services.AddControllers(); // Tilføjer MVC controller services

// Tilføjer SensorBackgroundService som en hosted service, der kører i baggrunden
// (fx til periodisk at simulere og gemme sensordata)
builder.Services.AddHostedService<SensorBackgroundService>();

// SWAGGER

builder.Services.AddEndpointsApiExplorer(); // Nødvendig for at Swagger kan finde API endpoints
builder.Services.AddSwaggerGen(c => // Konfigurerer Swagger/OpenAPI dokumentation
{
    c.EnableAnnotations(); // Gør det muligt at bruge [SwaggerOperation] og andre annotations på controllers/actions
    
    // Definerer "Bearer" som en sikkerhedsmekanisme i Swagger UI for JWT token-baseret autentifikation
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

// Justerer ApiBehaviorOptions for at undgå konflikter med IFormFile parametre i Swagger UI
// (Swagger kan nogle gange have svært ved at udlede den korrekte Content-Type for IFormFile)
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(o =>
{
    o.SuppressConsumesConstraintForFormFileParameters = true;
});



// CORS  – Konfigurerer Cross-Origin Resource Sharing (CORS) politikker
// Her tillades forespørgsler fra alle origins, med alle headers og metoder.
// Vigtigt for udvikling, men bør strammes op for produktion.

const string AllowAll = "AllowAll"; // Navn på CORS politikken

builder.Services.AddCors(opts =>
{
    opts.AddPolicy(AllowAll, p => p
        .AllowAnyOrigin()       // Tillad alle domæner
        .AllowAnyHeader()       // Tillad alle HTTP headers
        .AllowAnyMethod());      // Tillad alle HTTP metoder (GET, POST, etc.)
});



var app = builder.Build(); // Bygger selve webapplikationen


app.UseHttpsRedirection();     // Middleware der automatisk omdirigerer HTTP-forespørgsler til HTTPS

app.UseCors(AllowAll);         // Anvender den definerede CORS politik. SKAL være før UseAuthentication/UseAuthorization.

app.UseStaticFiles();          // Gør det muligt at servere statiske filer fra wwwroot (fx billeder, css, js)

app.UseAuthentication();       // Aktiverer authentication middleware. Nødvendig for JWT. Skal være før UseAuthorization.
app.UseAuthorization();        // Aktiverer authorization middleware. Håndhæver [Authorize] attributter.

app.MapControllers(); // Mapper forespørgsler til controller actions baseret på routing konfiguration

// Konfigurerer Swagger middleware til at generere swagger.json og Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PlantHomie API V1");
    c.RoutePrefix = ""; // Viser Swagger direkte ved roden (fx: https://...azurewebsites.net/)
});

app.Run(); // Starter applikationen og lytter efter indkommende HTTP-forespørgsler
