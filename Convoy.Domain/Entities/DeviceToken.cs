using Convoy.Domain.Commons;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convoy.Domain.Entities;

public class DeviceToken : Auditable
{
    [Column("user_id")]
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    [Column("token")]
    public string Token { get; set; }

    [Column("device_system")]
    public string DeviceSystem { get; set; } // "android", "ios"

    [Column("model")]
    public string Model { get; set; }

    [Column("device_id")]
    public string DeviceId { get; set; }

    [Column("is_physical_device")]
    public bool IsPhysicalDevice { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
