using Convoy.Api.Authorization;
using Convoy.Domain.Constants;
using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Convoy.Api.Controllers;

/// <summary>
/// Permission management controller
/// Permission sistemasini boshqarish uchun endpoint'lar
/// </summary>
[ApiController]
[Route("api/permissions")]
[Authorize] // Faqat authenticate qilingan userlar
public class PermissionController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionController> _logger;

    public PermissionController(
        IPermissionService permissionService,
        ILogger<PermissionController> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <summary>
    /// Barcha rollarni olish
    /// </summary>
    [HttpGet("roles")]
    [HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetAllRoles()
    {
        var result = await _permissionService.GetAllRolesAsync();

        return StatusCode(result.StatusCode, new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        });
    }

    /// <summary>
    /// Barcha ruxsatlarni olish
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.PermissionsManagement.View)]
    public async Task<IActionResult> GetAllPermissions()
    {
        var result = await _permissionService.GetAllPermissionsAsync();

        return StatusCode(result.StatusCode, new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        });
    }

    /// <summary>
    /// Role'ning ruxsatlarini olish
    /// </summary>
    [HttpGet("roles/{roleId}/permissions")]
    [HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetRolePermissions(long roleId)
    {
        var result = await _permissionService.GetRolePermissionsAsync(roleId);

        return StatusCode(result.StatusCode, new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        });
    }

    /// <summary>
    /// User'ning rollarini olish
    /// </summary>
    [HttpGet("users/{userId}/roles")]
    [HasPermission(Permissions.Users.View)]
    public async Task<IActionResult> GetUserRoles(long userId)
    {
        var result = await _permissionService.GetUserRolesAsync(userId);

        return StatusCode(result.StatusCode, new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        });
    }

    /// <summary>
    /// User'ning barcha ruxsatlarini olish
    /// </summary>
    [HttpGet("users/{userId}")]
    [HasPermission(Permissions.Users.View)]
    public async Task<IActionResult> GetUserPermissions(long userId)
    {
        var permissions = await _permissionService.GetUserPermissionsAsync(userId);

        return Ok(new
        {
            status = true,
            message = "Foydalanuvchi ruxsatlari olindi",
            data = permissions
        });
    }

    /// <summary>
    /// User'ga rol biriktirish
    /// </summary>
    [HttpPost("users/{userId}/roles/{roleId}")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<IActionResult> AssignRoleToUser(long userId, long roleId)
    {
        // Current user ID'ni olish (kim assign qilyapti)
        var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                                  ?? User.FindFirst("user_id");
        long? currentUserId = currentUserIdClaim != null && long.TryParse(currentUserIdClaim.Value, out var id)
            ? id
            : null;

        var result = await _permissionService.AssignRoleToUserAsync(userId, roleId, currentUserId);

        return StatusCode(result.StatusCode, new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        });
    }

    /// <summary>
    /// User'dan rol olib tashlash
    /// </summary>
    [HttpDelete("users/{userId}/roles/{roleId}")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<IActionResult> RemoveRoleFromUser(long userId, long roleId)
    {
        var result = await _permissionService.RemoveRoleFromUserAsync(userId, roleId);

        return StatusCode(result.StatusCode, new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        });
    }

    /// <summary>
    /// Role'ga ruxsat biriktirish
    /// </summary>
    [HttpPost("roles/{roleId}/permissions/{permissionId}")]
    [HasPermission(Permissions.Roles.AssignPermissions)]
    public async Task<IActionResult> AssignPermissionToRole(long roleId, long permissionId)
    {
        // Current user ID'ni olish
        var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                                  ?? User.FindFirst("user_id");
        long? currentUserId = currentUserIdClaim != null && long.TryParse(currentUserIdClaim.Value, out var id)
            ? id
            : null;

        var result = await _permissionService.AssignPermissionToRoleAsync(roleId, permissionId, currentUserId);

        return StatusCode(result.StatusCode, new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        });
    }

    /// <summary>
    /// Role'dan ruxsat olib tashlash
    /// </summary>
    [HttpDelete("roles/{roleId}/permissions/{permissionId}")]
    [HasPermission(Permissions.Roles.AssignPermissions)]
    public async Task<IActionResult> RemovePermissionFromRole(long roleId, long permissionId)
    {
        var result = await _permissionService.RemovePermissionFromRoleAsync(roleId, permissionId);

        return StatusCode(result.StatusCode, new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        });
    }
}
