using Convoy.Domain.Entities;
using Convoy.Service.Common;

namespace Convoy.Service.Interfaces;

/// <summary>
/// Permission service interface
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// User'ning berilgan permission'i borligini tekshirish
    /// </summary>
    Task<bool> UserHasPermissionAsync(long userId, string permissionName);

    /// <summary>
    /// User'ning barcha permission'larini olish
    /// </summary>
    Task<List<string>> GetUserPermissionsAsync(long userId);

    /// <summary>
    /// User'ga role assign qilish
    /// </summary>
    Task<ServiceResult<UserRole>> AssignRoleToUserAsync(long userId, long roleId, long? assignedBy = null);

    /// <summary>
    /// User'dan role'ni olib tashlash
    /// </summary>
    Task<ServiceResult<bool>> RemoveRoleFromUserAsync(long userId, long roleId);

    /// <summary>
    /// Role'ga permission assign qilish
    /// </summary>
    Task<ServiceResult<RolePermission>> AssignPermissionToRoleAsync(long roleId, long permissionId, long? grantedBy = null);

    /// <summary>
    /// Role'dan permission'ni olib tashlash
    /// </summary>
    Task<ServiceResult<bool>> RemovePermissionFromRoleAsync(long roleId, long permissionId);

    /// <summary>
    /// Barcha role'larni olish
    /// </summary>
    Task<ServiceResult<List<Role>>> GetAllRolesAsync();

    /// <summary>
    /// Barcha permission'larni olish
    /// </summary>
    Task<ServiceResult<List<Permission>>> GetAllPermissionsAsync();

    /// <summary>
    /// Role bo'yicha permission'larni olish
    /// </summary>
    Task<ServiceResult<List<Permission>>> GetRolePermissionsAsync(long roleId);

    /// <summary>
    /// User'ning role'larini olish
    /// </summary>
    Task<ServiceResult<List<Role>>> GetUserRolesAsync(long userId);

    /// <summary>
    /// User'ning permission'larini grouped format'da olish (Flutter uchun)
    /// Format: [{"users": ["view", "create"]}, {"locations": ["view", "create"]}]
    /// </summary>
    Task<List<Dictionary<string, List<string>>>> GetUserPermissionsGroupedAsync(long userId);
}
