# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Convoy GPS Tracking System** - Enterprise GPS tracking with PostgreSQL partitioned tables, built with .NET 8. This system uses a hybrid ORM approach: Dapper for partitioned tables (high performance) and EF Core for standard tables (convenience).

**Key Technologies**:
- **PostgreSQL Partitioning**: Monthly partitioned `locations` table by `recorded_at` (format: `locations_MM_YYYY`) for efficient queries
- **SignalR**: Real-time GPS location broadcasting to connected clients
- **JWT Authentication**: OTP-based authentication with external PHP API integration
- **Dual SMS Providers**: Failover SMS system (SmsFly → Sayqal)

## Build & Run Commands

### Local Development (Windows)

```bash
# Restore all packages
dotnet restore

# Build entire solution
dotnet build

# Build specific project
dotnet build Convoy.Api/Convoy.Api.csproj

# Run API (from API directory)
cd Convoy.Api
dotnet run

# Run API (from solution root)
dotnet run --project Convoy.Api

# Watch mode for development
dotnet watch run --project Convoy.Api
```

### Docker

```bash
# Start all services (PostgreSQL + API)
docker-compose up -d

# Rebuild and start
docker-compose up -d --build

# View logs
docker-compose logs -f
docker-compose logs -f api

# Stop services
docker-compose down

# Production deployment
docker-compose -f docker-compose.prod.yml up -d
```

### Database Setup

```bash
# Manual database setup (if not using Docker)
psql -U postgres -d convoy_db -f database-setup.sql

# Create partitions manually (Windows batch script)
cmd.exe /c create-partitions.bat

# Verify partitions exist
psql -U postgres -d convoy_db -c "SELECT tablename FROM pg_tables WHERE tablename LIKE 'locations_%' ORDER BY tablename;"
```

### Testing

```bash
# Run all tests (when tests are added)
dotnet test

# Run tests with detailed output
dotnet test --verbosity detailed

# Run specific test project
dotnet test Convoy.Tests/Convoy.Tests.csproj
```

## Architecture

### Solution Structure

```
Convoy/
├── Convoy.Domain/          # Entities only (no dependencies)
│   └── Entities/
│       ├── User.cs         # EF Core entity with Auditable base
│       ├── Location.cs     # Plain POCO for Dapper (no base class)
│       └── OtpCode.cs      # EF Core entity for OTP verification
│
├── Convoy.Data/            # Data access layer
│   ├── DbContexts/
│   │   └── AppDbContext.cs         # EF Core context (Users, OtpCodes)
│   ├── IRepositories/
│   │   └── ILocationRepository.cs  # Dapper repository interface
│   └── Repositories/
│       ├── Repository.cs           # Generic EF Core repository
│       └── LocationRepository.cs   # Dapper implementation
│
├── Convoy.Service/         # Business logic layer
│   ├── DTOs/
│   │   ├── LocationDtos.cs         # Request/Response DTOs
│   │   └── AuthDtos.cs             # Authentication DTOs
│   ├── Interfaces/
│   │   ├── ILocationService.cs
│   │   ├── IAuthService.cs
│   │   ├── IOtpService.cs
│   │   ├── ITokenService.cs
│   │   ├── ISmsService.cs
│   │   └── IPhpApiService.cs
│   └── Services/
│       ├── LocationService.cs              # Business logic + SignalR broadcast
│       ├── AuthService.cs                  # OTP authentication flow
│       ├── OtpService.cs                   # OTP generation/validation
│       ├── TokenService.cs                 # JWT token generation
│       ├── PhpApiService.cs                # External PHP API integration
│       ├── SmsProviders/
│       │   ├── CompositeSmsService.cs      # Failover SMS (SmsFly → Sayqal)
│       │   ├── SmsFlySender.cs             # Primary SMS provider
│       │   └── SayqalSender.cs             # Backup SMS provider
│       ├── PartitionMaintenanceService.cs  # IHostedService - auto partition creation
│       └── DatabaseInitializerService.cs   # IHostedService - database initialization
│
└── Convoy.Api/             # REST API layer
    ├── Controllers/
    │   ├── LocationController.cs   # Location CRUD endpoints
    │   ├── AuthController.cs       # Authentication endpoints
    │   └── SignalRTestController.cs # SignalR testing endpoints
    ├── Hubs/
    │   └── LocationHub.cs          # SignalR hub for real-time tracking
    ├── Program.cs                  # DI setup, JWT, SignalR, CORS
    └── appsettings.json            # Connection strings, JWT settings, SMS config
```

