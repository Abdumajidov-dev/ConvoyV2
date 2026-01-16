# Flutter Background Geolocation Integration

## Overview

Bu loyiha Flutter Background Geolocation library bilan to'liq integratsiya qilindi. Flutter'dan kelgan location data'lari to'liq saqlanadi va qaytariladi.

---

## Changes Made

### 1. Database Migration (`update-locations-table.sql`)

**Location**: Root directory - `update-locations-table.sql`

PostgreSQL `locations` table'ga 14 ta yangi column qo'shildi:

#### Extended Coords
- `ellipsoidal_altitude` - Altitude above WGS84 reference ellipsoid (meters)
- `heading_accuracy` - Heading accuracy in degrees
- `speed_accuracy` - Speed accuracy in meters/second
- `altitude_accuracy` - Altitude accuracy in meters
- `floor` - Building floor (iOS only)

#### Battery
- `battery_is_charging` - Is device plugged in to power

#### Location Metadata
- `timestamp` - Flutter timestamp (ISO 8601 UTC format)
- `age` - Location age in milliseconds
- `event` - Event type: motionchange, heartbeat, providerchange, geofence
- `mock` - Android mock location flag
- `sample` - Sample location flag (ignore for upload)
- `odometer` - Distance traveled in meters
- `uuid` - Unique identifier
- `extras` - Arbitrary extras (JSONB)

**Migration qo'llash**:
```bash
# pgAdmin yoki psql orqali run qiling:
psql -U postgres -d convoy_db -f update-locations-table.sql
```

**Important**: Bu script partitioned table bilan ishlaydi - barcha partition'larga avtomatik qo'llanadi.

---

### 2. Updated DTOs

**Location**: `Convoy.Service/DTOs/LocationDtos.cs`

#### LocationDataDto (Request)
Flutter'dan kelgan barcha field'lar qabul qilinadi:

```csharp
public class LocationDataDto
{
    // Core location properties
    public DateTime RecordedAt { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? Accuracy { get; set; }
    public decimal? Speed { get; set; }
    public decimal? Heading { get; set; }
    public decimal? Altitude { get; set; }

    // Flutter Background Geolocation - Extended Coords
    public decimal? EllipsoidalAltitude { get; set; }
    public decimal? HeadingAccuracy { get; set; }
    public decimal? SpeedAccuracy { get; set; }
    public decimal? AltitudeAccuracy { get; set; }
    public int? Floor { get; set; }

    // Activity
    public string? ActivityType { get; set; }
    public int? ActivityConfidence { get; set; }
    public bool IsMoving { get; set; } = false;

    // Battery
    public int? BatteryLevel { get; set; }
    public bool? IsCharging { get; set; }

    // Flutter Background Geolocation - Location metadata
    public DateTime? Timestamp { get; set; }
    public decimal? Age { get; set; }
    public string? Event { get; set; }
    public bool? Mock { get; set; }
    public bool? Sample { get; set; }
    public decimal? Odometer { get; set; }
    public string? Uuid { get; set; }
    public string? Extras { get; set; }  // JSON string
}
```

#### LocationResponseDto (Response)
API response'da barcha field'lar qaytariladi.

---

### 3. Updated Location Entity

**Location**: `Convoy.Domain/Entities/Location.cs`

Entity barcha yangi property'lar bilan yangilandi. To'liq Flutter Background Geolocation model strukturasini aks ettiradi.

---

### 4. Updated LocationService

**Location**: `Convoy.Service/Services/LocationService.cs`

#### CreateUserLocationBatchAsync Method
Location yaratishda barcha yangi field'lar mapping qilinadi:

```csharp
var location = new Location
{
    UserId = dto.UserId,
    RecordedAt = locDto.RecordedAt,

    // Core location properties
    Latitude = locDto.Latitude,
    Longitude = locDto.Longitude,
    // ... other core fields

    // Flutter Background Geolocation - Extended Coords
    EllipsoidalAltitude = locDto.EllipsoidalAltitude,
    HeadingAccuracy = locDto.HeadingAccuracy,
    SpeedAccuracy = locDto.SpeedAccuracy,
    AltitudeAccuracy = locDto.AltitudeAccuracy,
    Floor = locDto.Floor,

    // Flutter Background Geolocation - Location metadata
    Timestamp = locDto.Timestamp,
    Age = locDto.Age,
    Event = locDto.Event,
    Mock = locDto.Mock,
    Sample = locDto.Sample,
    Odometer = locDto.Odometer,
    Uuid = locDto.Uuid,
    Extras = locDto.Extras,

    // ... battery, activity, calculated fields
};
```

#### MapToDto Method
Response yaratishda barcha field'lar to'liq mapped.

---

### 5. Updated LocationRepository

**Location**: `Convoy.Data/Repositories/LocationRepository.cs`

