# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Convoy GPS Tracking System** - Enterprise GPS tracking with PostgreSQL partitioned tables, built with .NET 8. This system uses a hybrid ORM approach: Dapper for partitioned tables (high performance) and EF Core for standard tables (convenience).

**Key Technology**: Monthly PostgreSQL table partitioning is the core architectural feature. The `locations` table is partitioned by `recorded_at` (format: `locations_MM_YYYY`), enabling efficient queries on large datasets.

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
│       └── Location.cs     # Plain POCO for Dapper (no base class)
│
├── Convoy.Data/            # Data access layer
│   ├── DbContexts/
│   │   └── AppDbContext.cs         # EF Core context (Users only)
│   ├── IRepositories/
│   │   └── ILocationRepository.cs  # Dapper repository interface
│   └── Repositories/
│       ├── Repository.cs           # Generic EF Core repository
│       └── LocationRepository.cs   # Dapper implementation
│
├── Convoy.Service/         # Business logic layer
│   ├── DTOs/
│   │   └── LocationDtos.cs         # Request/Response DTOs
│   ├── Interfaces/
│   │   └── ILocationService.cs
│   └── Services/
│       ├── LocationService.cs              # Business logic
│       ├── PartitionMaintenanceService.cs  # IHostedService - auto partition creation
│       └── DatabaseInitializerService.cs   # IHostedService - database initialization
│
└── Convoy.Api/             # REST API layer
    ├── Controllers/
    │   └── LocationController.cs
    ├── Program.cs          # DI setup, Dapper + EF Core configuration
    └── appsettings.json    # Connection string configuration
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

### Standard Table (EF Core)

```sql
users
├── id SERIAL PRIMARY KEY
├── name VARCHAR(200)
├── phone VARCHAR(20)
├── is_active BOOLEAN
├── created_at, updated_at, delete_at
```

## Development Guidelines

### When Modifying Location Table

1. **Never use EF Core migrations** for `locations` table - it's partitioned and managed by raw SQL
2. **Always include both `id` AND `recorded_at`** in WHERE clauses for single-record queries (enables partition pruning)
3. **Dapper mapping**: Use column aliases in SELECT (e.g., `user_id as UserId`) or configure column mappings
4. **New columns**: Add to `database-setup.sql` script, not EF migrations

### When Modifying User Table

1. **Use EF Core migrations** normally - standard table without partitioning
2. Generate migration: `dotnet ef migrations add MigrationName --project Convoy.Data --startup-project Convoy.Api`
3. Apply migration: `dotnet ef database update --project Convoy.Data --startup-project Convoy.Api`

### Adding New Partitioned Tables

If you need another partitioned table:
1. Create parent table with `PARTITION BY RANGE (column_name)` in SQL script
2. Use Dapper repository pattern (see `LocationRepository` as template)
3. Create partition creation function in SQL
4. Add partition maintenance to `PartitionMaintenanceService` or create new IHostedService

### API Development

- **Swagger UI**: Available at `/swagger` endpoint (dev environment only)
- **Controller pattern**: Follow `LocationController` for RESTful conventions
- **DTO mapping**: Keep in Service layer, not in Controllers
- **Logging**: Use ILogger injected into services (already configured)
- **Error handling**: Let exceptions bubble up for now (global exception handler not implemented)

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
