# Convoy GPS Tracking - Quick Start Guide

## 🚀 Tez Boshlash (5 daqiqa)

### 1️⃣ NuGet Packages va Project References

**Windows:**
```bash
install-packages.bat
```

**Linux/Mac:**
```bash
chmod +x install-packages.sh
./install-packages.sh
```

Yoki manual:
```bash
# Convoy.Data
cd Convoy.Data
dotnet add package Dapper
dotnet add package Npgsql
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# Convoy.Api
cd ../Convoy.Api
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# Project references
cd ../Convoy.Data
dotnet add reference ../Convoy.Domain/Convoy.Domain.csproj

cd ../Convoy.Service
dotnet add reference ../Convoy.Data/Convoy.Data.csproj
dotnet add reference ../Convoy.Domain/Convoy.Domain.csproj

cd ../Convoy.Api
dotnet add reference ../Convoy.Service/Convoy.Service.csproj
dotnet add reference ../Convoy.Data/Convoy.Data.csproj
dotnet add reference ../Convoy.Domain/Convoy.Domain.csproj
```

### 2️⃣ PostgreSQL Database Setup

```bash
# PostgreSQL'ga ulanish
psql -U postgres

# Database yaratish
CREATE DATABASE convoy_db;
\c convoy_db

# SQL script ishga tushirish
\i database-setup.sql

# Yoki:
psql -U postgres -d convoy_db -f database-setup.sql
```

### 3️⃣ Connection String

`Convoy.Api/appsettings.json` faylini tahrirlang:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=convoy_db;Username=postgres;Password=YOUR_PASSWORD;Include Error Detail=true"
  }
}
```

### 4️⃣ Run Application

```bash
cd Convoy.Api
dotnet run
```

Application ochiladi:
- **API**: https://localhost:5001
- **Swagger**: https://localhost:5001/swagger

---

## 📂 Yaratilgan Fayllar

### Domain Layer
- ✅ `Convoy.Domain/Entities/User.cs` - EF Core entity
- ✅ `Convoy.Domain/Entities/Location.cs` - Dapper entity (no Auditable)

### Data Layer
- ✅ `Convoy.Data/IRepositories/ILocationRepository.cs` - Dapper repository interface
- ✅ `Convoy.Data/Repositories/LocationRepository.cs` - Dapper implementation
- ✅ `Convoy.Data/DbContexts/AppDbContext.cs` - Updated with User DbSet

### Service Layer
- ✅ `Convoy.Service/DTOs/LocationDtos.cs` - Request/Response DTOs
- ✅ `Convoy.Service/Interfaces/ILocationService.cs` - Service interface
- ✅ `Convoy.Service/Services/LocationService.cs` - Business logic
- ✅ `Convoy.Service/Services/PartitionMaintenanceService.cs` - IHostedService (auto partition creation)

### API Layer
- ✅ `Convoy.Api/Controllers/LocationController.cs` - REST endpoints

### Configuration
- ✅ `Convoy.Api/Program.cs` - DI, Dapper + EF Core setup
- ✅ `Convoy.Api/appsettings.json` - Connection string

### Documentation & Scripts
- ✅ `SETUP.md` - To'liq setup guide
- ✅ `QUICK-START.md` - Tez boshlash
- ✅ `database-setup.sql` - PostgreSQL schema script
- ✅ `API-EXAMPLES.http` - Sample API requests
- ✅ `install-packages.bat` - Windows installation
- ✅ `install-packages.sh` - Linux/Mac installation

---

## 🧪 Test Qilish

### 1. Health Check - Partition'lar yaratildimi?

Application ishga tushganda logs'da ko'ring:
```
PartitionMaintenanceService starting...
Found X existing partitions
Partition creation result: Created: locations_12_2025
```

### 2. PostgreSQL'da tekshirish

```sql
-- Partition'lar
SELECT tablename FROM pg_tables WHERE tablename LIKE 'locations_%' ORDER BY tablename;

