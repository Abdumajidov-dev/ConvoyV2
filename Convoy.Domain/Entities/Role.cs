using Convoy.Domain.Commons;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convoy.Domain.Entities;

/// <summary>
/// Role entity - Foydalanuvchi rollari (Admin, Manager, Driver, va h.k.)
/// </summary>
[Table("roles")]
public class Role : Auditable
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
