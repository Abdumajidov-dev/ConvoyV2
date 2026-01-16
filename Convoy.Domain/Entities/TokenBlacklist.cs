using Convoy.Domain.Commons;

namespace Convoy.Domain.Entities;

/// <summary>
/// Logout qilingan yoki bekor qilingan JWT tokenlar
/// </summary>
public class TokenBlacklist : Auditable
{
    /// <summary>
    /// JWT ID (jti claim) - token'ning unique identifieri
    /// </summary>
    public string TokenJti { get; set; } = string.Empty;

    /// <summary>
    /// Token egasi (user ID)
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Blacklist qilingan vaqt
    /// </summary>
    public DateTime BlacklistedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Token'ning asl expiry vaqti (bundan keyin database'dan o'chirilishi mumkin)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Blacklist qilish sababi: logout, security, admin_revoke, etc.
    /// </summary>
    public string? Reason { get; set; }

    // Navigation property
    public User? User { get; set; }
}