### Critical Architecture Decisions

**1. Hybrid ORM Strategy**
- **Dapper** for `locations` table: Partitioned tables require raw SQL for optimal performance and partition pruning
- **EF Core** for `users` table: Standard CRUD operations benefit from EF Core's convenience
- **Connection Management**: Single NpgsqlConnection registered as Singleton (PostgreSQL handles connection pooling natively)

**2. Partition Design Pattern**
- Table: `locations` partitioned by `recorded_at` (RANGE partitioning)
- Format: `locations_MM_YYYY` (e.g., `locations_12_2025`)
- Composite Primary Key: `(id, recorded_at)` - both columns required for unique constraint
- **Auto-creation**: `PartitionMaintenanceService` (IHostedService) creates partitions on startup for: previous month, current month, and next 3 months
- **Manual creation**: Call `create_location_partition(DATE)` PostgreSQL function

**3. Entity Design Differences**
- **User entity**: Inherits from `Auditable` base class (EF Core pattern)
- **Location entity**: Plain POCO with no inheritance (Dapper requires simple mapping)
- **Key difference**: Location does NOT use `Auditable` base class to avoid Dapper mapping issues

**4. Distance Calculation**
- Haversine formula implemented in C# (`LocationRepository.CalculateDistance`)
- Also available as PostgreSQL function `calculate_distance()` in database
- Distance stored in `distance_from_previous` column (nullable decimal, meters)
- Calculated on insert by comparing to user's last location

**5. Background Services Execution Order**
- `DatabaseInitializerService` registers FIRST (ensures database is ready)
- `PartitionMaintenanceService` registers SECOND (depends on database being initialized)
- Order matters in `Program.cs` - do not rearrange

**6. SignalR Real-Time Broadcasting**
- `LocationService` receives `IHubContext<LocationHub>` via DI (injected as `object?` to avoid circular dependencies)
- When location is created, automatically broadcasts to two SignalR groups:
  - `user_{userId}`: Specific user tracking group
  - `all_users`: Global tracking group
- Clients join groups via hub methods: `JoinUserTracking(userId)`, `JoinAllUsersTracking()`
- Event name: `LocationUpdated` with `LocationResponseDto` payload

**7. Authentication Flow (OTP + JWT)**
- **Step 1 - Verify Phone**: `POST /api/auth/verify-number` → Validates user exists in external PHP API and checks allowed position IDs
- **Step 2 - Send OTP**: `POST /api/auth/send-otp` → Generates 6-digit code, sends via SMS (SmsFly → Sayqal failover)
- **Step 3 - Verify OTP**: `POST /api/auth/verify-otp` → Validates code, returns JWT token
- **Step 4 - Use Token**: Include `Authorization: Bearer {token}` header in subsequent requests
- Worker data cached in-memory during auth flow (phone number → PhpWorkerDto)
- OTP codes stored in `otp_codes` table (EF Core), auto-expire after 5 minutes (configurable)

**8. SMS Provider Failover Strategy**
- `CompositeSmsService` implements `ISmsService` interface
- Primary: `SmsFlySender` (attempts first)
- Backup: `SayqalSender` (attempts if SmsFly fails)
- Both providers use HttpClient with configured base URLs and credentials from `appsettings.json`
- Logs provider failures for monitoring

## Database Schema

### Partitioned Table (Dapper)

```sql
-- Parent table (no data stored here)
locations (partitioned by RANGE on recorded_at)
├── id BIGSERIAL
├── user_id INTEGER (FK -> users.id)
├── recorded_at TIMESTAMPTZ (partition key)
├── latitude, longitude DECIMAL
├── accuracy, speed, heading, altitude
├── activity_type, activity_confidence
├── is_moving, battery_level, is_charging
├── distance_from_previous DECIMAL (calculated on insert)
└── created_at TIMESTAMPTZ
└── PRIMARY KEY (id, recorded_at)  -- Composite key required for partitioning

-- Child partitions (actual data storage)
locations_11_2025  -- November 2025
locations_12_2025  -- December 2025
locations_01_2026  -- January 2026
...
```

### Standard Tables (EF Core)

