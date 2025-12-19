using Convoy.Data.DbContexts;
using Convoy.Data.IRepositories;
using Convoy.Data.Repositories;
using Convoy.Service.Interfaces;
using Convoy.Service.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// PostgreSQL connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// EF Core DbContext (User va boshqa EF Core entity'lar uchun)
builder.Services.AddDbContext<AppDbConText>(options =>
    options.UseNpgsql(connectionString));

// Dapper uchun Npgsql connection (Singleton - Connection pooling PostgreSQL tomonidan boshqariladi)
builder.Services.AddSingleton(sp =>
{
    var conn = new NpgsqlConnection(connectionString);
    return conn;
});

// Repositories
builder.Services.AddScoped<ILocationRepository, LocationRepository>();

// Services
builder.Services.AddScoped<ILocationService, LocationService>();

// Background services (ordering matters - DatabaseInitializer birinchi)
builder.Services.AddHostedService<DatabaseInitializerService>();
builder.Services.AddHostedService<PartitionMaintenanceService>();

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Convoy GPS Tracking API",
        Version = "v1",
        Description = "GPS tracking system with PostgreSQL partitioned tables"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
