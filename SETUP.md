# Convoy GPS Tracking System - Setup Guide

## NuGet Packages

### Convoy.Data Project
```bash
cd Convoy.Data
dotnet add package Dapper
dotnet add package Npgsql
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

### Convoy.Api Project
```bash
cd Convoy.Api
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Swashbuckle.AspNetCore
```

### Project References

```bash
# Convoy.Data -> Convoy.Domain
cd Convoy.Data
dotnet add reference ../Convoy.Domain/Convoy.Domain.csproj

# Convoy.Service -> Convoy.Data, Convoy.Domain
cd ../Convoy.Service
dotnet add reference ../Convoy.Data/Convoy.Data.csproj
dotnet add reference ../Convoy.Domain/Convoy.Domain.csproj

# Convoy.Api -> Convoy.Service, Convoy.Data, Convoy.Domain
cd ../Convoy.Api
dotnet add reference ../Convoy.Service/Convoy.Service.csproj
dotnet add reference ../Convoy.Data/Convoy.Data.csproj
dotnet add reference ../Convoy.Domain/Convoy.Domain.csproj
```

## PostgreSQL Setup

### 1. Database yaratish

```sql
CREATE DATABASE convoy_db;
```

### 2. Tables va Functions yaratish

```sql
-- Users table
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    phone VARCHAR(20),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    delete_at TIMESTAMPTZ
);

-- Locations partitioned table
CREATE TABLE locations (
    id BIGSERIAL,
    user_id INTEGER NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL,
    latitude DECIMAL(10, 8) NOT NULL,
    longitude DECIMAL(11, 8) NOT NULL,
    accuracy DECIMAL(6, 2),
    speed DECIMAL(6, 2),
    heading DECIMAL(5, 2),
    altitude DECIMAL(8, 2),
    activity_type VARCHAR(20),
    activity_confidence INTEGER,
    is_moving BOOLEAN DEFAULT false,
    battery_level INTEGER,
    is_charging BOOLEAN,
    distance_from_previous DECIMAL(10, 2),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    PRIMARY KEY (id, recorded_at),
    FOREIGN KEY (user_id) REFERENCES users(id)
) PARTITION BY RANGE (recorded_at);

-- Indexes
CREATE INDEX idx_locations_user_time ON locations (user_id, recorded_at DESC);
CREATE INDEX idx_locations_time ON locations (recorded_at DESC);

-- Partition creation function
CREATE OR REPLACE FUNCTION create_location_partition(target_month DATE)
RETURNS TEXT AS $$
DECLARE
    partition_name TEXT;
    start_date DATE;
    end_date DATE;
BEGIN
    partition_name := 'locations_' || TO_CHAR(target_month, 'MM_YYYY');
    start_date := DATE_TRUNC('month', target_month);
    end_date := start_date + INTERVAL '1 month';

    IF EXISTS (SELECT 1 FROM pg_tables WHERE tablename = partition_name) THEN
        RETURN 'Already exists: ' || partition_name;
    END IF;

    EXECUTE format(
        'CREATE TABLE %I PARTITION OF locations FOR VALUES FROM (%L) TO (%L)',
        partition_name, start_date, end_date
    );

    RETURN 'Created: ' || partition_name;
END;
$$ LANGUAGE plpgsql;
```

### 3. Test user yaratish

```sql
INSERT INTO users (name, phone, is_active)
VALUES ('Test User', '+998901234567', true);
```

## Configuration

### appsettings.json

Connection string'ni o'zgartiring:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=convoy_db;Username=postgres;Password=your_password;Include Error Detail=true"
  }
}
```

## Running the Application

```bash
cd Convoy.Api
dotnet build
dotnet run
```

Application ishga tushganda:
- PartitionMaintenanceService avtomatik ishga tushadi
- Kerakli partition'lar avtomatik yaratiladi (oldingi 1 oy, hozirgi oy, keyingi 3 oy)
- Swagger UI: https://localhost:5001/swagger