```sql
users
├── id SERIAL PRIMARY KEY
├── name VARCHAR(200)
├── phone VARCHAR(20)
├── is_active BOOLEAN
├── created_at, updated_at, delete_at

otp_codes
├── id SERIAL PRIMARY KEY
├── phone_number VARCHAR(20)
├── code VARCHAR(10)
├── created_at TIMESTAMPTZ
├── expires_at TIMESTAMPTZ
├── is_used BOOLEAN
└── IsValid (computed property: !is_used && expires_at > NOW())
```

## Development Guidelines

### When Modifying Location Table

1. **Never use EF Core migrations** for `locations` table - it's partitioned and managed by raw SQL
2. **Always include both `id` AND `recorded_at`** in WHERE clauses for single-record queries (enables partition pruning)
3. **Dapper mapping**: Use column aliases in SELECT (e.g., `user_id as UserId`) or configure column mappings
4. **New columns**: Add to `database-setup.sql` script, not EF migrations

### When Modifying Standard Tables (User, OtpCode)

1. **Use EF Core migrations** normally - standard tables without partitioning
2. Generate migration: `dotnet ef migrations add MigrationName --project Convoy.Data --startup-project Convoy.Api`
3. Apply migration: `dotnet ef database update --project Convoy.Data --startup-project Convoy.Api`
4. **OtpCode cleanup**: Use `OtpService.CleanupExpiredOtpsAsync()` to remove old codes (call periodically or via background job)

### Adding New Partitioned Tables

If you need another partitioned table:
1. Create parent table with `PARTITION BY RANGE (column_name)` in SQL script
2. Use Dapper repository pattern (see `LocationRepository` as template)
3. Create partition creation function in SQL
4. Add partition maintenance to `PartitionMaintenanceService` or create new IHostedService

### API Development

- **Swagger UI**: Available at `/swagger` endpoint (dev environment only)
  - JWT authentication configured in Swagger (use "Authorize" button)
  - SignalR endpoints documented via `SignalRTestController`
- **Controller pattern**: Follow `LocationController` for RESTful conventions
- **Authentication**: Use `[Authorize]` attribute for protected endpoints
  - Public endpoints: `/api/auth/*` (verify-number, send-otp, verify-otp)
  - Protected endpoints: All others require `Authorization: Bearer {token}` header
- **DTO mapping**: Keep in Service layer, not in Controllers
- **Logging**: Use ILogger injected into services (already configured)
- **Error handling**: Let exceptions bubble up for now (global exception handler not implemented)

### SignalR Development

- **Hub location**: `Convoy.Api/Hubs/LocationHub.cs`
- **Hub endpoint**: `/hubs/location` (configured in `Program.cs`)
- **CORS**: Currently set to `AllowAll` for development (restrict in production)
- **Broadcasting from services**: Inject `IHubContext<LocationHub>` as `object?` to avoid circular dependencies
- **Client methods** (callable from Flutter/JavaScript):
  - `JoinUserTracking(int userId)`: Subscribe to specific user's location updates
  - `LeaveUserTracking(int userId)`: Unsubscribe from user
  - `JoinAllUsersTracking()`: Subscribe to all users' location updates
  - `LeaveAllUsersTracking()`: Unsubscribe from all
- **Server events** (sent to clients):
  - `LocationUpdated`: Fired when new location created, payload is `LocationResponseDto`
  - `TestMessage`: Used by `SignalRTestController` for testing
- **Testing**: Use `SignalRTestController` endpoints to test broadcasting without creating real locations
  - `GET /api/signalrtest/health`: Check SignalR status
  - `POST /api/signalrtest/broadcast-test/{userId}`: Send test location to groups
  - See `SIGNALR-TESTING-GUIDE.md` and `FLUTTER-SIGNALR-EXAMPLE.md` for detailed examples

### Authentication & External API Integration

- **External PHP API**: System validates users against external PHP API (`IPhpApiService`)
  - Base URL configured in `appsettings.json` under `PhpApi:BaseUrl`
  - Endpoint: `/api/verify-user/{phoneNumber}`
  - Returns worker data: ID, name, position, branch info
- **Position-based access control**: Configure allowed position IDs in `Auth:AllowedPositionIds` (comma-separated)
  - Example: `"2,3,5"` allows only workers with these position IDs
  - Empty = all positions allowed
- **JWT Token claims**:
  - `nameid`: Worker ID
  - `unique_name`: Worker name
  - `mobilephone`: Phone number
  - `worker_guid`, `branch_guid`, `branch_name`, `position_id`: Worker metadata
