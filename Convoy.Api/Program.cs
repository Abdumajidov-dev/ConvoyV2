using Convoy.Api.Middleware;
using Convoy.Data.DbContexts;
using Convoy.Data.IRepositories;
using Convoy.Data.Repositories;
using Convoy.Service.Interfaces;
using Convoy.Service.Services;
using Convoy.Service.Services.SmsProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

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
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// HttpClient for PhpApiService
builder.Services.AddHttpClient<IPhpApiService, PhpApiService>();

// SMS Providers
builder.Services.AddHttpClient<SmsFlySender>();
builder.Services.AddHttpClient<SayqalSender>();

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
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ISmsService, CompositeSmsService>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(Convoy.Service.Mapping.MappingProfile));

// Permission service
builder.Services.AddScoped<IPermissionService, PermissionService>();

// Role service
builder.Services.AddScoped<IRoleService, RoleService>();

// Background services (ordering matters - DatabaseInitializer birinchi)
builder.Services.AddHostedService<DatabaseInitializerService>();
builder.Services.AddHostedService<PartitionMaintenanceService>();
builder.Services.AddHostedService<PermissionSeedService>(); // Permission sistemasi seed

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false, // Token expiration check o'chirilgan - token abadiy amal qiladi
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };

    // Custom token extraction - "token" headeridan yoki "Authorization" headeridan
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            //// Birinchi "token" headerini tekshirish
            //if (context.Request.Headers.TryGetValue("token", out var tokenValue))
            //{
            //    context.Token = tokenValue;
            //}
            // Agar "token" header bo'lmasa, standart "Authorization: Bearer" ni ishlatish
             if (context.Request.Headers.ContainsKey("Authorization"))
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = authHeader.Substring("Bearer ".Length).Trim();
                }
            }

            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            // Token blacklist'da ekanligini tekshirish
            var tokenService = context.HttpContext.RequestServices.GetRequiredService<ITokenService>();
            var token = context.SecurityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;

            if (token != null)
            {
                var jti = token.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;

                if (!string.IsNullOrEmpty(jti))
                {
                    var isBlacklisted = await tokenService.IsTokenBlacklistedAsync(jti);
                    if (isBlacklisted)
                    {
                        context.Fail("Token has been revoked (logged out)");
                    }
                }
            }
        }
    };
});

// Authorization - Permission-based policies
builder.Services.AddAuthorization(options =>
{
    // Har bir permission uchun policy yaratish
    var allPermissions = Convoy.Domain.Constants.Permissions.GetAll();
    foreach (var (name, _, _, _, _) in allPermissions)
    {
        options.AddPolicy(name, policy =>
            policy.Requirements.Add(new Convoy.Api.Authorization.PermissionRequirement(name)));
    }
});

// Authorization handler
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Convoy.Api.Authorization.PermissionAuthorizationHandler>();

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
