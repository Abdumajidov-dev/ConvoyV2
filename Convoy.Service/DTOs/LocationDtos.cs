namespace Convoy.Service.DTOs;


/// <summary>
/// Location data from Flutter (sent in request body)
/// Matches Flutter Background Geolocation library model
/// FAQAT latitude va longitude required, qolgan barcha field'lar optional
/// </summary>
public class LocationDataDto
{
    // Core location properties (REQUIRED)
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    // Core location properties (OPTIONAL)
    public DateTime? RecordedAt { get; set; }
    public decimal? Accuracy { get; set; }
    public decimal? Speed { get; set; }
    public decimal? Heading { get; set; }
    public decimal? Altitude { get; set; }

    // Flutter Background Geolocation - Extended Coords (OPTIONAL)
    public decimal? EllipsoidalAltitude { get; set; }
    public decimal? HeadingAccuracy { get; set; }
    public decimal? SpeedAccuracy { get; set; }
    public decimal? AltitudeAccuracy { get; set; }
    public int? Floor { get; set; }

    // Activity (OPTIONAL)
    public string? ActivityType { get; set; }
    public int? ActivityConfidence { get; set; }
    public bool? IsMoving { get; set; }

    // Battery (OPTIONAL)
    public int? BatteryLevel { get; set; }
    public bool? IsCharging { get; set; }

    // Flutter Background Geolocation - Location metadata (OPTIONAL)
    public DateTime? Timestamp { get; set; }  // Flutter timestamp
    public decimal? Age { get; set; }  // Location age in milliseconds
    public string? Event { get; set; }  // motionchange, heartbeat, providerchange, geofence
    public bool? Mock { get; set; }  // Android mock location
    public bool? Sample { get; set; }  // Is this a sample location
    public decimal? Odometer { get; set; }  // Distance traveled in meters
    public string? Uuid { get; set; }  // Unique identifier
    public string? Extras { get; set; }  // Arbitrary extras (JSON string)
}

/// <summary>
/// Flutter Background Geolocation nested format
/// </summary>
public class FlutterCoordsDto
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? Accuracy { get; set; }
    public decimal? Speed { get; set; }
    public decimal? SpeedAccuracy { get; set; }
    public decimal? Heading { get; set; }
    public decimal? HeadingAccuracy { get; set; }
    public decimal? Altitude { get; set; }
    public decimal? EllipsoidalAltitude { get; set; }
    public decimal? AltitudeAccuracy { get; set; }
}

public class FlutterActivityDto
{
    public string? Type { get; set; }
    public int? Confidence { get; set; }
}

public class FlutterBatteryDto
{
    public bool? IsCharging { get; set; }
    public int? Level { get; set; }
}

/// <summary>
/// Flutter Background Geolocation request format
/// Body: { "location": { "coords": {...}, "activity": {...}, ... } }
/// </summary>
public class FlutterLocationDto
{
    public bool? IsMoving { get; set; }
    public string? Uuid { get; set; }
    public DateTime? Timestamp { get; set; }
    public DateTime? RecordedAt { get; set; }
    public decimal? Age { get; set; }
    public decimal? Odometer { get; set; }
    public FlutterCoordsDto? Coords { get; set; }
    public FlutterActivityDto? Activity { get; set; }
    public FlutterBatteryDto? Battery { get; set; }
    public object? Extras { get; set; }
}

/// <summary>
/// Wrapped location request DTO (Flutter format)
/// Body format: { "location": { "coords": {...}, ... } }
/// </summary>
public class LocationRequestWrapperDto
{
    public FlutterLocationDto? Location { get; set; }
}

/// <summary>
/// Location response DTO
/// Matches Flutter Background Geolocation library model
/// </summary>
public class LocationResponseDto
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public DateTime RecordedAt { get; set; }

    // Core location properties
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
    public bool IsMoving { get; set; }

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
    public string? Extras { get; set; }

    // Calculated fields
    public decimal? DistanceFromPrevious { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Kunlik statistika DTO
/// </summary>
public class DailyStatisticsDto
{
    public DateTime Date { get; set; }
    public decimal TotalDistanceMeters { get; set; }
    public decimal TotalDistanceKilometers => Math.Round(TotalDistanceMeters / 1000, 2);
    public int LocationCount { get; set; }
}

/// <summary>
/// Location query uchun filter DTO
/// </summary>
public class LocationQueryDto
{
    public int UserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? Limit { get; set; } = 100;
}

/// <summary>
/// Daily summary query DTO
/// </summary>
public class DailySummaryQueryDto
{
    public int UserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// User ma'lumotlari va oxirgi location bilan birga
/// Barcha userlarning oxirgi locationlarini olish uchun ishlatiladi
/// </summary>
public class UserWithLatestLocationDto
{
    // User ma'lumotlari
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Image { get; set; }
    public bool IsActive { get; set; }

    // Oxirgi location (null bo'lishi mumkin agar user hali location yubormagan bo'lsa)
    public LocationResponseDto? LatestLocation { get; set; }
}
