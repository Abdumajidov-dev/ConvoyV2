using Convoy.Domain.Commons;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convoy.Domain.Entities;

/// <summary>
/// User entity - EF Core bilan ishlaydi
/// </summary>
[Table("users")]
public class User : Auditable
{
    /// <summary>
    /// PHP API'dan kelgan worker_id (external system ID)
    /// Bu orqali user'ni tekshiramiz va sync qilamiz
    /// </summary>
    [Column("user_id")]
    public int? UserId { get; set; }  // PHP API worker_id

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("username")]
    public string? Username { get; set; }  // Nullable - ba'zi userlar uchun NULL bo'lishi mumkin

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("branch_guid")]
    public string? BranchGuid { get; set; }

    /// <summary>
    /// PHP API'dan kelgan user image URL
    /// </summary>
    [Column("image")]
    public string? Image { get; set; }

    /// <summary>
    /// PHP API'dan kelgan worker_guid (UUID)
    /// </summary>
    [Column("worker_guid")]
    public string? WorkerGuid { get; set; }

    /// <summary>
    /// PHP API'dan kelgan position_id
    /// </summary>
    [Column("position_id")]
    public int? PositionId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
