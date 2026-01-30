# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Convoy GPS Tracking System** - Enterprise GPS tracking with PostgreSQL partitioned tables, built with .NET 8. This system uses a hybrid ORM approach: Dapper for partitioned tables (high performance) and EF Core for standard tables (convenience).

**Key Technologies**:
- **PostgreSQL Partitioning**: Monthly partitioned `locations` table by `recorded_at` (format: `locations_MM_YYYY`) for efficient queries
- **SignalR**: Real-time GPS location broadcasting to connected clients
- **JWT Authentication**: OTP-based authentication with external PHP API integration
- **Permission System**: Role-Based Access Control (RBAC) with granular permissions
- **Dual SMS Providers**: Failover SMS system (SmsFly → Sayqal)
- **Flutter Background Geolocation**: Full integration with flutter_background_geolocation library (extended coords, metadata, events)
- **Telegram Bot Integration**: Channel notifications for locations, alerts, and custom reports
- **snake_case JSON**: ALL API endpoints and JSON fields use snake_case naming convention

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

# Python integration tests (manual API testing)
python test_encryption.py          # Test encryption endpoints
python test_permissions_me.py      # Test /api/auth/me with permissions
python test_verify_number.py       # Test phone verification flow
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

**7. Authentication Flow (OTP + JWT + Logout)**
- **Step 1 - Verify Phone**: `POST /api/auth/verify_number` → Validates user exists in external PHP API and checks allowed position IDs
- **Step 2 - Send OTP**: `POST /api/auth/send_otp` → Generates OTP code, sends via SMS (SmsFly → Sayqal failover)
- **Step 3 - Verify OTP**: `POST /api/auth/verify_otp` → Validates code, returns JWT token
- **Step 4 - Use Token**: Include `Authorization: Bearer {token}` header in subsequent requests
- **Step 5 - Get User Info**: `GET /api/auth/me` → Returns current user info from JWT token (requires authentication)
- **Step 6 - Logout**: `POST /api/auth/logout` → Blacklists current token to prevent reuse (requires authentication)
- Worker data cached in-memory during auth flow (phone number → PhpWorkerDto)
- OTP codes stored in `otp_codes` table (EF Core), auto-expire after configured minutes (default: 1 minute)
- Token blacklisting prevents logout'ed tokens from being reused (stored in `token_blacklist` table)

**8. SMS Provider Failover Strategy**
- `CompositeSmsService` implements `ISmsService` interface
- Primary: `SmsFlySender` (attempts first)
- Backup: `SayqalSender` (attempts if SmsFly fails)
- Both providers use HttpClient with configured base URLs and credentials from `appsettings.json`
- Logs provider failures for monitoring

**9. API Response Pattern (ServiceResult + ApiResponse)**
- **Service Layer**: Returns `ServiceResult<T>` or custom result wrappers with `Status`, `Message`, `Data` properties
- **Controller Layer**: Converts service results to standardized JSON format:
  ```json
  {
    "status": true/false,
    "message": "User-friendly message",
    "data": { ... } or null
  }
  ```
- **LocationController**: Uses `ServiceResult<T>` pattern (see `SERVICE_RESULT_PATTERN.md`)
- **AuthController**: Uses custom response format with `Status`, `Message`, `Data` properties
- **Exception handling**: Services handle business logic errors, controllers handle HTTP status codes
- Controllers should map service responses to consistent HTTP status codes (200 OK, 400 Bad Request, 500 Internal Server Error)

**10. End-to-End Encryption (AES-256-CBC)**
- **Purpose**: Encrypt request/response data to prevent man-in-the-middle attacks and data interception
- **Algorithm**: AES-256-CBC with PKCS7 padding
- **Components**:
  - `EncryptionService`: Core AES encryption/decryption service
  - `EncryptionMiddleware`: Automatic request/response encryption middleware
- **Configuration**: `Encryption:Enabled`, `Encryption:Key`, `Encryption:ExcludedRoutes` in appsettings
- **Excluded Routes**: Ba'zi route'lar encryption'dan exclude qilinishi mumkin (e.g., `/api/locations`, `/swagger`)
  - Configure via `Encryption:ExcludedRoutes` array in appsettings.json
  - Wildcard support: `/api/locations/*` matches all routes starting with `/api/locations/`
  - See `ENCRYPTION_EXCLUDED_ROUTES_GUIDE.md` for details
