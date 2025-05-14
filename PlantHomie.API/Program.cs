using Microsoft.EntityFrameworkCore;
using PlantHomie.API.Data;
using PlantHomie.API.Services; // SensorBackgroundService
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);


// DATABASE

builder.Services.AddDbContext<PlantHomieContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));



// CONTROLLERS  +  BAGGRUNDSSERVICE

builder.Services.AddControllers();

// Tilføj baggrundsservice (fx dummy-sensordata)
builder.Services.AddHostedService<SensorBackgroundService>();



// SWAGGER

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();                 // ← nødvendig for Swagger-annotations og IFormFile
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
