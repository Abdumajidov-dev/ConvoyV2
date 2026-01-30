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

    // Navigation property
    public User? User { get; set; }
}