- **Request Format**: Raw Base64 encrypted string (e.g., `"t5oLfaFS3jSqrDuQB+eTIRI..."`)
- **Response Format**: Raw Base64 encrypted string (e.g., `"muMA0bv2XvoawCPU1xd7c9J9..."`)
- **Content-Type**: `text/plain` for encrypted requests/responses
- **Key Generation**: Use `generate-encryption-keys.ps1` (Windows) or `generate-encryption-keys.sh` (Linux/Mac)
- **Flutter Integration**: Use `encrypt` package with same Key/IV as backend
- **Development Mode**: Set `Encryption:Enabled: false` to disable encryption for testing
- **Security**: NEVER commit encryption keys to Git, use environment variables in production
- **Note**: No wrapper object - direct encrypted string for maximum security

**11. Telegram Bot Integration**
- **Purpose**: Telegram kanaliga real-time notification yuborish (location create, alerts, reports)
- **Service**: `TelegramService` - reusable service for any notification
- **Configuration**: `BotSettings:Telegram:BotToken`, `BotSettings:Telegram:ChannelId` in appsettings
- **Features**:
  - Oddiy text xabarlar
  - Formatted xabarlar (HTML/Markdown)
  - Location ma'lumotlari (Google Maps link bilan)
  - Bulk location reports
  - Custom data reports
  - Alert/Warning xabarlari (ERROR, WARNING, INFO, SUCCESS)
- **Auto-Integration**: LocationService'da location create bo'lganda avtomatik Telegram'ga yuboriladi
- **Non-Blocking**: Telegram failure main operation'ni to'xtatmaydi
- **Test Endpoints**: `/api/telegram-test/*` - service'ni test qilish uchun
- See `TELEGRAM_SERVICE_GUIDE.md` for complete documentation

**12. snake_case JSON Naming Convention**
- **CRITICAL**: ALL API endpoints use snake_case: `/api/auth/verify_number`, `/api/locations/user_batch`
- **CRITICAL**: ALL JSON fields use snake_case: `user_id`, `recorded_at`, `phone_number`, `activity_type`
- **CRITICAL**: ALL query parameters use snake_case: `?start_date=...&end_date=...`
- Configured via `JsonNamingPolicy.SnakeCaseLower` in `Program.cs` AddControllers() options
- Dart/Flutter clients can use property names directly without manual mapping
- See `SNAKE_CASE_API_GUIDE.md` for complete API contract

**13. Permission System (RBAC)**
- **Purpose**: Role-Based Access Control for fine-grained authorization
- **Components**:
  - `Permission` entity: Defines actions (e.g., `users.view`, `locations.create`)
  - `Role` entity: Groups permissions (e.g., SuperAdmin, Admin, Manager, Driver, Viewer)
  - `UserRole`: Many-to-many relationship between users and roles
  - `RolePermission`: Many-to-many relationship between roles and permissions
- **Usage**: Controllers use `[HasPermission("resource.action")]` attribute
- **Auto-seeding**: `PermissionSeedService` creates default roles and permissions on startup
- **Permission format**: `<resource>.<action>` (e.g., `users.view`, `locations.create`)
- **Default roles**: 5 pre-configured roles with 28 permissions
- **JWT integration**: User permissions can be checked via `IPermissionService.UserHasPermissionAsync()`
- See `PERMISSION_SYSTEM_GUIDE.md` for complete documentation

**14. SignalR User Active Status Tracking**
- **Purpose**: Automatically mark users as active/inactive based on SignalR connection status
- **Hub Methods**:
  - `RegisterUser(userId)`: Client MUST call after connecting to mark user as ACTIVE
  - `UnregisterUser(userId)`: Client calls to manually mark user as INACTIVE and disconnect
- **Automatic Behavior**:
  - `OnConnectedAsync`: Logs connection (does NOT mark user active automatically)
  - `OnDisconnectedAsync`: Automatically marks user as INACTIVE when connection drops
- **Connection Mapping**: In-memory `ConcurrentDictionary<string, int>` stores `ConnectionId -> UserId`
- **User Lifecycle**:
  1. Flutter connects → SignalR connection established
  2. Flutter calls `RegisterUser(userId)` → User marked ACTIVE in database
  3. Flutter disconnects (app closed/network issue) → User marked INACTIVE automatically
  4. Flutter calls `UnregisterUser(userId)` → User marked INACTIVE + connection closed by server
