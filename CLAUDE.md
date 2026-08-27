# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Convoy GPS Tracking System** - Enterprise GPS tracking with PostgreSQL partitioned tables, built with .NET 8. This system uses a hybrid ORM approach: Dapper for partitioned tables (high performance) and EF Core for standard tables (convenience).

### CRITICAL CHANGES FROM ORIGINAL DESIGN

**⚠️ IMPORTANT**: This system has evolved significantly from the initial architecture:

1. **Authentication**: External PHP API handles login/OTP (NOT this backend)
   - This API only validates JWT tokens from PHP API
   - No internal OTP generation or SMS sending
   - `PhpTokenAuthenticationHandler` replaces JWT bearer authentication

2. **Notifications**: Firebase Cloud Messaging (FCM) instead of Telegram
   - Admin notifications via push notifications (not Telegram bot)
   - Device tokens stored in `device_tokens` table
   - Firebase Admin SDK initialized on app startup

3. **Location Monitoring**: Background service tracks user activity
   - `CheckLocationCreatedBackrounService` runs every 1 minute
   - Notifies admins when users offline > 20, 40, 60, 80, 100, 120 minutes
   - Stores notification history in `admin_notifications` table

4. **Location Clustering**: Automatic grouping of nearby locations
   - Reduces response size by 4-10x (e.g., 43 → 10 locations)
   - Calculates `stopped_time` for each cluster
   - Applied automatically to `POST /api/locations/multiple_users`

5. **Railway Deployment**: Cloud-native configuration
   - Supports `DATABASE_URL` environment variable (PostgreSQL URI)
   - Firebase credentials via `FIREBASE_CREDENTIALS_BASE64` (base64-encoded JSON)
   - Health check endpoints for platform monitoring

**Key Technologies**:
- **PostgreSQL Partitioning**: Monthly partitioned `locations` table by `recorded_at` (format: `locations_MM_YYYY`) for efficient queries
- **SignalR**: Real-time GPS location broadcasting to connected clients
- **PHP API Integration**: External PHP API for authentication (JWT token validation, no internal OTP/SMS)
- **Firebase Cloud Messaging**: Push notifications to mobile devices for location monitoring alerts
- **Location Clustering**: Automatic location grouping (10m radius) with stopped time calculation
- **User Monitoring**: Background service checking user location activity and sending admin notifications
- **Flutter Background Geolocation**: Full integration with flutter_background_geolocation library (extended coords, metadata, events)
- **snake_case JSON**: ALL API endpoints and JSON fields use snake_case naming convention
- **OSRM Integration**: Road-based distance via OSRM HTTP API with Haversine fallback
- **Railway/Docker Deployment**: Support for both Railway cloud and Docker container deployment

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
│   │   ├── IPhpApiService.cs
│   │   └── IOsrmService.cs
│   └── Services/
│       ├── LocationService.cs              # Business logic + SignalR broadcast + OSRM distance
│       ├── OsrmService.cs                  # OSRM HTTP API client (road distance, Haversine fallback)
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

**4. Distance Calculation (OSRM + Haversine Fallback)**
- **Primary**: OSRM HTTP API (`IOsrmService`) calculates road-based distance
  - Configured via `Osrm:BaseUrl` in appsettings.json (default: `http://router.project-osrm.org`)
  - CRITICAL: OSRM coordinate format is `longitude,latitude` (NOT lat,lon)
  - 5-second timeout; returns `null` on failure
- **Fallback**: Haversine formula (`LocationRepository.CalculateDistance`) used when OSRM returns null
- `LocationService.CalculateDistanceAsync()` orchestrates primary + fallback logic
- Distance stored in `distance_from_previous` column (nullable decimal, meters)
- Calculated on each insert by comparing to user's last location
- Also available as PostgreSQL function `calculate_distance()` in database (Haversine only)

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

