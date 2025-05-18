using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PlantHomie.API.Data;
using PlantHomie.API.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Tilføj tjenester til containeren.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Use camel case for property names to be more JavaScript friendly
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        
        // Handle reference loops in entity framework objects
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        
        // Use ISO 8601 date format for better frontend compatibility
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        
        // Make JSON serialization case-insensitive to handle PascalCase/camelCase inconsistency
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Tilføj swagger/OpenApi dokumentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(setup =>
{
    setup.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PlantHomie API",
        Version = "v1",
        Description = "API for PlantHomie, din digitale planteassistent!",
        Contact = new OpenApiContact
        {
            Name = "Plant Homie Team",
            Email = "planthomie@email.dk",
        }
    });

    // Tilføj JWT-authentication til Swagger UI
    setup.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authentication. Format: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    setup.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Tilføj database-kontekst
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PlantHomieContext>(options =>
    options.UseSqlServer(connectionString));

// Tilføj Authentication med JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secretKey = builder.Configuration["Jwt:Key"] ?? "PlantHomieDefaultSecretKey12345678";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

        // Customize the authentication challenge handler
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                // Override the default 401 response with a JSON response
                context.HandleResponse();
                
                // Add proper content type for JSON
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                
                var message = "Not authenticated. Please login first to obtain a valid JWT token and include it in the Authorization header.";
                var result = JsonSerializer.Serialize(new 
                { 
                    error = "Unauthorized", 
                    message = message,
                    status = 401 
                });
                
                await context.Response.WriteAsync(result);
            }
        };
    });

// Tilføj CORS-understøttelse
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Tilføj custom services
builder.Services.AddScoped<JwtService>();

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction()) // Aktiver Swagger i både DEV og PROD for demo-formål
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure static file serving for uploads
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Konfigurer statiske filer for at servere uploadede billeder
app.UseStaticFiles(); // Serverer filer fra wwwroot
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
}); // Serverer filer fra uploads-mappen

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Add a redirect from the root URL to Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapControllers();

// Tilføj global exception handling
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        // Log undtagelsen hvis logning er konfigureret
        var logger = context.RequestServices.GetService<ILogger<Program>>();
        logger?.LogError(ex, "Unhandled exception in request pipeline");
        
        // Returnér en passende JSON-fejlbesked
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";
        
        var response = new 
        { 
            error = "Internal Server Error", 
            message = "An unexpected error occurred. Please try again later.",
            status = 500 
        };
        
        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
});

app.Run();