- **CRITICAL**: Flutter client MUST call `RegisterUser(userId)` after connection, otherwise disconnect won't mark user inactive (no mapping exists)
- **App Lifecycle Handling**: Disconnect on app pause, reconnect + RegisterUser on app resume
- **Multiple Devices**: Same user can connect from multiple devices, each tracked separately
- See `SIGNALR_USER_ACTIVE_STATUS.md` and `SIGNALR_USER_STATUS_CHANGE.md` for complete documentation

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

roles (Permission System)
├── id BIGSERIAL PRIMARY KEY
├── name VARCHAR(100) UNIQUE (e.g., "SuperAdmin", "Driver")
├── display_name VARCHAR(200)
├── description VARCHAR(500)
├── is_active BOOLEAN
└── created_at, updated_at, delete_at

permissions (Permission System)
├── id BIGSERIAL PRIMARY KEY
├── name VARCHAR(100) UNIQUE (e.g., "users.view", "locations.create")
├── display_name VARCHAR(200)
├── resource VARCHAR(50) (e.g., "users", "locations")
├── action VARCHAR(50) (e.g., "view", "create")
├── description VARCHAR(500)
├── is_active BOOLEAN
└── created_at, updated_at, delete_at

user_roles (Junction Table)
├── id BIGSERIAL PRIMARY KEY
├── user_id BIGINT FK -> users.id
├── role_id BIGINT FK -> roles.id
├── assigned_at TIMESTAMPTZ
├── assigned_by BIGINT (who assigned this role)
├── created_at, updated_at, delete_at
└── UNIQUE(user_id, role_id)

role_permissions (Junction Table)
├── id BIGSERIAL PRIMARY KEY
├── role_id BIGINT FK -> roles.id
├── permission_id BIGINT FK -> permissions.id
├── granted_at TIMESTAMPTZ
├── granted_by BIGINT (who granted this permission)
├── created_at, updated_at, delete_at
└── UNIQUE(role_id, permission_id)

token_blacklist (Logout/Security)
├── id BIGSERIAL PRIMARY KEY
├── token_hash VARCHAR(500) UNIQUE
├── user_id BIGINT FK -> users.id
├── blacklisted_at TIMESTAMPTZ
├── reason VARCHAR(200) (e.g., "logout", "security")
└── expires_at TIMESTAMPTZ
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
- **Controller pattern**: Follow these strict rules:
  1. Controllers receive `ServiceResult<T>` from services
  2. Map to standardized JSON: `{ status: bool, message: string, data: T }`
  3. Return `StatusCode(result.StatusCode, responseObject)`
  4. DO NOT handle exceptions - services handle them
  5. Example pattern in `LocationController` and `AuthController`
- **Authentication**: Use `[Authorize]` attribute for protected endpoints
  - Public endpoints: `/api/auth/verify_number`, `/api/auth/send_otp`, `/api/auth/verify_otp`
  - Protected endpoints: `/api/auth/me` and all location endpoints require `Authorization: Bearer {token}` header
- **DTO mapping**: Keep in Service layer, not in Controllers
- **Logging**: Use ILogger injected into services (already configured)
- **JSON naming**: MUST use snake_case for all endpoints, query params, and JSON fields
  - Routes: `[HttpPost("verify_number")]` NOT `[HttpPost("verifyNumber")]`
  - DTOs: Use `[JsonProperty("phone_number")]` attribute for snake_case serialization

### SignalR Development

- **Hub location**: `Convoy.Api/Hubs/LocationHub.cs`
- **Hub endpoint**: `/hubs/location` (configured in `Program.cs`)
- **CORS**: Currently set to `AllowAll` for development (restrict in production)
- **Broadcasting from services**: Inject `IHubContext<LocationHub>` as `object?` to avoid circular dependencies
- **Client methods** (callable from Flutter/JavaScript):
  - `RegisterUser(int userId)`: **REQUIRED** - Mark user as ACTIVE (call after connection)
  - `UnregisterUser(int userId)`: Mark user as INACTIVE and disconnect
  - `JoinUserTracking(int userId)`: Subscribe to specific user's location updates
  - `LeaveUserTracking(int userId)`: Unsubscribe from user
  - `JoinAllUsersTracking()`: Subscribe to all users' location updates
  - `LeaveAllUsersTracking()`: Unsubscribe from all
