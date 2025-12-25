using Convoy.Domain.Commons;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convoy.Domain.Entities;

/// <summary>
/// RolePermission junction entity - Role va Permission orasidagi Many-to-Many relationship
/// Bir role bir nechta permission'ga ega bo'lishi mumkin
/// </summary>
[Table("role_permissions")]
public class RolePermission : Auditable
{
    [Column("role_id")]
    public long RoleId { get; set; }

    [Column("permission_id")]
    public long PermissionId { get; set; }

    /// <summary>
    /// Permission qachon role'ga assign qilingan
    /// </summary>
    [Column("granted_at")]
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Permission kim tomonidan grant qilingan (optional)
    /// </summary>
    [Column("granted_by")]
    public long? GrantedBy { get; set; }

    // Navigation properties
    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("PermissionId")]
    public virtual Permission Permission { get; set; } = null!;
}
