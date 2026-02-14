using Convoy.Domain.Commons;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Convoy.Domain.Entities;

/// <summary>
/// User'ning location post qilish holatini kuzatish uchun
/// </summary>
[Table("user_status_reports")]
public class UserStatusReport : Auditable
{
    [Column("user_id")]
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [Column("last_location_time")]
    [JsonPropertyName("last_location_time")]
    public DateTime? LastLocationTime { get; set; }

    [Column("last_notified_at")]
    [JsonPropertyName("last_notified_at")]
    public DateTime? LastNotifiedAt { get; set; }

    [Column("offline_duration_minutes")]
    [JsonPropertyName("offline_duration_minutes")]
    public int OfflineDurationMinutes { get; set; }

    [Column("is_notified")]
    [JsonPropertyName("is_notified")]
    public bool IsNotified { get; set; }

    [Column("notification_count")]
    [JsonPropertyName("notification_count")]
    public int NotificationCount { get; set; } = 0;

    /// <summary>
    /// User'ning hozirgi aktiv statusi (true = ishda, false = ishdan ketgan)
    /// </summary>
    [Column("is_active")]
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    /// <summary>
    /// Status o'zgarish turi: "came_to_work", "left_work", "offline_detected", "came_online", etc.
    /// </summary>
    [Column("status_change_type")]
    [JsonPropertyName("status_change_type")]
    public string StatusChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Qo'shimcha izoh (user o'zi yozishi mumkin)
    /// </summary>
    [Column("note")]
    [JsonPropertyName("note")]
    public string? Note { get; set; }

    // Navigation property
    public User? User { get; set; }
}