- **Server events** (sent to clients):
  - `LocationUpdated`: Fired when new location created, payload is `LocationResponseDto`
  - `UserRegistered`: Confirmation of successful user registration with `{user_id, is_active, message}`
  - `UserUnregistered`: Confirmation of successful user unregistration with `{user_id, is_active, message}`
  - `UserRegistrationFailed`: Registration failed with `{user_id, error}`
  - `UserUnregistrationFailed`: Unregistration failed with `{user_id, error}`
  - `TestMessage`: Used by `SignalRTestController` for testing
- **User Status Tracking**:
  - Users marked ACTIVE when `RegisterUser(userId)` is called
  - Users marked INACTIVE when connection drops (automatic) or `UnregisterUser(userId)` is called (manual)
  - Connection-to-User mapping stored in-memory using `ConcurrentDictionary<string, int>`
  - **CRITICAL**: Flutter MUST call `RegisterUser(userId)` after connecting, otherwise disconnect won't update user status
- **Testing**: Use `SignalRTestController` endpoints to test broadcasting without creating real locations
  - `GET /api/signalrtest/health`: Check SignalR status
  - `POST /api/signalrtest/broadcast-test/{userId}`: Send test location to groups
  - See `SIGNALR-TESTING-GUIDE.md`, `FLUTTER-SIGNALR-EXAMPLE.md`, `SIGNALR_USER_ACTIVE_STATUS.md`, and `SIGNALR_USER_STATUS_CHANGE.md` for detailed examples

### Authentication & External API Integration

- **External PHP API**: System validates users against external PHP API (`IPhpApiService`)
  - Base URL configured in `appsettings.json` under `PhpApi:GlobalPathForSupport`
  - Endpoint: `POST /auth-service/verification-user` (with phone_number in request body)
  - Uses Basic Authentication (configured via `PhpApi:Username` and `PhpApi:Password`)
  - Returns worker data: ID, name, position, branch info
- **Position-based access control**: Configure allowed position IDs in `Auth:AllowedPositionIds` (comma-separated)
  - Example: `"2,3,5"` allows only workers with these position IDs
  - Empty = all positions allowed
- **JWT Token claims**:
  - `user_id`: Worker ID (primary identifier)
  - `unique_name`: Worker name
  - `mobilephone`: Phone number
  - `worker_guid`, `branch_guid`, `branch_name`, `position_id`: Worker metadata
