using Convoy.Domain.Commons;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convoy.Domain.Entities;

/// <summary>
/// User entity - EF Core bilan ishlaydi
/// </summary>
[Table("users")]
public class User : Auditable
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("username")]
    public string? Username { get; set; }  // Nullable - ba'zi userlar uchun NULL bo'lishi mumkin

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("branch_guid")]
    public string? BranchGuid { get; set; }

    [Column("image")]
    public string? Image { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties for Roles
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
