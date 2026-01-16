namespace Convoy.Domain.Entities;

/// <summary>
/// Location entity - Dapper bilan ishlaydi (partitioned table)
/// Auditable'dan inherit QILMAYDI chunki partition key conflict
/// Matches Flutter Background Geolocation library model
/// </summary>
public class Location
{
    /// <summary>
    /// Primary key (composite key bilan: id + recorded_at)
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// User ID (Foreign Key)
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// GPS koordinata yozilgan vaqt (Partition key)
    /// </summary>
    public DateTime RecordedAt { get; set; }

    // ============================
    // Core Location Properties
    // ============================

    /// <summary>
    /// Kenglik (Latitude) - decimal(10, 8)
    /// </summary>
    public decimal Latitude { get; set; }

    /// <summary>
    /// Uzunlik (Longitude) - decimal(11, 8)
    /// </summary>
    public decimal Longitude { get; set; }

    /// <summary>
    /// GPS aniqlik (metrda) - decimal(6, 2)
    /// </summary>
    public decimal? Accuracy { get; set; }

    /// <summary>
    /// Tezlik (m/s) - decimal(6, 2)
    /// </summary>
    public decimal? Speed { get; set; }

    /// <summary>
    /// Yo'nalish (daraja) - decimal(5, 2)
    /// </summary>
    public decimal? Heading { get; set; }

    /// <summary>
    /// Balandlik (metrda) - decimal(8, 2)
    /// </summary>
    public decimal? Altitude { get; set; }

    // ============================
    // Flutter Background Geolocation - Extended Coords
    // ============================

    /// <summary>
    /// Altitude above WGS84 reference ellipsoid (meters)
    /// </summary>
    public decimal? EllipsoidalAltitude { get; set; }

    /// <summary>
    /// Heading accuracy in degrees
    /// </summary>
    public decimal? HeadingAccuracy { get; set; }

    /// <summary>
    /// Speed accuracy in meters/second
    /// </summary>
    public decimal? SpeedAccuracy { get; set; }

    /// <summary>
    /// Altitude accuracy in meters
    /// </summary>
    public decimal? AltitudeAccuracy { get; set; }

    /// <summary>
    /// Floor within a building (iOS only)
    /// </summary>
    public int? Floor { get; set; }

    // ============================
    // Activity
    // ============================

    /// <summary>
    /// Faoliyat turi (still, walking, running, cycling, automotive)
    /// </summary>
    public string? ActivityType { get; set; }

    /// <summary>
    /// Faoliyat ishonch darajasi (0-100)
    /// </summary>
    public int? ActivityConfidence { get; set; }

    /// <summary>
    /// Harakat holatida
    /// </summary>
    public bool IsMoving { get; set; } = false;

    // ============================
    // Battery
    // ============================

    /// <summary>
    /// Batareya darajasi (0-100)
    /// </summary>
    public int? BatteryLevel { get; set; }

    /// <summary>
    /// Quvvatlash holatida (is device plugged in)
    /// </summary>
    public bool? IsCharging { get; set; }

    // ============================
    // Flutter Background Geolocation - Location Metadata
    // ============================

    /// <summary>
    /// Flutter timestamp (ISO 8601 UTC format)
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Age of location in milliseconds
    /// </summary>
    public decimal? Age { get; set; }

    /// <summary>
    /// Event that caused this location: motionchange, heartbeat, providerchange, geofence
    /// </summary>
    public string? Event { get; set; }

    /// <summary>
    /// Android only - true if location from mock app
    /// </summary>
    public bool? Mock { get; set; }

    /// <summary>
    /// True if this is sample location (ignore for upload)
    /// </summary>
    public bool? Sample { get; set; }

    /// <summary>
    /// Current distance traveled in meters
    /// </summary>
    public decimal? Odometer { get; set; }

    /// <summary>
    /// Universally Unique Identifier
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Arbitrary extras object (JSON string)
    /// </summary>
    public string? Extras { get; set; }

    // ============================
    // Calculated Fields
    // ============================

    /// <summary>
    /// Oldingi nuqtadan masofa (metrda) - decimal(10, 2)
    /// </summary>
    public decimal? DistanceFromPrevious { get; set; }

    /// <summary>
    /// Yaratilgan vaqt
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