- **Token configuration**: `Jwt:SecretKey`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationHours` in appsettings
- **OTP configuration**:
  - `Auth:OtpLength` (default: 4) - OTP kod uzunligi
  - `Auth:OtpExpirationMinutes` (default: 1) - OTP kodning amal qilish muddati (daqiqalarda)
  - `Auth:OtpRateLimitSeconds` (default: 60) - Bir telefon raqam uchun OTP jo'natish oralig'i (soniyalarda, 0 = o'chirish)

### SMS Provider Configuration

- **Composite pattern**: `CompositeSmsService` tries providers in order (failover)
- **Provider 1 - SmsFly**:
  - Config path: `SmsProviders:SmsFly:ApiKey`, `SmsProviders:SmsFly:ApiUrl`
  - Used first, falls back to Sayqal if fails
- **Provider 2 - Sayqal**:
  - Config path: `SmsProviders:Sayqal:UserName`, `SmsProviders:Sayqal:SecretKey`, `SmsProviders:Sayqal:ApiUrl`
  - Backup provider
- **Development mode**: OTP codes logged to console with warning level (search logs for "DEVELOPMENT")
- **HttpClient registration**: Both providers use `AddHttpClient<T>()` in DI container

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

## OTP Rate Limiting Quick Reference

### Development/Testing (No Rate Limit)
```json
// appsettings.Development.json
{
  "Auth": {
    "OtpRateLimitSeconds": 0  // Disable rate limiting
  }
}
```

### Production (Recommended Settings)
```json
// appsettings.json
{
  "Auth": {
    "OtpRateLimitSeconds": 60  // 1 minute between requests
  }
}
```

### Custom Settings
```json
{
  "Auth": {
    "OtpRateLimitSeconds": 30   // 30 seconds
    // or
    "OtpRateLimitSeconds": 120  // 2 minutes
  }
}
```

**Note**: Setting to `0` completely disables rate limiting. Use only for temporary development/testing purposes.

---

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
POST /api/auth/verify_number
{ "phone_number": "+998901234567" }
// Response: { status: true, message: "...", data: { worker_id, worker_name, ... } }

// Step 2: Request OTP
POST /api/auth/send_otp
{ "phone_number": "+998901234567" }
// SMS sent via SmsFly or Sayqal
// Response: { status: true, message: "OTP sent", data: null }

// Step 3: Verify OTP and get JWT
POST /api/auth/verify_otp
{ "phone_number": "+998901234567", "otp_code": "1234" }
// Response: { status: true, message: "...", data: { token: "eyJhbGc..." } }

// Step 4: Use token to get user info
GET /api/auth/me
Headers: Authorization: Bearer eyJhbGc...
// Response: { status: true, message: "...", data: { user_id, name, phone, ... } }

// Step 5: Access protected endpoints
GET /api/locations/user/123
Headers: Authorization: Bearer eyJhbGc...

// Step 6: Logout (optional - invalidates token)
POST /api/auth/logout
Headers: Authorization: Bearer eyJhbGc...
// Response: { status: true, message: "Muvaffaqiyatli logout qilindi", data: null }
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

### Adding New Service with ServiceResult Pattern

1. Create interface in `Convoy.Service/Interfaces/`
   ```csharp
   public interface IYourService
   {
       Task<ServiceResult<YourDto>> GetDataAsync(int id);
   }
   ```
2. Implement in `Convoy.Service/Services/`
   ```csharp
   public async Task<ServiceResult<YourDto>> GetDataAsync(int id)
   {
       try {
           var data = await _repository.GetAsync(id);
           if (data == null)
               return ServiceResult<YourDto>.NotFound("Data topilmadi");

           return ServiceResult<YourDto>.Ok(data, "Ma'lumot olindi");
       } catch (Exception ex) {
           _logger.LogError(ex, "Error getting data");
           return ServiceResult<YourDto>.ServerError("Xatolik yuz berdi");
       }
   }
   ```
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

### SignalR User Active Status Tracking (Flutter)

```dart
import 'package:signalr_netcore/signalr_client.dart';

class LocationSignalRService {
  HubConnection? _hubConnection;
  final String hubUrl = "https://your-api.com/hubs/location";
  final int userId;

  LocationSignalRService({required this.userId});

  // 1. Connect and register user
  Future<void> connect() async {
    _hubConnection = HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect() // Auto-reconnect on network issues
        .build();

    _setupListeners();

    await _hubConnection!.start();

    // CRITICAL: Must call RegisterUser after connection
    await registerUser();
  }

  // 2. Register user (mark as ACTIVE)
  Future<void> registerUser() async {
    try {
      await _hubConnection!.invoke("RegisterUser", args: [userId]);
      print("🟢 User marked as ACTIVE: userId=$userId");
    } catch (e) {
      print("❌ Error registering user: $e");
    }
  }

  // 3. Unregister user (mark as INACTIVE + disconnect)
  Future<void> unregisterUser() async {
    try {
      await _hubConnection!.invoke("UnregisterUser", args: [userId]);
      print("🔴 User unregistered and disconnected");
      await Future.delayed(Duration(milliseconds: 500));
      _hubConnection = null;
    } catch (e) {
      print("❌ Error unregistering user: $e");
    }
  }

  // 4. Listen for server events
  void _setupListeners() {
    _hubConnection!.on("UserRegistered", (args) {
      print("✅ User registered: ${args?[0]}");
    });

    _hubConnection!.on("UserUnregistered", (args) {
      print("🔴 User unregistered: ${args?[0]}");
    });

    _hubConnection!.on("LocationUpdated", (args) {
      print("📍 Location updated: ${args?[0]}");
    });
  }

  // 5. Disconnect (automatic inactive marking)
  Future<void> disconnect() async {
    await _hubConnection?.stop();
    print("🔴 Disconnected - user marked INACTIVE automatically");
  }
}

