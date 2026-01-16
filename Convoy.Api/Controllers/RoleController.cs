using Convoy.Api.Authorization;
using Convoy.Domain.Constants;
using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Convoy.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RoleController> _logger;

    public RoleController(IRoleService roleService, ILogger<RoleController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// Barcha role'larni olish
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetAllRoles()
    {
        var result = await _roleService.GetAllRolesAsync();

        var response = new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        };

        return StatusCode(result.StatusCode, response);
    }

    /// <summary>
    /// Role'ni ID orqali olish
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetRoleById(long id)
    {
        var result = await _roleService.GetRoleByIdAsync(id);

        var response = new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        };

        return StatusCode(result.StatusCode, response);
    }

    /// <summary>
    /// Role'ni permission'lari bilan olish
    /// </summary>
    [HttpGet("{id}/permissions")]
    [HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetRoleWithPermissions(long id)
    {
        var result = await _roleService.GetRoleWithPermissionsAsync(id);

        var response = new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        };

        return StatusCode(result.StatusCode, response);
    }

    /// <summary>
    /// Yangi role yaratish
    /// </summary>
    [HttpPost]
    [HasPermission(Permissions.Roles.Create)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var result = await _roleService.CreateRoleAsync(request);

        var response = new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        };

        return StatusCode(result.StatusCode, response);
    }

    /// <summary>
    /// Role'ni yangilash
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission(Permissions.Roles.Update)]
    public async Task<IActionResult> UpdateRole(long id, [FromBody] UpdateRoleRequest request)
    {
        var result = await _roleService.UpdateRoleAsync(id, request);

        var response = new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        };

        return StatusCode(result.StatusCode, response);
    }

    /// <summary>
    /// Role'ni o'chirish (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [HasPermission(Permissions.Roles.Delete)]
    public async Task<IActionResult> DeleteRole(long id)
    {
        var result = await _roleService.DeleteRoleAsync(id);

        var response = new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        };

        return StatusCode(result.StatusCode, response);
    }

    /// <summary>
    /// Role'ni aktivlashtirish/deaktivlashtirish
    /// </summary>
    [HttpPatch("{id}/status")]
    [HasPermission(Permissions.Roles.Update)]
    public async Task<IActionResult> ToggleRoleStatus(long id, [FromBody] ToggleStatusRequest request)
    {
        var result = await _roleService.ToggleRoleStatusAsync(id, request.IsActive);

        var response = new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        };

        return StatusCode(result.StatusCode, response);
    }
}

/// <summary>
/// Toggle status request DTO
/// </summary>
public class ToggleStatusRequest
{
    public bool IsActive { get; set; }
}
