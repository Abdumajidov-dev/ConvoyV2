namespace Convoy.Domain.Entities;

/// <summary>
/// Location entity - Dapper bilan ishlaydi (partitioned table)
/// Auditable'dan inherit QILMAYDI chunki partition key conflict
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

    /// <summary>
    /// Batareya darajasi (0-100)
    /// </summary>
    public int? BatteryLevel { get; set; }

    /// <summary>
    /// Quvvatlash holatida
    /// </summary>
    public bool? IsCharging { get; set; }

    /// <summary>
    /// Oldingi nuqtadan masofa (metrda) - decimal(10, 2)
    /// </summary>
    public decimal? DistanceFromPrevious { get; set; }

    /// <summary>
    /// Yaratilgan vaqt
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