// App lifecycle handling
class MyApp extends StatefulWidget with WidgetsBindingObserver {
  late LocationSignalRService _signalRService;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _signalRService = LocationSignalRService(userId: currentUserId);
    _signalRService.connect();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.paused) {
      _signalRService.disconnect(); // User marked inactive
    } else if (state == AppLifecycleState.resumed) {
      _signalRService.connect(); // User marked active
    }
  }

  @override
  void dispose() {
    _signalRService.disconnect();
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }
}
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
- **OTP expired**: Default 1 minute, check `Auth:OtpExpirationMinutes` config
- **Wrong OTP**: OTP is one-time use, request new one if failed
- **Rate limit exceeded**: User trying to request OTP too frequently
  - Default: 60 seconds between requests for same phone number
  - Error message: "Iltimos N soniya kuting va qayta urinib ko'ring"
  - Configure via `Auth:OtpRateLimitSeconds`:
    - Set to `60` (recommended for production) - 1 minute wait time
    - Set to `30` - 30 seconds wait time
    - Set to `0` - **DISABLE** rate limiting (for development/testing only, NOT recommended for production)
  - When disabled, logs will show: "OTP rate limiting is DISABLED"
- **Development testing**: OTP codes logged to console (search for "DEVELOPMENT" in logs)
- **Phone number format**: SmsFly automatically formats phone numbers (9 digits → adds 998 prefix)

### External PHP API Integration Issues
- **User not found**: Verify phone number exists in PHP API database
- **Position denied**: Check `Auth:AllowedPositionIds` configuration
- **API unreachable**: Verify `PhpApi:GlobalPathForSupport` and network connectivity
- **Authentication failed**: Check `PhpApi:Username` and `PhpApi:Password` are correct (uses Basic Auth)
- **Endpoint not found**: Ensure using POST `/auth-service/verification-user` with phone_number in body
- **Timeout**: Increase HttpClient timeout in `PhpApiService` if needed

### Permission System Issues
- **403 Forbidden**: User lacks required permission for endpoint
  - Check user has role assigned: `SELECT * FROM user_roles WHERE user_id = 123;`
  - Check role has permission: `SELECT p.name FROM role_permissions rp JOIN permissions p ON rp.permission_id = p.id WHERE rp.role_id = 1;`
  - Verify permission exists: `SELECT * FROM permissions WHERE name = 'users.view';`
- **Permission not working**: Ensure `PermissionSeedService` ran successfully on startup
  - Check logs for: "Permission seed completed successfully"
  - Manually run: `psql -U postgres -d convoy_db -f add-permission-system.sql`
- **GetMe returns no permissions**: User has no roles assigned
  - Assign role via API: `POST /api/permissions/users/{userId}/roles/{roleId}`
  - Or SQL: `INSERT INTO user_roles (user_id, role_id, assigned_at, created_at) VALUES (1, 4, NOW(), NOW());`

### Token Blacklist Issues
- **Logout not working**: Check `token_blacklist` table exists
  - Create manually if needed (see database schema)
- **Token still valid after logout**: Verify `PermissionAuthorizationHandler` checks blacklist
- **Blacklist table full**: Implement cleanup job to remove expired tokens:
  ```sql
  DELETE FROM token_blacklist WHERE expires_at < NOW();
  ```

### SignalR User Active Status Issues
- **User not marked inactive on disconnect**: Flutter didn't call `RegisterUser(userId)` after connecting
  - **Solution**: Add `await registerUser()` after `await _hubConnection!.start()`
- **User marked inactive immediately after connect**: Network issue causing rapid connect/disconnect
  - **Solution**: Use `.withAutomaticReconnect()` in HubConnectionBuilder
- **User stays active after app closes**: Flutter didn't properly disconnect
  - **Solution**: Handle app lifecycle (pause/resume) with `WidgetsBindingObserver`
  - Call `disconnectHub()` on pause, `connect() + registerUser()` on resume
- **Multiple users marked inactive when one disconnects**: Bug in ConnectionId -> UserId mapping
  - **Solution**: Check backend logs for connection ID mismatches
- **UnregisterUser not working**: SignalR not connected
  - **Solution**: Use hybrid approach - try SignalR first, fallback to REST API `PATCH /api/users/{id}/status?isActive=false`
- **Check user status in database**:
  ```sql
  SELECT user_id, name, is_active, updated_at
  FROM users
  WHERE user_id = 5475;
  ```