**7. Authentication Flow (PHP API Integration)**
- **IMPORTANT**: Authentication handled by external PHP API, NOT this backend
- **Step 1 - Mobile Login**: User logs in via PHP API (handled by Flutter app)
- **Step 2 - Get JWT Token**: PHP API returns JWT token to mobile app
- **Step 3 - Use Token**: Include `Authorization: Bearer {token}` header in requests to this API
- **Step 4 - Token Validation**: `PhpTokenService` decodes JWT token (no validation, trusted source)
- **Step 5 - Get User Info**: `GET /api/auth/me` → Returns user info from JWT token claims
- **Step 6 - Save Device Token**: `POST /api/auth/save_device_token` → Register FCM token for push notifications
- JWT token contains: `user_id`, `unique_name`, `mobilephone`, `worker_guid`, `branch_guid`, `branch_name`, `position_id`
- No OTP generation or SMS sending in this backend (removed from original design)
- No token blacklist (logout handled by mobile app clearing token)

**8. Firebase Cloud Messaging (FCM) Integration**
- **Purpose**: Send push notifications to mobile devices for location monitoring alerts
- **Service**: `NotificationService` uses Firebase Admin SDK for sending notifications
- **Device Registration**: `DeviceTokenService` manages FCM device tokens
- **Initialization**: `DirectFirebaseService` singleton initializes Firebase on app startup
- **Credential Loading**: Supports both environment variable (Railway) and file (local dev)
  - Production: `FIREBASE_CREDENTIALS_BASE64` environment variable (base64 encoded JSON)
  - Development: `firebase-adminsdk.json` file in API root directory
- **Notification Types**: User offline alerts, location timeouts, admin notifications
- **Target Users**: Can send to specific users or all admins (role='admin_unduruv')
- See `FIREBASE_DEPLOYMENT.md` and `USER_LOCATION_MONITORING_GUIDE.md` for setup

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

**15. Location Clustering & Stopped Time Calculation**
- **Purpose**: Reduce location data by grouping nearby points and calculating stopped time
- **Service**: `LocationClusteringService` automatically clusters locations within 10m radius
- **Algorithm**: DBSCAN-like clustering - groups locations < 10m apart into clusters
- **Output**: One representative location per cluster with `stopped_time` field
- **Stopped Time**: Duration between first and last location in cluster (in minutes)
- **Example**: 43 locations → 10 clustered locations (4.3x reduction)
- **Endpoints**: `POST /api/locations/multiple_users` automatically applies clustering
- **Configuration**: Change `ClusterRadiusMeters = 10.0` in `LocationClusteringService.cs`
- **Benefits**: Reduced response size, fewer map markers, better UX, meaningful data
- See `FILTERED_LOCATIONS_GUIDE.md` and `MULTIPLE_USERS_CLUSTERING.md` for details

**16. User Location Monitoring & Admin Notifications**
- **Purpose**: Monitor users for inactivity and notify admins via push notifications
- **Background Service**: `CheckLocationCreatedBackrounService` runs every 1 minute
- **Monitoring Logic**: Checks all active users' last location timestamp
- **Thresholds**: Sends notifications at 20, 40, 60, 80, 100, 120 minutes offline
- **Target Admins**: Only users with `role='admin_unduruv'` receive notifications
- **Tables**:
  - `device_tokens`: FCM tokens for push notifications
  - `user_status_reports`: Tracks last location time and notification count
  - `admin_notifications`: Stores all sent notifications
- **Notification Content**: User name, offline duration, last seen time
- **Deduplication**: Only sends one notification per threshold (no spam)
- See `USER_LOCATION_MONITORING_GUIDE.md` for complete documentation

**17. Railway Cloud Platform Deployment**
- **Connection String**: Supports both `ConnectionStrings__DefaultConnection` and `DATABASE_URL` env vars
- **PostgreSQL URI Conversion**: Automatically converts Railway's `postgresql://` URI to Npgsql format
- **Firebase Credentials**: Base64-encoded JSON in `FIREBASE_CREDENTIALS_BASE64` environment variable
- **Health Checks**: `/health` and `/` (redirects to Swagger) endpoints for Railway monitoring
- **HTTPS Handling**: Disables HTTPS redirection in production (Railway handles SSL termination)
- **Swagger**: Enabled in all environments (Railway requires public docs endpoint)
- **Logs**: Console logging for Railway log aggregation
- See `RAILWAY_FIREBASE_SETUP.md` and `PRODUCTION_DEPLOYMENT_GUIDE.md` for deployment

