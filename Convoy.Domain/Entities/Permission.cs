using Convoy.Domain.Commons;
using System.ComponentModel.DataAnnotations.Schema;

namespace Convoy.Domain.Entities;

/// <summary>
/// Permission entity - Tizim ruxsatlari (users.view, locations.create, va h.k.)
/// Naming convention: <resource>.<action>
/// </summary>
[Table("permissions")]
public class Permission : Auditable
{
    /// <summary>
    /// Unique permission identifier (users.view, locations.create, va h.k.)
    /// </summary>
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name (View Users, Create Location)
    /// </summary>
    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Resource category (users, locations, reports, va h.k.)
    /// </summary>
    [Column("resource")]
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Action type (view, create, update, delete, export, va h.k.)
    /// </summary>
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
