using Convoy.Domain.Commons;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convoy.Domain.Entities;

/// <summary>
/// User 1 soatdan ko'p to'xtab qolganda sababini saqlash uchun
/// </summary>
[Table("user_stopped_reports")]
public class UserStoppedReport : Auditable
{
    /// <summary>
    /// User ID (PHP API worker_id, int type)
    /// Foreign key bo'lmaydi, faqat ma'lumot sifatida saqlanadi
    /// </summary>
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("location_id")]
    public long LocationId { get; set; }

    [Column("latitude")]
    public decimal Latitude { get; set; }

    [Column("longitude")]
    public decimal Longitude { get; set; }

    [Column("stopped_at")]
    public DateTime StoppedAt { get; set; }  // Qachon to'xtagan

    [Column("stopped_duration_minutes")]
    public int StoppedDurationMinutes { get; set; }  // Necha daqiqa to'xtagan

    [Column("reason")]
    public string Reason { get; set; } = string.Empty;  // Sababi (internet uzildi, aktiv emas, manual stop, va hokazo)

    [Column("is_resolved")]
    public bool IsResolved { get; set; } = false;  // User harakatni davom ettirganda true bo'ladi

    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; set; }  // Qachon hal qilingan
}