## Performance Considerations

- **Partition pruning**: Always include `recorded_at` in WHERE clause for best query performance
- **Batch inserts**: Use `InsertBatchAsync` for multiple locations (single SQL statement)
- **Indexes**: Already created on `(user_id, recorded_at)` for common query patterns
- **Connection pooling**: Handled by Npgsql/PostgreSQL automatically (min 0, max 100 by default)

## File Locations & Important Documentation

### Critical Reference Documents (READ THESE FIRST)

- **`SERVICE_RESULT_PATTERN.md`**: How services return results and controllers handle them - MANDATORY reading
- **`API_RESPONSE_FORMAT.md`**: Standard API response format with complete examples for all endpoints
- **`SNAKE_CASE_API_GUIDE.md`**: Complete guide to snake_case naming convention - CRITICAL for API consistency
- **`PERMISSION_SYSTEM_GUIDE.md`**: Complete Permission & Role-Based Access Control (RBAC) guide - MANDATORY for authorization
- **`SIGNALR-TESTING-GUIDE.md`**: Complete testing guide for SignalR real-time features
- **`FLUTTER-SIGNALR-EXAMPLE.md`**: Flutter client implementation examples
- **`SIGNALR_USER_ACTIVE_STATUS.md`**: SignalR user active/inactive tracking - CRITICAL for connection lifecycle
- **`SIGNALR_USER_STATUS_CHANGE.md`**: User manual status change and disconnect - Flutter integration patterns
- **`FLUTTER_ENCRYPTION_GUIDE.md`**: End-to-end AES-256 encryption implementation for Flutter (request/response encryption)
- **`ENCRYPTION_EXCLUDED_ROUTES_GUIDE.md`**: How to exclude specific routes from encryption (e.g., `/api/locations`)
- **`TELEGRAM_SERVICE_GUIDE.md`**: Telegram bot integration - kanalga xabar yuborish (locations, alerts, reports)
- **`FLUTTER_BACKGROUND_GEOLOCATION_INTEGRATION.md`**: Flutter Background Geolocation library integration - complete migration guide

### Code & Scripts

- **SQL scripts**: Root directory (`database-setup.sql`, `create-partitions.sql`, `add-permission-system.sql`, `update-locations-table.sql`)
- **API examples**: `API-EXAMPLES.http` (REST Client format)
- **Deployment docs**: `DOCKER-DEPLOYMENT.md`, `QUICK-START.md`, `SETUP.md`
- **Batch scripts**: Windows: `*.bat`, Linux/Mac: `*.sh`
- **Encryption key generators**: `generate-encryption-keys.ps1` (Windows), `generate-encryption-keys.sh` (Linux/Mac)
- **Configuration templates**: `appsettings.json`, `appsettings.Development.json`

## Critical Configuration Keys

### Required in appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=convoy_db;Username=postgres;Password=YOUR_PASSWORD;Include Error Detail=true"
  },
  "Jwt": {
    "SecretKey": "your-256-bit-secret-key-here-make-it-long-and-secure-change-this-in-production",
    "Issuer": "ConvoyApi",
    "Audience": "ConvoyClients",
    "ExpirationHours": 24
  },
  "Auth": {
    "AllowedPositionIds": "86",  // Comma-separated position IDs, empty = all positions
    "OtpLength": 4,
    "OtpExpirationMinutes": 1,
    "OtpRateLimitSeconds": 60  // Minimum seconds between OTP requests for same phone number
  },
  "PhpApi": {
    "GlobalPathForSupport": "https://your-php-api.com/api/",
    "Username": "login",
    "Password": "password"
  },
  "SmsProviders": {
    "SmsFly": {
      "ApiKey": "your-api-key",
      "ApiUrl": "https://api.smsfly.uz/send"
    },
    "Sayqal": {
      "UserName": "your-username",
      "SecretKey": "your-secret-key",
      "ApiUrl": "https://routee.sayqal.uz/sms/TransmitSMS"
    }
  },
  "DeploymentUrl": "https://your-deployment-url.com",
  "Encryption": {
    "Enabled": false,  // Set to true in production
    "Key": "GENERATE_WITH_generate-encryption-keys.ps1",  // Base64 encoded 32-byte key
    "IV": "GENERATE_WITH_generate-encryption-keys.ps1"    // Base64 encoded 16-byte IV
  }
}
```

**IMPORTANT**: Configuration structure changed - SMS providers are nested under `SmsProviders` object, and PhpApi uses `GlobalPathForSupport` instead of `BaseUrl`.

### Environment-Specific Overrides

- **Development**: Use `appsettings.Development.json` for local settings
- **Docker**: Set via environment variables (double underscore notation):
  - `ConnectionStrings__DefaultConnection`
  - `Jwt__SecretKey`
  - `PhpApi__GlobalPathForSupport`
  - `SmsProviders__SmsFly__ApiKey`
- **Production**: Use secrets management (Azure Key Vault, AWS Secrets Manager, etc.)

---

## Common Mistakes to Avoid

### ❌ WRONG: Using camelCase in API

```csharp
// WRONG - camelCase endpoint
[HttpPost("verifyNumber")]

