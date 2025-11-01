using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OilAndGasImport.Data;
using OilAndGasImport.Hubs;
using OilAndGasImport.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ✅ Swagger configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Oil & Gas Import API",
        Version = "v1",
        Description = "Demo API using Swashbuckle"
    });
});

// ✅ Database: SQL Server (Docker)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer("Server=localhost,1433;Database=AppDb;User Id=SA;Password=Pass@123;TrustServerCertificate=True;"));

// ✅ Custom services
builder.Services.AddScoped<CsvImportService>();

// ✅ SignalR
builder.Services.AddSignalR();

// ✅ CORS (Allow local web apps or HTML to connect)
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .SetIsOriginAllowed(_ => true); // ⚠️ Allow all origins (for dev)
        });
});

var app = builder.Build();

// ✅ Swagger always on
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Oil & Gas Import API v1");
    c.RoutePrefix = string.Empty; // open Swagger at root
});

app.UseHttpsRedirection();

// ✅ Add CORS before authorization
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthorization();

// ✅ Map controllers and hub
app.MapControllers();
app.MapHub<ImportHub>("/hubs/import");

app.Run();