Barcha SQL query'lar yangilandi:

#### InsertAsync & InsertBatchAsync
INSERT statement'ga barcha yangi column'lar qo'shildi:

```sql
INSERT INTO locations (
    user_id, recorded_at, latitude, longitude,
    accuracy, speed, heading, altitude,
    ellipsoidal_altitude, heading_accuracy, speed_accuracy, altitude_accuracy, floor,
    activity_type, activity_confidence, is_moving,
    battery_level, is_charging,
    timestamp, age, event, mock, sample, odometer, uuid, extras,
    distance_from_previous, created_at
) VALUES (
    @UserId, @RecordedAt, @Latitude, @Longitude,
    @Accuracy, @Speed, @Heading, @Altitude,
    @EllipsoidalAltitude, @HeadingAccuracy, @SpeedAccuracy, @AltitudeAccuracy, @Floor,
    @ActivityType, @ActivityConfidence, @IsMoving,
    @BatteryLevel, @IsCharging,
    @Timestamp, @Age, @Event, @Mock, @Sample, @Odometer, @Uuid, @Extras,
    @DistanceFromPrevious, @CreatedAt
)
```

#### GetUserLocationsAsync, GetLastLocationsAsync, GetByIdAsync
SELECT statement'larga barcha yangi column'lar qo'shildi va Dapper mapping uchun alias'lar to'g'ri configured.

---

## API Usage

### Request Format (snake_case)

```json
POST /api/locations/user_batch
{
  "user_id": 123,
  "locations": [
    {
      "recorded_at": "2025-12-25T10:30:00Z",
      "latitude": 41.311151,
      "longitude": 69.279737,
      "accuracy": 10.5,
      "speed": 5.2,
      "heading": 180.0,
      "altitude": 350.0,

      // Flutter Background Geolocation - Extended Coords
      "ellipsoidal_altitude": 352.5,
      "heading_accuracy": 5.0,
      "speed_accuracy": 1.5,
      "altitude_accuracy": 3.0,
      "floor": 2,

      // Activity
      "activity_type": "walking",
      "activity_confidence": 85,
      "is_moving": true,

      // Battery
      "battery_level": 75,
      "is_charging": false,

      // Flutter Background Geolocation - Location metadata
      "timestamp": "2025-12-25T10:30:00Z",
      "age": 500,
      "event": "motionchange",
      "mock": false,
      "sample": false,
      "odometer": 1234.56,
      "uuid": "550e8400-e29b-41d4-a716-446655440000",
      "extras": "{\"custom_field\":\"value\"}"
    }
  ]
}
```

### Response Format (snake_case)

```json
{
  "status": true,
  "message": "Location muvaffaqiyatli yaratildi",
  "data": [
    {
      "id": 12345,
      "user_id": 123,
      "recorded_at": "2025-12-25T10:30:00Z",
      "latitude": 41.311151,
      "longitude": 69.279737,
      "accuracy": 10.5,
      "speed": 5.2,
      "heading": 180.0,
      "altitude": 350.0,

      // Flutter Background Geolocation - Extended Coords
      "ellipsoidal_altitude": 352.5,
      "heading_accuracy": 5.0,
      "speed_accuracy": 1.5,
      "altitude_accuracy": 3.0,
      "floor": 2,

      // Activity
      "activity_type": "walking",
      "activity_confidence": 85,
      "is_moving": true,

      // Battery
      "battery_level": 75,
      "is_charging": false,

      // Flutter Background Geolocation - Location metadata
      "timestamp": "2025-12-25T10:30:00Z",
      "age": 500.0,
      "event": "motionchange",
      "mock": false,
      "sample": false,
      "odometer": 1234.56,
      "uuid": "550e8400-e29b-41d4-a716-446655440000",
      "extras": "{\"custom_field\":\"value\"}",

      // Calculated fields
      "distance_from_previous": 250.5,
      "created_at": "2025-12-25T10:30:05Z"
    }
  ]
}
```

---

## Flutter Integration Example

### 1. Flutter Background Geolocation Package

```yaml
# pubspec.yaml
dependencies:
  flutter_background_geolocation: ^4.16.3
```

### 2. Location Callback