// WRONG - camelCase JSON property
public class Dto {
    public string PhoneNumber { get; set; }  // Will serialize as "phoneNumber"
}
```

### ✅ CORRECT: Using snake_case everywhere

```csharp
// CORRECT - snake_case endpoint
[HttpPost("verify_number")]

// CORRECT - snake_case JSON property
public class Dto {
    [JsonProperty("phone_number")]
    public string PhoneNumber { get; set; }  // Will serialize as "phone_number"
}
```

### ❌ WRONG: Exposing raw exceptions to clients

```csharp
// WRONG - Leaking exception details
[HttpGet]
public async Task<IActionResult> Get()
{
    var data = await _service.GetData();  // May throw exception
    return Ok(data);  // Exception bubbles up with stack trace
}
```

### ✅ CORRECT: Proper exception handling with standardized responses

```csharp
// OPTION 1 - LocationController pattern (ServiceResult)
public async Task<ServiceResult<DataDto>> GetData()
{
    try {
        var data = await _repository.GetAsync();
        return ServiceResult<DataDto>.Ok(data, "Success");
    } catch (Exception ex) {
        _logger.LogError(ex, "Error");
        return ServiceResult<DataDto>.ServerError("Error occurred");
    }
}

[HttpGet]
public async Task<IActionResult> Get()
{
    var result = await _service.GetData();
    return StatusCode(result.StatusCode, new {
        status = result.Success,
        message = result.Message,
        data = result.Data
    });
}

// OPTION 2 - AuthController pattern (try-catch in controller)
[HttpPost("endpoint")]
public async Task<IActionResult> DoSomething([FromBody] Request request)
{
    try {
        var result = await _service.DoSomethingAsync(request);

        var response = new {
            status = result.Status,
            message = result.Message,
            data = result.Data
        };

        if (!result.Status)
            return BadRequest(response);

        return Ok(response);
    } catch (Exception ex) {
        _logger.LogError(ex, "Error in DoSomething");
        return StatusCode(500, new {
            status = false,
            message = "Internal server error",
            data = (object?)null
        });
    }
}
```

### ❌ WRONG: Querying partitioned table without partition key

```csharp
// WRONG - Missing recorded_at filter (scans all partitions)
SELECT * FROM locations WHERE user_id = @UserId
```

### ✅ CORRECT: Always include partition key in WHERE clause

```csharp
// CORRECT - Includes recorded_at for partition pruning
SELECT * FROM locations
WHERE user_id = @UserId
  AND recorded_at >= @StartDate
  AND recorded_at < @EndDate
```

### ❌ WRONG: Using EF migrations for partitioned tables

```bash
# WRONG - Don't use EF migrations for locations table
dotnet ef migrations add AddColumnToLocations
```

### ✅ CORRECT: Modify partitioned tables via SQL scripts

```sql
-- CORRECT - Modify database-setup.sql and re-run
ALTER TABLE locations ADD COLUMN new_field VARCHAR(100);
```

### ❌ WRONG: Inconsistent response format

```csharp
// WRONG - Different response formats
return Ok(data);  // Returns just data
return Ok(new { success = true, result = data });  // Different structure
```

### ✅ CORRECT: Always use standardized format

```csharp
// CORRECT - Consistent response format
return StatusCode(result.StatusCode, new {
    status = result.Success,
    message = result.Message,
    data = result.Data
});
```