- **Token configuration**: `Jwt:SecretKey`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationHours` in appsettings
- **OTP configuration**: `Auth:OtpLength` (default: 6), `Auth:OtpExpirationMinutes` (default: 5)

### SMS Provider Configuration

- **Composite pattern**: `CompositeSmsService` tries providers in order (failover)
- **Provider 1 - SmsFly**:
  - Config keys: `SmsFly:BaseUrl`, `SmsFly:AuthKey`, `SmsFly:Sender`
  - Used first, falls back to Sayqal if fails
- **Provider 2 - Sayqal**:
  - Config keys: `Sayqal:BaseUrl`, `Sayqal:Login`, `Sayqal:Password`
  - Backup provider
- **Development mode**: OTP codes logged to console with warning level (search logs for "DEVELOPMENT")

## Connection String Configuration

**Development** (`appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=convoy_db;Username=postgres;Password=YOUR_PASSWORD;Include Error Detail=true"
  }
}
```

**Docker** (docker-compose.yml):
- Database host: `postgres` (service name)
- Default password: `Danger124` (change for production)
- Environment variable: `ConnectionStrings__DefaultConnection`

## Common Patterns

### Testing SignalR Integration

```csharp
// In integration tests or manual testing
// 1. Connect Flutter/JS client to /hubs/location
// 2. Join tracking group
await hubConnection.invoke('JoinUserTracking', args: [123]);

// 3. Create location via API
POST /api/locations { userId: 123, latitude: 41.0, longitude: 69.0, ... }

// 4. Client receives LocationUpdated event automatically
hubConnection.on('LocationUpdated', (data) => {
  // data contains LocationResponseDto
  console.log(data);
});
```

### Implementing OTP Authentication Flow

```csharp
// Client-side flow
// Step 1: Verify phone exists
POST /api/auth/verify-number
{ "phoneNumber": "+998901234567" }
// Response: { success: true, data: { workerId, workerName, ... } }

// Step 2: Request OTP
POST /api/auth/send-otp
{ "phoneNumber": "+998901234567" }
// SMS sent via SmsFly or Sayqal

// Step 3: Verify OTP and get JWT
POST /api/auth/verify-otp
{ "phoneNumber": "+998901234567", "otpCode": "123456" }
// Response: { success: true, data: { token: "eyJhbGc..." } }

// Step 4: Use token
GET /api/locations/user/123
Headers: Authorization: Bearer eyJhbGc...
```

### Querying Partitioned Table with Dapper

```csharp
// GOOD - Includes partition key in WHERE clause
const string sql = @"
    SELECT * FROM locations
    WHERE user_id = @UserId
        AND recorded_at >= @StartDate
        AND recorded_at < @EndDate
    ORDER BY recorded_at DESC";

// BAD - Missing recorded_at filter (scans all partitions)
const string sql = @"
    SELECT * FROM locations
    WHERE user_id = @UserId
    ORDER BY recorded_at DESC";
```

### Adding New Service

1. Create interface in `Convoy.Service/Interfaces/`
2. Implement in `Convoy.Service/Services/`
3. Register in `Program.cs`: `builder.Services.AddScoped<IYourService, YourService>()`
4. **For services needing SignalR**: Inject `IHubContext<LocationHub>` as `object?` (cast to `dynamic` when using)

### Adding New SMS Provider

1. Create class in `Convoy.Service/Services/SmsProviders/` implementing `ISmsService`
2. Add configuration keys to `appsettings.json`
3. Register HttpClient: `builder.Services.AddHttpClient<YourSmsProvider>()`
4. Update `CompositeSmsService` to include new provider in fallback chain

### Creating New Background Service

```csharp
public class YourService : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Run on application startup
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Cleanup on shutdown
        return Task.CompletedTask;
    }
}