**18. Portainer Docker Deployment**
- **Container Management**: Deploy via Portainer UI using Docker Compose
- **Docker Image**: `jm7uz/convoy:latest` (pre-built image from Docker Hub)
- **Port Mapping**: External port 3908 → Internal port 8080
- **Environment Variables**: Same as Railway (database connection, Firebase credentials)
- **Health Checks**: Configured with 30s interval, 10s timeout, 3 retries, 40s start period
- **Logging**: JSON file driver with 10MB max size, 3 file rotation
- **Firebase Setup**: Use `encode-firebase-credentials.ps1` to convert JSON to base64
- See `PORTAINER-DEPLOYMENT-GUIDE.md` for complete deployment instructions

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
├── id SERIAL PRIMARY KEY (internal DB key)
├── user_id INTEGER UNIQUE (PHP API worker ID - external reference)
├── name VARCHAR(200)
├── username VARCHAR(100)
├── phone VARCHAR(20)
├── branch_guid VARCHAR(100) (PHP API branch GUID)
├── branch_name VARCHAR(200)
├── worker_guid VARCHAR(100) (PHP API worker GUID)
├── position_id INTEGER (PHP API position ID)
├── image VARCHAR(500) (user avatar URL)
├── user_type VARCHAR(50) (e.g., "worker", "admin")
├── role VARCHAR(100) (e.g., "admin_unduruv", "driver")
├── is_active BOOLEAN
└── created_at, updated_at, delete_at

device_tokens (FCM Push Notifications)
├── id BIGSERIAL PRIMARY KEY
├── user_id BIGINT FK -> users.id
├── token VARCHAR(500) (Firebase Cloud Messaging token)
├── device_system VARCHAR(20) ("android", "ios")
├── model VARCHAR(100) (device model name)
├── device_id VARCHAR(100) (unique device identifier)
├── is_physical_device BOOLEAN
├── is_active BOOLEAN (token validity)
└── created_at, updated_at, delete_at

user_status_reports (Location Monitoring)
├── id BIGSERIAL PRIMARY KEY
├── user_id BIGINT FK -> users.id
├── last_location_time TIMESTAMPTZ (last location post time)
├── last_notified_at TIMESTAMPTZ (last notification sent time)
├── offline_duration_minutes INTEGER (current offline duration)
├── is_notified BOOLEAN
├── notification_count INTEGER (total notifications sent)
└── created_at, updated_at, delete_at

admin_notifications (Notification History)
├── id BIGSERIAL PRIMARY KEY
├── user_id BIGINT FK -> users.id (user being monitored)
├── admin_user_id BIGINT FK -> users.id (admin receiving notification)
├── notification_type VARCHAR(50) ("user_offline", "location_timeout")
├── title VARCHAR(200)
├── message VARCHAR(1000)
├── offline_duration_minutes INTEGER
├── is_sent BOOLEAN
├── sent_at TIMESTAMPTZ
├── is_read BOOLEAN
├── read_at TIMESTAMPTZ
└── created_at, updated_at, delete_at

