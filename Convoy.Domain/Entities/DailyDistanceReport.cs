using Convoy.Domain.Commons;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convoy.Domain.Entities;

/// <summary>
/// Kunlik masofa hisoboti - har bir foydalanuvchi kunlik bosib o'tgan masofani saqlaydi
/// EF Core bilan ishlaydi
/// </summary>
[Table("daily_distance_reports")]
public class DailyDistanceReport : Auditable
{
    /// <summary>
    /// Foydalanuvchi ID (users.id ga foreign key)
    /// </summary>
    [Column("user_id")]
    public long UserId { get; set; }

    /// <summary>
    /// Hisobot sanasi (yyyy-MM-dd format)
    /// </summary>
    [Column("report_date")]
    public DateTime ReportDate { get; set; }

    /// <summary>
    /// Umumiy masofa metrlarda
    /// </summary>
    [Column("total_distance_meters")]
    public decimal TotalDistanceMeters { get; set; }

    /// <summary>
    /// Umumiy masofa kilometrlarda
    /// </summary>
    [Column("total_distance_km")]
    public decimal TotalDistanceKm { get; set; }

    /// <summary>
    /// Jami location qo'shilgan soni
    /// </summary>
    [Column("location_count")]
    public int LocationCount { get; set; }

    /// <summary>
    /// Birinchi location vaqti
    /// </summary>
    [Column("first_location_time")]
    public DateTime? FirstLocationTime { get; set; }

    /// <summary>
    /// Oxirgi location vaqti
    /// </summary>
    [Column("last_location_time")]
    public DateTime? LastLocationTime { get; set; }

    // Navigation property
    public virtual User? User { get; set; }
}