-- Expected output:
-- locations_11_2025
-- locations_12_2025
-- locations_01_2026
-- locations_02_2026
-- locations_03_2026
```

### 3. API Test (cURL)

```bash
# Create location
curl -X POST https://localhost:5001/api/location \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 1,
    "recordedAt": "2025-12-18T10:00:00Z",
    "latitude": 41.311151,
    "longitude": 69.279737,
    "isMoving": true
  }'

# Get last locations
curl https://localhost:5001/api/location/user/1/last?count=10
```

### 4. Swagger UI

Browser'da: https://localhost:5001/swagger

---

## 🏗️ Arxitektura

### Clean Architecture Pattern
```
┌─────────────────────────────────────┐
│         Convoy.Api                  │
│  (Controllers, Program.cs)          │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│       Convoy.Service                │
│  (Business Logic, DTOs)             │
│  - LocationService                  │
│  - PartitionMaintenanceService      │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│        Convoy.Data                  │
│  - LocationRepository (Dapper)      │
│  - AppDbContext (EF Core)           │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│       Convoy.Domain                 │
│  (Entities: User, Location)         │
└─────────────────────────────────────┘
```

### Dapper vs EF Core

| Entity    | ORM       | Sabab                              |
|-----------|-----------|------------------------------------|
| Location  | Dapper    | Partitioned table, Raw SQL needed  |
| User      | EF Core   | Standard CRUD operations           |

---

## 🔑 Asosiy Xususiyatlar

- ✅ **PostgreSQL Partitioning** - Oylik partition'lar (locations_12_2025, locations_01_2026)
- ✅ **Hybrid ORM** - Dapper (Location) + EF Core (User)
- ✅ **Auto Partition Creation** - IHostedService orqali startup'da
- ✅ **Distance Calculation** - Haversine formula
- ✅ **Daily Statistics** - Kunlik masofa va location count
- ✅ **Batch Insert** - Bir nechta location'larni bir vaqtda
- ✅ **Production Logging** - ILogger integration
- ✅ **Clean Code** - SOLID, separation of concerns

---

## 📊 API Endpoints

| Method | Endpoint                                    | Tavsif                    |
|--------|---------------------------------------------|---------------------------|
| POST   | `/api/location`                             | Yangi location yaratish   |
| POST   | `/api/location/batch`                       | Batch locations yaratish  |
| GET    | `/api/location/user/{userId}`               | User locations olish      |
| GET    | `/api/location/user/{userId}/last`          | Oxirgi N ta location      |
| GET    | `/api/location/user/{userId}/daily-statistics` | Kunlik statistikalar   |
| GET    | `/api/location/{id}`                        | ID orqali location        |

---

## 🐛 Muammolarni Hal Qilish

### Connection error
```bash
# PostgreSQL ishlab turganini tekshiring
sudo systemctl status postgresql
# yoki
pg_isready -U postgres
```

### Partition yaratilmagan
```sql
-- Manual partition yaratish
SELECT create_location_partition('2025-12-01'::DATE);
```

### Logs ko'rish
```bash
cd Convoy.Api
dotnet run --verbosity detailed
```

---

## 📚 Keyingi Qadamlar

1. **Authentication** - JWT authentication qo'shing
2. **Rate Limiting** - API endpoint'lar uchun
3. **Caching** - Redis yoki in-memory cache
4. **Real-time** - SignalR yoki WebSockets
5. **Monitoring** - Prometheus + Grafana
6. **Background Jobs** - Hangfire (eski partition'larni o'chirish)

---

## 📞 Yordam

- `SETUP.md` - To'liq setup guide
- `API-EXAMPLES.http` - API examples
- `database-setup.sql` - Database schema

---

**Author**: GPS Tracking System
**Tech Stack**: .NET 8, PostgreSQL 12+, Dapper, EF Core
**License**: MIT