user_stopped_reports (Manual Stop Reports)
├── id BIGSERIAL PRIMARY KEY
├── user_id INTEGER FK -> users.id
├── location_id BIGINT (reference to location)
├── reason VARCHAR (why user stopped)
└── created_at, updated_at, delete_at
```

## Development Guidelines

### When Modifying Location Table

1. **Never use EF Core migrations** for `locations` table - it's partitioned and managed by raw SQL
2. **Always include both `id` AND `recorded_at`** in WHERE clauses for single-record queries (enables partition pruning)
3. **Dapper mapping**: Use column aliases in SELECT (e.g., `user_id as UserId`) or configure column mappings
4. **New columns**: Add to `database-setup.sql` script, not EF migrations

### When Modifying Standard Tables (User, DeviceToken, etc.)

1. **Use EF Core migrations** normally - standard tables without partitioning
2. Generate migration: `dotnet ef migrations add MigrationName --project Convoy.Data --startup-project Convoy.Api`
3. Apply migration: `dotnet ef database update --project Convoy.Data --startup-project Convoy.Api`
4. **Cleanup old data**: Use scheduled jobs or manual queries to clean old notifications/reports

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

- **CRITICAL**: This backend does NOT handle login/registration - delegated to external PHP API
- **PHP Token Validation**: `PhpTokenAuthenticationHandler` decodes JWT tokens from PHP API
  - Token issued by external PHP system (not this backend)
  - Contains claims: `user_id`, `unique_name`, `mobilephone`, `worker_guid`, `branch_guid`, `branch_name`, `position_id`, `role`
  - No signature validation (trusted source assumption)
- **User Sync**: First request with valid token auto-creates/updates user in local database
  - Extracts user data from JWT claims
  - Creates or updates user record in `users` table
- **Token Service**: `IPhpTokenService` for decoding tokens (no validation, just parsing)
- **Device Token Registration**: After login, mobile app calls `POST /api/auth/save_device_token`
  - Saves FCM token for push notifications
  - Required for receiving location monitoring alerts

### Firebase Cloud Messaging Configuration

- **Service Account Setup**: Download `firebase-adminsdk.json` from Firebase Console → Project Settings → Service Accounts
- **Development**: Place `firebase-adminsdk.json` in `Convoy.Api/` directory
- **Production (Railway)**:
  1. Base64 encode the JSON file: `base64 -w 0 firebase-adminsdk.json > firebase-base64.txt`
  2. Set environment variable: `FIREBASE_CREDENTIALS_BASE64=<base64-string>`
- **Initialization**: `DirectFirebaseService` singleton loads credentials on app startup
- **Testing**: Check logs for "Firebase Admin SDK initialized successfully"
- **Device Token Storage**: Mobile apps send FCM tokens via `POST /api/auth/save_device_token`
- See `FIREBASE_DEPLOYMENT.md` and `RAILWAY_FIREBASE_SETUP.md` for detailed setup

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

**Railway Cloud Platform**:
- Supports `DATABASE_URL` environment variable (PostgreSQL URI format)
- Automatically converts `postgresql://user:pass@host:port/db` to Npgsql format
- Priority: `ConnectionStrings__DefaultConnection` > `DATABASE_URL` > appsettings.json
- URI conversion handles URL-encoded passwords

**Portainer (Production)**:
- Docker image: `jm7uz/convoy:latest`
- External port: 3908 → Internal port: 8080
- Database: Connect via Docker bridge network (172.17.0.1) or direct IP
- Firebase credentials: Base64-encoded via `FIREBASE_CREDENTIALS_BASE64` env var
- Use `encode-firebase-credentials.ps1` script to generate base64 string

## Background Services Configuration

### CheckLocationCreatedBackrounService

**Purpose**: Monitor user location activity and send admin notifications

**Configuration**:
```csharp
// Runs every 1 minute
private const int CheckIntervalMinutes = 1;

// Notification thresholds (minutes offline)
private readonly int[] NotificationThresholds = { 20, 40, 60, 80, 100, 120 };

// Target admins by role
private const string AdminRole = "admin_unduruv";
```

**Logs to monitor**:
- "🔄 CheckLocationCreatedBackrounService started at..."
- "🔍 Checking user locations at..."
- "🚨 NOTIFICATION YUBORILDI: User ... - 20 daqiqadan beri offline"

### Location Clustering Configuration

**Default settings**:
```csharp
// LocationClusteringService.cs
private const double ClusterRadiusMeters = 10.0;  // 10 meter radius
```

**Change clustering radius**:
```csharp
private const double ClusterRadiusMeters = 20.0;  // For looser clustering
private const double ClusterRadiusMeters = 5.0;   // For tighter clustering
```

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

### Implementing PHP API Authentication Flow

