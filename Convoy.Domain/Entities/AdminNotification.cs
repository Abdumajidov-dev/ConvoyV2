using Convoy.Domain.Commons;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Convoy.Domain.Entities;

/// <summary>
/// Admin'larga yuborilgan notification'larni saqlash
/// </summary>
[Table("admin_notifications")]
public class AdminNotification : Auditable
{
    [Column("user_id")]
    [JsonPropertyName("user_id")]
    public long UserId { get; set; } // Qaysi user haqida notification

    [Column("admin_user_id")]
    [JsonPropertyName("admin_user_id")]
    public long AdminUserId { get; set; } // Qaysi admin'ga yuborildi

    [Column("notification_type")]
    [JsonPropertyName("notification_type")]
    public string NotificationType { get; set; } = string.Empty; // "location_timeout", "offline_alert"

    [Column("title")]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [Column("message")]
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [Column("offline_duration_minutes")]
    [JsonPropertyName("offline_duration_minutes")]
    public int OfflineDurationMinutes { get; set; }

    [Column("is_sent")]
    [JsonPropertyName("is_sent")]
    public bool IsSent { get; set; }

    [Column("sent_at")]
    [JsonPropertyName("sent_at")]
    public DateTime? SentAt { get; set; }

    [Column("is_read")]
    [JsonPropertyName("is_read")]
    public bool IsRead { get; set; }

    [Column("read_at")]
    [JsonPropertyName("read_at")]
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public User? AdminUser { get; set; }
}
