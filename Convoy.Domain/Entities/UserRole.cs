using Convoy.Domain.Commons;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convoy.Domain.Entities;

/// <summary>
/// UserRole junction entity - User va Role orasidagi Many-to-Many relationship
/// Bir user bir nechta role'ga ega bo'lishi mumkin
/// </summary>
[Table("user_roles")]
public class UserRole : Auditable
{
    [Column("user_id")]
    public long UserId { get; set; }

    [Column("role_id")]
    public long RoleId { get; set; }

    /// <summary>
    /// Role qachon user'ga assign qilingan
    /// </summary>
    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Role kim tomonidan assign qilingan (optional)
    /// </summary>
    [Column("assigned_by")]
    public long? AssignedBy { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;
}
