using Convoy.Domain.Commons;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convoy.Domain.Entities;

/// <summary>
/// User status o'zgarishlarining historical tarixini saqlash uchun
/// Har safar user offline/online bo'lganda yoki location post qilganda yangi qator qo'shiladi
/// </summary>
[Table("user_status_history")]
public class UserStatusHistory : Auditable
{
    /// <summary>
    /// User ID (users.id FK)
    /// </summary>
    [Column("user_id")]
    public long UserId { get; set; }

    /// <summary>
    /// User haqida ma'lumot (navigation property)
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// User'ning so'nggi location post qilgan vaqti
    /// </summary>
    [Column("last_location_time")]
    public DateTime? LastLocationTime { get; set; }

    /// <summary>
    /// Offline bo'lgan vaqt (daqiqalarda)
    /// </summary>
    [Column("offline_duration_minutes")]
    public int OfflineDurationMinutes { get; set; }

    /// <summary>
    /// Status o'zgarish turi (e.g., "became_offline", "came_online", "location_posted", "notification_sent")
    /// </summary>
    [Column("status_change_type")]
    public string StatusChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Notification yuborilganmi (agar notification event bo'lsa)
    /// </summary>
    [Column("notification_sent")]
    public bool NotificationSent { get; set; } = false;

    /// <summary>
    /// Qaysi threshold'da notification yuborilgan (20, 40, 60, 80, 100, 120)
    /// </summary>
    [Column("notification_threshold")]
    public int? NotificationThreshold { get; set; }

    /// <summary>
    /// Qo'shimcha ma'lumot (JSON format)
    /// </summary>
    [Column("additional_info")]
    public string? AdditionalInfo { get; set; }
}
