using Convoy.Api.Middleware;
using Convoy.Data.DbContexts;
using Convoy.Data.IRepositories;
using Convoy.Data.Repositories;
using Convoy.Service.Interfaces;
using Convoy.Service.Services;
using Convoy.Service.Services.SmsProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// PostgreSQL connection string - support both Railway DATABASE_URL and custom ConnectionStrings
// Priority: ConnectionStrings__DefaultConnection > DATABASE_URL > appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? builder.Configuration["DATABASE_URL"]
                       ?? throw new InvalidOperationException("Database connection string is not configured! Set either 'ConnectionStrings__DefaultConnection' or 'DATABASE_URL' environment variable.");

// DEBUG: Log connection string to verify it's loaded correctly
Console.WriteLine($"========================================");
Console.WriteLine($"ENVIRONMENT: {builder.Environment.EnvironmentName}");
Console.WriteLine($"CONNECTION STRING SOURCE: {(builder.Configuration.GetConnectionString("DefaultConnection") != null ? "ConnectionStrings:DefaultConnection" : builder.Configuration["DATABASE_URL"] != null ? "DATABASE_URL" : "NONE")}");
Console.WriteLine($"CONNECTION STRING LENGTH: {connectionString?.Length ?? 0}");

// Only show first 50 chars to avoid exposing password
var preview = string.IsNullOrEmpty(connectionString) ? "EMPTY OR NULL" : connectionString.Substring(0, Math.Min(50, connectionString.Length));
Console.WriteLine($"CONNECTION STRING (first 50 chars): {preview}...");

// Convert PostgreSQL URI format to Npgsql connection string if needed
// Railway provides: postgresql://user:pass@host:port/db
// Npgsql needs: Host=host;Port=port;Database=db;Username=user;Password=pass
if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
{
    try
    {
        var uri = new Uri(connectionString);
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : ""; // Unescape password

        connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Include Error Detail=true";
        Console.WriteLine($"✅ CONVERTED TO NPGSQL FORMAT");
        Console.WriteLine($"Host: {host}");
        Console.WriteLine($"Port: {port}");
        Console.WriteLine($"Database: {database}");
        Console.WriteLine($"Username: {username}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERROR CONVERTING URI: {ex.Message}");
        Console.WriteLine($"Connection string that failed: {preview}...");
        throw new InvalidOperationException($"Failed to parse PostgreSQL URI: {ex.Message}", ex);
    }
}

Console.WriteLine($"FINAL CONNECTION STRING LENGTH: {connectionString.Length}");
Console.WriteLine($"========================================");

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
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// HttpClient for PhpApiService
builder.Services.AddHttpClient<IPhpApiService, PhpApiService>();

// SMS Providers olib tashlandi - PHP API'da boshqariladi
// builder.Services.AddHttpClient<SmsFlySender>();
// builder.Services.AddHttpClient<SayqalSender>();

// Telegram Bot Service
builder.Services.AddHttpClient<ITelegramService, TelegramService>();

// Services
builder.Services.AddScoped<ILocationService>(sp =>
{
    var locationRepo = sp.GetRequiredService<ILocationRepository>();
    var mapper = sp.GetRequiredService<AutoMapper.IMapper>();
    var logger = sp.GetRequiredService<ILogger<LocationService>>();
    var hubContext = sp.GetService<IHubContext<Convoy.Api.Hubs.LocationHub>>();
    var telegramService = sp.GetService<ITelegramService>();
    return new LocationService(locationRepo, mapper, logger, hubContext, telegramService);
});
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPhpTokenService, PhpTokenService>(); // JWT token decode service
// OtpService va SmsService olib tashlandi - PHP API'da boshqariladi
// builder.Services.AddScoped<IOtpService, OtpService>();
// builder.Services.AddScoped<ISmsService, CompositeSmsService>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(Convoy.Service.Mapping.MappingProfile));

// Background services (ordering matters - DatabaseInitializer birinchi)
builder.Services.AddHostedService<DatabaseInitializerService>();
builder.Services.AddHostedService<PartitionMaintenanceService>();

// PHP Token Authorization (JWT authentication o'chirilgan - PHP token ishlatiladi)
// Custom authorization handler orqali PHP token'larni validate qilamiz
builder.Services.AddAuthentication("PhpTokenScheme")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, Convoy.Api.Authorization.PhpTokenAuthenticationHandler>("PhpTokenScheme", options => { });

// Authorization with custom policy
builder.Services.AddAuthorization(options =>
{
    // Default policy - barcha [Authorize] attribute'lar uchun
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes("PhpTokenScheme")
        .RequireAuthenticatedUser()
        .Build();
});

// SignalR
builder.Services.AddSignalR();

// CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Controllers with snake_case JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Response serialization: PascalCase -> snake_case
        options.JsonSerializerOptions.PropertyNamingPolicy = new Convoy.Api.Helpers.SnakeCaseNamingPolicy();
        options.JsonSerializerOptions.DictionaryKeyPolicy = new Convoy.Api.Helpers.SnakeCaseNamingPolicy();

        // Request deserialization: snake_case, camelCase, PascalCase -> C# property names (case-insensitive)
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Convoy GPS Tracking API",
        Version = "v1",
        Description = "GPS tracking system with PostgreSQL partitioned tables and JWT authentication"
    });

    // JWT Bearer authentication uchun Swagger configuration
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n" +
                      "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                      "Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
            new string[] { }
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable Swagger in all environments for Railway deployment
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Convoy API v1");
    c.RoutePrefix = "swagger"; // Serve Swagger UI at /swagger (Railway healthcheck compatible)
});

// Disable HTTPS redirection in production (Railway handles HTTPS)
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

// CORS middleware
app.UseCors("AllowAll");

// Token header logging middleware (for debugging Flutter requests)
app.UseTokenHeaderLogging();

// Encryption middleware (MUST run BEFORE routing to decrypt endpoint)
app.UseEncryption();
app.UseMiddleware<TelegramRequestLoggingMiddleware>();

// Explicit routing (allows encryption middleware to change path before routing)
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SignalR Hub endpoint
app.MapHub<Convoy.Api.Hubs.LocationHub>("/hubs/location");

// Health check endpoint for Railway and monitoring
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
})).AllowAnonymous();

// Root endpoint redirects to Swagger
app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();

app.Run();
