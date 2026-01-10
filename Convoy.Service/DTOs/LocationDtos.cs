using System.Text.Json.Serialization;

namespace Convoy.Service.DTOs;


/// <summary>
/// Location data from Flutter (sent in request body)
/// Matches Flutter Background Geolocation library model
/// FAQAT latitude va longitude required, qolgan barcha field'lar optional
/// </summary>
public class LocationDataDto
{
    // Core location properties (REQUIRED)
    [JsonPropertyName("latitude")]
    public decimal Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public decimal Longitude { get; set; }

    // Core location properties (OPTIONAL)
    [JsonPropertyName("recorded_at")]
    public DateTime? RecordedAt { get; set; }

    [JsonPropertyName("accuracy")]
    public decimal? Accuracy { get; set; }

    [JsonPropertyName("speed")]
    public decimal? Speed { get; set; }

    [JsonPropertyName("heading")]
    public decimal? Heading { get; set; }

    [JsonPropertyName("altitude")]
    public decimal? Altitude { get; set; }

    // Flutter Background Geolocation - Extended Coords (OPTIONAL)
    [JsonPropertyName("ellipsoidal_altitude")]
    public decimal? EllipsoidalAltitude { get; set; }

    [JsonPropertyName("heading_accuracy")]
    public decimal? HeadingAccuracy { get; set; }

    [JsonPropertyName("speed_accuracy")]
    public decimal? SpeedAccuracy { get; set; }

    [JsonPropertyName("altitude_accuracy")]
    public decimal? AltitudeAccuracy { get; set; }

    [JsonPropertyName("floor")]
    public int? Floor { get; set; }

    // Activity (OPTIONAL)
    [JsonPropertyName("activity_type")]
    public string? ActivityType { get; set; }

    [JsonPropertyName("activity_confidence")]
    public int? ActivityConfidence { get; set; }

    [JsonPropertyName("is_moving")]
    public bool? IsMoving { get; set; }

    // Battery (OPTIONAL)
    [JsonPropertyName("battery_level")]
    public int? BatteryLevel { get; set; }

    [JsonPropertyName("is_charging")]
    public bool? IsCharging { get; set; }

    // Flutter Background Geolocation - Location metadata (OPTIONAL)
    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }  // Flutter timestamp

    [JsonPropertyName("age")]
    public decimal? Age { get; set; }  // Location age in milliseconds

    [JsonPropertyName("event")]
    public string? Event { get; set; }  // motionchange, heartbeat, providerchange, geofence

    [JsonPropertyName("mock")]
    public bool? Mock { get; set; }  // Android mock location

    [JsonPropertyName("sample")]
    public bool? Sample { get; set; }  // Is this a sample location

    [JsonPropertyName("odometer")]
    public decimal? Odometer { get; set; }  // Distance traveled in meters

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }  // Unique identifier

    [JsonPropertyName("extras")]
    public string? Extras { get; set; }  // Arbitrary extras (JSON string)
}

/// <summary>
/// Flutter Background Geolocation nested format
/// </summary>
public class FlutterCoordsDto
{
    [JsonPropertyName("latitude")]
    public decimal Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public decimal Longitude { get; set; }

    [JsonPropertyName("accuracy")]
    public decimal? Accuracy { get; set; }

    [JsonPropertyName("speed")]
    public decimal? Speed { get; set; }

    [JsonPropertyName("speed_accuracy")]
    public decimal? SpeedAccuracy { get; set; }

    [JsonPropertyName("heading")]
    public decimal? Heading { get; set; }

    [JsonPropertyName("heading_accuracy")]
    public decimal? HeadingAccuracy { get; set; }

    [JsonPropertyName("altitude")]
    public decimal? Altitude { get; set; }

    [JsonPropertyName("ellipsoidal_altitude")]
    public decimal? EllipsoidalAltitude { get; set; }

    [JsonPropertyName("altitude_accuracy")]
    public decimal? AltitudeAccuracy { get; set; }
}

public class FlutterActivityDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("confidence")]
    public int? Confidence { get; set; }
}

//public class FlutterBatteryDto
//{
//    [JsonPropertyName("is_charging")]
//    public bool? IsCharging { get; set; }

//    [JsonPropertyName("level")]
//    public int? Level { get; set; }
//}