```dart
// Flutter client-side flow

// Step 1: Login via PHP API (external system)
final response = await phpApi.login(phone: "+998901234567", password: "1234");
final jwtToken = response.data['token'];  // JWT token from PHP API

// Step 2: Save token locally
await secureStorage.write(key: 'auth_token', value: jwtToken);

// Step 3: Get user info from Convoy API
GET /api/auth/me
Headers: Authorization: Bearer {jwtToken}
// Response: { status: true, data: { user_id, name, phone, role, ... } }

// Step 4: Save FCM device token for notifications
POST /api/auth/save_device_token
Headers: Authorization: Bearer {jwtToken}
Body: {
  "user_id": 5475,
  "device_info": {
    "device_token": "fcm-token-from-firebase",
    "device_system": "android",
    "model": "Samsung Galaxy S21",
    "device_id": "unique-device-id",
    "is_physical_device": true
  }
}

// Step 5: Access protected endpoints
GET /api/locations/user/123
Headers: Authorization: Bearer {jwtToken}

// Step 6: Logout (mobile app clears token, no backend invalidation)
await secureStorage.delete(key: 'auth_token');
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

### Adding Location Clustering to Existing Endpoints

```csharp
// In LocationService
public async Task<List<LocationResponseDto>> GetUserLocationsAsync(int userId, DateTime date)
{
    // 1. Get raw locations from repository
    var locations = await _locationRepository.GetByUserAndDateAsync(userId, date);

    // 2. Apply clustering
    var clustered = _clusteringService.GetFilteredLocationsWithStoppedTime(locations);

    // 3. Map to DTOs
    return _mapper.Map<List<LocationResponseDto>>(clustered);
}
```

### Adding Firebase Notification to New Features

```csharp
// 1. Inject INotificationService
private readonly INotificationService _notificationService;

// 2. Get admin users
var admins = await _userRepository.GetByRoleAsync("admin_unduruv");

// 3. Send notification
foreach (var admin in admins)
{
    await _notificationService.SendNotificationToUserAsync(
        userId: admin.Id,
        title: "Location Alert",
        body: $"User {userName} has been offline for {minutes} minutes",
        data: new Dictionary<string, string>
        {
            { "type", "user_offline" },
            { "user_id", userId.ToString() },
            { "offline_minutes", minutes.ToString() }
        }
    );
}
```

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

### PHP API Authentication Issues
- **401 Unauthorized**: Check token is valid and from PHP API
  - Verify token in request header: `Authorization: Bearer {token}`
  - Check token is not expired (expires_at claim)
- **Invalid token format**: PHP API must return valid JWT token
  - Required claims: `user_id`, `unique_name`, `mobilephone`, `role`
- **User not synced**: First request with token auto-creates user
  - Check user exists: `SELECT * FROM users WHERE user_id = 5475;`
  - If not created, check JWT claims are present
- **Token validation**: This backend does NOT validate signatures (trusted source)
  - Tokens come from external PHP API (already validated there)

### Firebase Cloud Messaging Issues
- **Notifications not received**: Check Firebase setup
  1. Verify credentials loaded: Check logs for "Firebase Admin SDK initialized successfully"
  2. Verify device token saved: `SELECT * FROM device_tokens WHERE user_id = 5475 AND is_active = true;`
  3. Check admin users exist: `SELECT * FROM users WHERE role = 'admin_unduruv' AND is_active = true;`
  4. Test notification manually: Use `POST /api/notification/test_notification`
- **Firebase initialization failed**:
  - Development: Ensure `firebase-adminsdk.json` exists in `Convoy.Api/`
  - Production: Check `FIREBASE_CREDENTIALS_BASE64` environment variable is set correctly
  - Verify base64 encoding: `echo $FIREBASE_CREDENTIALS_BASE64 | base64 -d | jq .`
- **Device token not working**: Token may be expired/invalid
  - Mobile app should refresh token on app startup
  - Call `POST /api/auth/save_device_token` after login

### OSRM Distance Calculation Issues
- **OSRM returns null**: Falls back to Haversine automatically — check logs for "OSRM xatolik" warning
- **Wrong distance calculated**: Verify coordinate order — OSRM expects `longitude,latitude` (NOT lat,lon)
- **Slow distance calculation**: OSRM has 5s timeout. Self-host OSRM for production; set `Osrm:BaseUrl` to internal server
- **Swap to Haversine-only**: Remove `IOsrmService` injection from `LocationService` and call `_locationRepository.CalculateDistance()` directly

### Location Monitoring Issues
- **Background service not running**: Check logs for startup errors
  - Expected log: "🔄 CheckLocationCreatedBackrounService started at..."
  - Verify service registered in `Program.cs`: `AddHostedService<CheckLocationCreatedBackrounService>()`
- **Notifications not sent**: Debug checklist
  1. User offline > 20 minutes? Check: `SELECT * FROM user_status_reports WHERE user_id = 5475;`
  2. Threshold already triggered? Check `last_notified_at` and `notification_count`
  3. Admin has device token? Check: `SELECT dt.* FROM device_tokens dt JOIN users u ON dt.user_id = u.id WHERE u.role = 'admin_unduruv';`
  4. Background service running? Check logs every 1 minute
- **User stays "offline" after posting location**: Check location insert triggers
  - Verify location inserted: `SELECT * FROM locations WHERE user_id = 5475 ORDER BY recorded_at DESC LIMIT 1;`
  - Check user_status_reports updated: Should have new `last_location_time`

### Railway Deployment Issues
- **Database connection failed**: Check `DATABASE_URL` environment variable
  - Railway provides: `postgresql://user:pass@host:port/db`
  - Verify conversion to Npgsql format in logs