// Register AFTER DatabaseInitializerService if it depends on database
builder.Services.AddHostedService<YourService>();
```

## Troubleshooting

### "Partition does not exist" Error
- Check if `PartitionMaintenanceService` ran successfully (check logs)
- Manually create partition: `SELECT create_location_partition('2025-12-01'::DATE);`
- Verify: `SELECT tablename FROM pg_tables WHERE tablename LIKE 'locations_%';`

### Dapper Mapping Issues
- Ensure SQL column aliases match C# property names exactly (case-sensitive)
- Example: `user_id as UserId` in SQL maps to `UserId` property

### EF Core Migration Issues
- Ensure you're in solution root directory
- Specify both `--project` (where DbContext lives) and `--startup-project` (where config lives)
- Never run migrations against partitioned tables

### Docker Database Connection Issues
- Wait for postgres healthcheck: `docker-compose logs postgres`
- API depends on postgres service health (configured in docker-compose.yml)
- Connection string must use service name `postgres` as host

### SignalR Connection Failures
- **CORS issues**: Check `AllowAll` policy is configured in `Program.cs`
- **Client URL**: Use real IP address, not `localhost` (e.g., `http://192.168.1.100:5084/hubs/location`)
- **Hub not starting**: Check logs for SignalR initialization errors
- **Events not received**: Ensure client joined correct group (`JoinUserTracking` or `JoinAllUsersTracking`)
- **Testing**: Use `/api/signalrtest/health` endpoint to verify SignalR is active

### JWT Authentication Issues
- **401 Unauthorized**: Check token is included in `Authorization: Bearer {token}` header
- **Token expired**: Tokens expire after configured hours (`Jwt:ExpirationHours`)
- **Invalid token**: Ensure `Jwt:SecretKey` matches between token generation and validation
- **Missing claims**: Verify PHP API returns all required worker fields

### OTP/SMS Issues
- **OTP not received**: Check logs for SMS provider failures
  - SmsFly failed → Should automatically try Sayqal
  - Both failed → Check configuration keys and network connectivity
- **OTP expired**: Default 5 minutes, check `Auth:OtpExpirationMinutes` config
- **Wrong OTP**: OTP is one-time use, request new one if failed
- **Development testing**: OTP codes logged to console (search for "DEVELOPMENT" in logs)

### External PHP API Integration Issues
- **User not found**: Verify phone number exists in PHP API database
- **Position denied**: Check `Auth:AllowedPositionIds` configuration
- **API unreachable**: Verify `PhpApi:BaseUrl` and network connectivity
- **Timeout**: Increase HttpClient timeout in `PhpApiService` if needed

## Performance Considerations

- **Partition pruning**: Always include `recorded_at` in WHERE clause for best query performance
- **Batch inserts**: Use `InsertBatchAsync` for multiple locations (single SQL statement)
- **Indexes**: Already created on `(user_id, recorded_at)` for common query patterns
- **Connection pooling**: Handled by Npgsql/PostgreSQL automatically (min 0, max 100 by default)

## File Locations

- **SQL scripts**: Root directory (`database-setup.sql`, `create-partitions.sql`)
- **API examples**: `API-EXAMPLES.http` (REST Client format)
- **Deployment docs**: `DOCKER-DEPLOYMENT.md`, `QUICK-START.md`, `SETUP.md`
- **Batch scripts**: Windows: `*.bat`, Linux/Mac: `*.sh`
- **SignalR documentation**:
  - `SIGNALR-TESTING-GUIDE.md`: Complete testing guide for SignalR
  - `FLUTTER-SIGNALR-EXAMPLE.md`: Flutter client implementation examples
- **Configuration templates**: `appsettings.json`, `appsettings.Development.json`

## Critical Configuration Keys

### Required in appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=convoy_db;..."
  },
  "Jwt": {
    "SecretKey": "your-secret-key-min-32-chars",
    "Issuer": "ConvoyApi",
    "Audience": "ConvoyClients",
    "ExpirationHours": 720
  },
  "Auth": {
    "AllowedPositionIds": "2,3,5",  // Empty = all positions
    "OtpLength": 6,
    "OtpExpirationMinutes": 5
  },
  "PhpApi": {
    "BaseUrl": "https://your-php-api.com",
    "VerifyUserEndpoint": "/api/verify-user/{0}"
  },
  "SmsFly": {
    "BaseUrl": "https://smsfly.uz/api",
    "AuthKey": "your-auth-key",
    "Sender": "YourSender"
  },
  "Sayqal": {
    "BaseUrl": "https://sayqal.uz/api",
    "Login": "your-login",
    "Password": "your-password"
  }
}
```

### Environment-Specific Overrides

- **Development**: Use `appsettings.Development.json` for local settings
- **Docker**: Set via environment variables (double underscore notation):
  - `ConnectionStrings__DefaultConnection`
  - `Jwt__SecretKey`
  - `PhpApi__BaseUrl`
- **Production**: Use secrets management (Azure Key Vault, AWS Secrets Manager, etc.)
