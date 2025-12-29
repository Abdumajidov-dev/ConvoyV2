using Convoy.Service.Common;
using Convoy.Service.DTOs;

namespace Convoy.Service.Interfaces;

/// <summary>
/// Role CRUD service interface
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Barcha role'larni olish
    /// </summary>
    Task<ServiceResult<List<RoleResponseDto>>> GetAllRolesAsync();

    /// <summary>
    /// Role'ni ID orqali olish
    /// </summary>
    Task<ServiceResult<RoleResponseDto>> GetRoleByIdAsync(long roleId);

    /// <summary>
    /// Role'ni permission'lari bilan olish
    /// </summary>
    Task<ServiceResult<RoleWithPermissionsDto>> GetRoleWithPermissionsAsync(long roleId);

    /// <summary>
    /// Yangi role yaratish
    /// </summary>
    Task<ServiceResult<RoleResponseDto>> CreateRoleAsync(CreateRoleRequest request);

    /// <summary>
    /// Role'ni yangilash
    /// </summary>
    Task<ServiceResult<RoleResponseDto>> UpdateRoleAsync(long roleId, UpdateRoleRequest request);

    /// <summary>
    /// Role'ni o'chirish (soft delete)
    /// </summary>
    Task<ServiceResult<bool>> DeleteRoleAsync(long roleId);

    /// <summary>
    /// Role'ni aktivlashtirish/deaktivlashtirish
    /// </summary>
    Task<ServiceResult<bool>> ToggleRoleStatusAsync(long roleId, bool isActive);
}