- **Firebase not working**: Check `FIREBASE_CREDENTIALS_BASE64` environment variable
  - Must be base64-encoded JSON (no newlines)
  - Test encoding: `cat firebase-adminsdk.json | base64 -w 0`
- **Health check failing**: Railway expects `/health` or `/` to return 200 OK
  - Both endpoints configured, check logs for startup errors

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
- **`USER_LOCATION_MONITORING_GUIDE.md`**: Location monitoring system with admin notifications - CRITICAL for understanding background services
- **`FILTERED_LOCATIONS_GUIDE.md`**: Location clustering and stopped time calculation - IMPORTANT for map optimization
- **`FIREBASE_DEPLOYMENT.md`**: Firebase Cloud Messaging setup and deployment guide
- **`RAILWAY_FIREBASE_SETUP.md`**: Railway platform deployment with Firebase integration
- **`PORTAINER-DEPLOYMENT-GUIDE.md`**: Portainer Docker deployment with complete setup instructions
- **`PRODUCTION_DEPLOYMENT_GUIDE.md`**: Complete production deployment checklist
- **`SIGNALR-TESTING-GUIDE.md`**: Complete testing guide for SignalR real-time features
- **`FLUTTER-SIGNALR-EXAMPLE.md`**: Flutter client implementation examples
- **`SIGNALR_USER_ACTIVE_STATUS.md`**: SignalR user active/inactive tracking - CRITICAL for connection lifecycle
- **`SIGNALR_USER_STATUS_CHANGE.md`**: User manual status change and disconnect - Flutter integration patterns
- **`FLUTTER_ENCRYPTION_GUIDE.md`**: End-to-end AES-256 encryption implementation for Flutter (request/response encryption)
- **`ENCRYPTION_EXCLUDED_ROUTES_GUIDE.md`**: How to exclude specific routes from encryption (e.g., `/api/locations`)
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
  "DeploymentUrl": "https://your-deployment-url.com",
  "Encryption": {
    "Enabled": false,  // Set to true in production
    "Key": "GENERATE_WITH_generate-encryption-keys.ps1",  // Base64 encoded 32-byte key
    "IV": "GENERATE_WITH_generate-encryption-keys.ps1"    // Base64 encoded 16-byte IV
  }
}
```

**IMPORTANT**:
- OTP/SMS providers removed (authentication via external PHP API)
- JWT configuration removed (tokens issued by PHP API, not this backend)
- Firebase credentials loaded from file or environment variable (not appsettings.json)

### Environment-Specific Overrides

- **Development**: Use `appsettings.Development.json` for local settings
  - Place `firebase-adminsdk.json` in `Convoy.Api/` directory
- **Docker**: Set via environment variables (double underscore notation):
  - `ConnectionStrings__DefaultConnection`
  - `Encryption__Enabled`, `Encryption__Key`, `Encryption__IV`
- **Railway/Cloud**: Use platform-specific environment variables:
  - `DATABASE_URL` (PostgreSQL connection string in URI format)
  - `FIREBASE_CREDENTIALS_BASE64` (base64-encoded Firebase service account JSON)
  - `ConnectionStrings__DefaultConnection` (optional, overrides DATABASE_URL)

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