## API Endpoints

### 1. Create Location
```http
POST /api/location
Content-Type: application/json

{
  "userId": 1,
  "recordedAt": "2025-12-18T10:30:00Z",
  "latitude": 41.311151,
  "longitude": 69.279737,
  "accuracy": 10.5,
  "speed": 5.2,
  "heading": 180.0,
  "altitude": 420.0,
  "activityType": "walking",
  "activityConfidence": 85,
  "isMoving": true,
  "batteryLevel": 75,
  "isCharging": false
}
```

### 2. Create Batch Locations
```http
POST /api/location/batch
Content-Type: application/json

{
  "locations": [
    {
      "userId": 1,
      "recordedAt": "2025-12-18T10:00:00Z",
      "latitude": 41.311151,
      "longitude": 69.279737,
      "isMoving": true
    },
    {
      "userId": 1,
      "recordedAt": "2025-12-18T10:05:00Z",
      "latitude": 41.312000,
      "longitude": 69.280000,
      "isMoving": true
    }
  ]
}
```

### 3. Get User Locations
```http
GET /api/location/user/1?startDate=2025-12-01&endDate=2025-12-31
```

### 4. Get Last Locations
```http
GET /api/location/user/1/last?count=100
```

### 5. Get Daily Statistics
```http
GET /api/location/user/1/daily-statistics?startDate=2025-12-01&endDate=2025-12-31
```

## Architecture

### Layered Architecture
- **Convoy.Domain**: Entities (User, Location)
- **Convoy.Data**: Repositories, DbContext
  - EF Core: User va boshqa entity'lar
  - Dapper: Location (partitioned table)
- **Convoy.Service**: Business logic, DTOs
- **Convoy.Api**: Controllers, Endpoints

### Key Features
- ✅ PostgreSQL partitioned tables (oylik partition'lar)
- ✅ Dapper + EF Core hybrid approach
- ✅ Automatic partition creation (IHostedService)
- ✅ Haversine formula for distance calculation
- ✅ Daily statistics and summaries
- ✅ Batch insert support
- ✅ Production-ready logging
- ✅ Clean separation of concerns

### Performance Benefits
- **Partitioning**: Tez query'lar (faqat kerakli partition'larni scan qiladi)
- **Dapper**: Minimal overhead, raw SQL performance
- **Batch Insert**: Bir nechta location'larni bir vaqtda yozish
- **Indexing**: user_id va recorded_at bo'yicha tez qidiruv

## Monitoring

### Check Existing Partitions
```sql
SELECT tablename
FROM pg_tables
WHERE tablename LIKE 'locations_%'
ORDER BY tablename;
```

### Check Location Count per Partition
```sql
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size,
    (SELECT COUNT(*) FROM locations WHERE tableoid = (schemaname||'.'||tablename)::regclass) AS row_count
FROM pg_tables
WHERE tablename LIKE 'locations_%'
ORDER BY tablename;
```

## Troubleshooting

### Partition yaratilmasa
Logs'ni tekshiring:
```bash
dotnet run --project Convoy.Api
```

PostgreSQL function'ni to'g'ri yaratilganini tekshiring:
```sql
SELECT create_location_partition('2025-12-01'::DATE);
```

### Connection error
- PostgreSQL ishlab turganini tekshiring
- Connection string to'g'riligini tekshiring
- Firewall/Port access tekshiring

## Production Recommendations

1. **Connection Pooling**: PostgreSQL tomonidan avtomatik boshqariladi
2. **Logging**: Serilog yoki NLog qo'shing
3. **Authentication**: JWT authentication qo'shing
4. **Rate Limiting**: API endpoints uchun rate limiting
5. **Monitoring**: Prometheus + Grafana
6. **Backup**: Regular database backups
7. **Old Partitions**: Eski partition'larni archive/drop qiling

## License
MIT