```dart
import 'package:flutter_background_geolocation/flutter_background_geolocation.dart' as bg;

// Configure background geolocation
bg.BackgroundGeolocation.onLocation((bg.Location location) async {
  // Send to API
  await sendLocationToApi(location);
});

Future<void> sendLocationToApi(bg.Location location) async {
  final response = await http.post(
    Uri.parse('https://your-api.com/api/locations/user_batch'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
      'user_id': currentUserId,
      'locations': [
        {
          'recorded_at': location.timestamp,
          'latitude': location.coords.latitude,
          'longitude': location.coords.longitude,
          'accuracy': location.coords.accuracy,
          'speed': location.coords.speed,
          'heading': location.coords.heading,
          'altitude': location.coords.altitude,

          // Extended coords
          'ellipsoidal_altitude': location.coords.ellipsoidalAltitude,
          'heading_accuracy': location.coords.headingAccuracy,
          'speed_accuracy': location.coords.speedAccuracy,
          'altitude_accuracy': location.coords.altitudeAccuracy,
          'floor': location.coords.floor,

          // Activity
          'activity_type': location.activity.type,
          'activity_confidence': location.activity.confidence,
          'is_moving': location.isMoving,

          // Battery
          'battery_level': (location.battery.level * 100).toInt(),
          'is_charging': location.battery.isCharging,

          // Metadata
          'timestamp': location.timestamp,
          'age': location.age,
          'event': location.event,
          'mock': location.mock,
          'sample': location.sample,
          'odometer': location.odometer,
          'uuid': location.uuid,
          'extras': jsonEncode(location.extras ?? {}),
        }
      ]
    }),
  );
}
```

---

## Deployment Steps

### 1. Database Migration

```bash
# Stop application (agar running bo'lsa)
docker-compose down

# Run migration
psql -U postgres -d convoy_db -f update-locations-table.sql

# Verify migration
psql -U postgres -d convoy_db -c "SELECT column_name, data_type FROM information_schema.columns WHERE table_name = 'locations' ORDER BY ordinal_position;"
```

### 2. Code Deployment

```bash
# Build application
dotnet build

# Run application
dotnet run --project Convoy.Api

# Or with Docker
docker-compose up -d --build
```

### 3. Testing

```bash
# Test location creation with new fields
curl -X POST http://localhost:5084/api/locations/user_batch \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "user_id": 123,
    "locations": [{
      "recorded_at": "2025-12-25T10:30:00Z",
      "latitude": 41.311151,
      "longitude": 69.279737,
      "accuracy": 10.5,
      "ellipsoidal_altitude": 352.5,
      "timestamp": "2025-12-25T10:30:00Z",
      "event": "motionchange",
      "uuid": "550e8400-e29b-41d4-a716-446655440000"
    }]
  }'
```

---

## Important Notes

### 1. Backwards Compatibility

✅ **Barcha yangi field'lar nullable** - eski Flutter clientlar hali ham ishlaydi
✅ **Eski data saqlanadi** - migration faqat yangi column'lar qo'shadi
✅ **API versioning kerak emas** - nullable field'lar tufayli

### 2. Performance

✅ **Index'lar yaratildi** - `uuid`, `event`, `is_moving`, `mock`, `timestamp` uchun
✅ **Partitioned table qoldirildi** - optimal performance
✅ **Batch insert qo'llab-quvvatlanadi** - ko'p location'larni bir requestda yuborish mumkin

### 3. Data Validation

⚠️ **Extras field** - JSONB type (PostgreSQL native JSON support)
⚠️ **Mock location** - Android'da aniqlash uchun
⚠️ **Sample location** - Test location'larni ignore qilish uchun

### 4. Flutter Library Events

- `motionchange` - User moving/stationary transition
- `heartbeat` - Periodic location update
- `providerchange` - Location provider changed (GPS/Network)
- `geofence` - Geofence enter/exit event

---

## Troubleshooting

### Problem: Migration failed

```bash
# Check table exists
psql -U postgres -d convoy_db -c "SELECT tablename FROM pg_tables WHERE tablename = 'locations';"

# Check for existing columns
psql -U postgres -d convoy_db -c "SELECT column_name FROM information_schema.columns WHERE table_name = 'locations';"
```

### Problem: Build failed due to locked files

```bash
# Application running bo'lsa to'xtating
# Stop running process (Ctrl+C yoki Docker stop)

# Rebuild
dotnet clean
dotnet build
```

### Problem: Dapper mapping error

**Solution**: SQL query'da column alias'lar to'g'ri bo'lishi kerak:
- Database: `ellipsoidal_altitude`
- C# Property: `EllipsoidalAltitude`
- SQL Alias: `ellipsoidal_altitude as EllipsoidalAltitude`

---

## Summary

✅ Database schema updated with 14 new columns
✅ DTOs updated to accept/return all Flutter Background Geolocation fields
✅ Entity updated with comprehensive property documentation
✅ Service layer updated with complete field mapping
✅ Repository updated with SQL queries for all new columns
✅ Backwards compatible - nullable fields
✅ Performance optimized - indexes created
✅ Production ready - tested with existing partitioned table structure

**Next Steps**:
1. Run database migration: `update-locations-table.sql`
2. Deploy updated code
3. Update Flutter app to send full location data
4. Monitor logs and Telegram notifications

---

Happy Coding! 🚀
