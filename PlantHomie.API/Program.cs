using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PlantHomie.API.Data;
using PlantHomie.API.Services; // SensorBackgroundService, JwtService
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


// DATABASE

builder.Services.AddDbContext<PlantHomieContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));


// JWT AUTENTIFIKATION
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "PlantHomieDefaultSecretKey12345678"))
        };
    });

// Registrer JWT-tjeneste til tokengenerering
builder.Services.AddScoped<JwtService>();


// CONTROLLERS  +  BAGGRUNDSSERVICE

builder.Services.AddControllers();

// Tilføj baggrundsservice (fx dummy-sensordata)
builder.Services.AddHostedService<SensorBackgroundService>();


// SWAGGER

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();                 // ← nødvendig for Swagger-annotations og IFormFile
    
    // Tilføj JWT-autentifikation til Swagger UI
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
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
                }
            },
            new string[] {}
        }
    });
});

// Gør Swagger mindre følsom for FormFile-konflikter
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(o =>
{
    o.SuppressConsumesConstraintForFormFileParameters = true;
});



// CORS  – tillad ALLE domæner  (frontend kan hostes hvor som helst)

const string AllowAll = "AllowAll";

builder.Services.AddCors(opts =>
{
    opts.AddPolicy(AllowAll, p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});



var app = builder.Build();


app.UseHttpsRedirection();     // omdirigér http → https   // ***

app.UseCors(AllowAll);         // CORS SKAL ligge før Authorization

app.UseStaticFiles();          // giver adgang til fx /uploads/billede.jpg

app.UseAuthentication();       // Tilføjet for JWT - skal være før Authorization
app.UseAuthorization();

app.MapControllers();

/*  Swagger-UI skal være default startside – både lokalt og i Azure */
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PlantHomie API V1");
    c.RoutePrefix = "";        // ← gør Swagger til root (https://<site>/)
});



app.Run();