/// <summary>
/// Flutter Background Geolocation request format
/// Body: { "location": { "coords": {...}, "activity": {...}, ... } }
/// </summary>
public class FlutterLocationDto
{
    [JsonPropertyName("is_moving")]
    public bool? IsMoving { get; set; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    [JsonPropertyName("recorded_at")]
    public DateTime? RecordedAt { get; set; }

    [JsonPropertyName("age")]
    public decimal? Age { get; set; }

    [JsonPropertyName("odometer")]
    public decimal? Odometer { get; set; }

    [JsonPropertyName("coords")]
    public FlutterCoordsDto? Coords { get; set; }

    [JsonPropertyName("activity")]
    public FlutterActivityDto? Activity { get; set; }

    //[JsonPropertyName("battery")]
    //public FlutterBatteryDto? Battery { get; set; }

    [JsonPropertyName("extras")]
    public object? Extras { get; set; }
}

/// <summary>
/// Wrapped location request DTO (Flutter format)
/// Body format: { "location": { "coords": {...}, ... } }
/// </summary>
public class LocationRequestWrapperDto
{
    [JsonPropertyName("location")]
    public FlutterLocationDto? Location { get; set; }
}

/// <summary>
/// Location response DTO
/// Matches Flutter Background Geolocation library model
/// </summary>
public class LocationResponseDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("recorded_at")]
    public DateTime RecordedAt { get; set; }

    // Core location properties
    [JsonPropertyName("latitude")]
    public decimal Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public decimal Longitude { get; set; }

    [JsonPropertyName("accuracy")]
    public decimal? Accuracy { get; set; }

    [JsonPropertyName("speed")]
    public decimal? Speed { get; set; }

    [JsonPropertyName("heading")]
    public decimal? Heading { get; set; }

    [JsonPropertyName("altitude")]
    public decimal? Altitude { get; set; }

    // Flutter Background Geolocation - Extended Coords
    [JsonPropertyName("ellipsoidal_altitude")]
    public decimal? EllipsoidalAltitude { get; set; }

    [JsonPropertyName("heading_accuracy")]
    public decimal? HeadingAccuracy { get; set; }

    [JsonPropertyName("speed_accuracy")]
    public decimal? SpeedAccuracy { get; set; }

    [JsonPropertyName("altitude_accuracy")]
    public decimal? AltitudeAccuracy { get; set; }

    [JsonPropertyName("floor")]
    public int? Floor { get; set; }

    // Activity
    [JsonPropertyName("activity_type")]
    public string? ActivityType { get; set; }

    [JsonPropertyName("activity_confidence")]
    public int? ActivityConfidence { get; set; }

    [JsonPropertyName("is_moving")]
    public bool IsMoving { get; set; }

    // Battery
    [JsonPropertyName("battery_level")]
    public int? BatteryLevel { get; set; }

    [JsonPropertyName("is_charging")]
    public bool? IsCharging { get; set; }

    // Flutter Background Geolocation - Location metadata
    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    [JsonPropertyName("age")]
    public decimal? Age { get; set; }

    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("mock")]
    public bool? Mock { get; set; }

    [JsonPropertyName("sample")]
    public bool? Sample { get; set; }

    [JsonPropertyName("odometer")]
    public decimal? Odometer { get; set; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("extras")]
    public string? Extras { get; set; }

    // Calculated fields
    [JsonPropertyName("distance_from_previous")]
    public decimal? DistanceFromPrevious { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Kunlik statistika DTO
/// </summary>
public class DailyStatisticsDto
{
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("total_distance_meters")]
    public decimal TotalDistanceMeters { get; set; }

    [JsonPropertyName("total_distance_kilometers")]
    public decimal TotalDistanceKilometers => Math.Round(TotalDistanceMeters / 1000, 2);

    [JsonPropertyName("location_count")]
    public int LocationCount { get; set; }
}

/// <summary>
/// Location query uchun filter DTO (GET endpoint - deprecated, query params)
/// </summary>
public class LocationQueryDto
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("start_date")]
    public DateTime? StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public DateTime? EndDate { get; set; }

    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }  // Format: "HH:MM" (masalan: "09:30", "14:45")

    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }    // Format: "HH:MM" (masalan: "17:30", "23:59")

    [JsonPropertyName("limit")]
    public int? Limit { get; set; } = 100;
}

/// <summary>
/// Bitta userning locationlarini olish uchun filter DTO (POST endpoint - body orqali)
/// User ID route'da beriladi, filterlar body'da
/// FAQAT BIR KUNLIK locationlar
/// </summary>
public class SingleUserLocationQueryDto
{
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }  // Faqat sana (masalan: "2026-01-07")

    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }  // Format: "HH:MM" (masalan: "09:30", "14:45")

    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }    // Format: "HH:MM" (masalan: "17:30", "23:59")
}

/// <summary>
/// Daily summary query DTO
/// </summary>
public class DailySummaryQueryDto
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("start_date")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Ko'p userlarning locationlarini olish uchun query DTO (body orqali)
/// FAQAT BIR KUNLIK locationlar
/// user_ids, branch_guid yoki ikkalasi ham null bo'lsa barcha userlar
/// </summary>
public class MultipleUsersLocationQueryDto
{
    [JsonPropertyName("user_ids")]
    public List<int>? UserIds { get; set; }  // Optional: aniq userlar ro'yxati

    [JsonPropertyName("branch_guid")]
    public string? BranchGuid { get; set; }  // Optional: branch bo'yicha filter

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;  // Format: "2026-01-07 03:54:32.302400" yoki "2026-01-07"

    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }  // Format: "HH:MM"

    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }    // Format: "HH:MM"

    [JsonPropertyName("limit")]
    public int? Limit { get; set; } = 100;  // Har bir user uchun limit
}
