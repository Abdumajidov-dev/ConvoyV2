using Convoy.Data.DbContexts;
using Convoy.Domain.Entities;
using Convoy.Service.Common;
using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services;

/// <summary>
/// Role CRUD service implementation
/// </summary>
public class RoleService : IRoleService
{
    private readonly AppDbConText _context;
    private readonly ILogger<RoleService> _logger;

    public RoleService(AppDbConText context, ILogger<RoleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<List<RoleResponseDto>>> GetAllRolesAsync()
    {
        try
        {
            var roles = await _context.Roles
                .Where(r => r.DeletedAt == null)
                .OrderBy(r => r.Name)
                .Select(r => new RoleResponseDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    DisplayName = r.DisplayName,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToListAsync();

            return ServiceResult<List<RoleResponseDto>>.Ok(roles, "Rollar muvaffaqiyatli olindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all roles");
            return ServiceResult<List<RoleResponseDto>>.ServerError("Rollarni olishda xatolik yuz berdi");
        }
    }

    public async Task<ServiceResult<RoleResponseDto>> GetRoleByIdAsync(long roleId)
    {
        try
        {
            var role = await _context.Roles
                .Where(r => r.Id == roleId && r.DeletedAt == null)
                .Select(r => new RoleResponseDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    DisplayName = r.DisplayName,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (role == null)
                return ServiceResult<RoleResponseDto>.NotFound("Rol topilmadi");

            return ServiceResult<RoleResponseDto>.Ok(role, "Rol muvaffaqiyatli olindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role {RoleId}", roleId);
            return ServiceResult<RoleResponseDto>.ServerError("Rolni olishda xatolik yuz berdi");
        }
    }

    public async Task<ServiceResult<RoleWithPermissionsDto>> GetRoleWithPermissionsAsync(long roleId)
    {
        try
        {
            var role = await _context.Roles
                .Where(r => r.Id == roleId && r.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (role == null)
                return ServiceResult<RoleWithPermissionsDto>.NotFound("Rol topilmadi");

            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Join(_context.Permissions,
                    rp => rp.PermissionId,
                    p => p.Id,
                    (rp, p) => p)
                .Where(p => p.IsActive)
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    DisplayName = p.DisplayName,
                    Resource = p.Resource,
                    Action = p.Action,
                    Description = p.Description
                })
                .ToListAsync();

            var result = new RoleWithPermissionsDto
            {
                Id = role.Id,
                Name = role.Name,
                DisplayName = role.DisplayName,
                Description = role.Description,
                IsActive = role.IsActive,
                Permissions = permissions,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt
            };

            return ServiceResult<RoleWithPermissionsDto>.Ok(result, "Rol va ruxsatlar muvaffaqiyatli olindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role with permissions {RoleId}", roleId);
            return ServiceResult<RoleWithPermissionsDto>.ServerError("Rol va ruxsatlarni olishda xatolik yuz berdi");
        }
    }

    public async Task<ServiceResult<RoleResponseDto>> CreateRoleAsync(CreateRoleRequest request)
    {
        try
        {
            // Role nomi unique bo'lishi kerak
            var existingRole = await _context.Roles
                .Where(r => r.Name == request.Name && r.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (existingRole != null)
                return ServiceResult<RoleResponseDto>.BadRequest($"'{request.Name}' nomli rol allaqachon mavjud");

            var role = new Role
            {
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = request.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            var response = new RoleResponseDto
            {
                Id = role.Id,
                Name = role.Name,
                DisplayName = role.DisplayName,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt
            };

            _logger.LogInformation("Created new role: {RoleName} (ID: {RoleId})", role.Name, role.Id);

            return ServiceResult<RoleResponseDto>.Ok(response, "Rol muvaffaqiyatli yaratildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role {RoleName}", request.Name);
            return ServiceResult<RoleResponseDto>.ServerError("Rol yaratishda xatolik yuz berdi");
        }
    }

    public async Task<ServiceResult<RoleResponseDto>> UpdateRoleAsync(long roleId, UpdateRoleRequest request)
    {
        try
        {
            var role = await _context.Roles
                .Where(r => r.Id == roleId && r.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (role == null)
                return ServiceResult<RoleResponseDto>.NotFound("Rol topilmadi");

            bool updated = false;

            if (!string.IsNullOrEmpty(request.DisplayName) && role.DisplayName != request.DisplayName)
            {
                role.DisplayName = request.DisplayName;
                updated = true;
            }

            if (request.Description != null && role.Description != request.Description)
            {
                role.Description = request.Description;
                updated = true;
            }

            if (request.IsActive.HasValue && role.IsActive != request.IsActive.Value)
            {
                role.IsActive = request.IsActive.Value;
                updated = true;
            }

            if (updated)
            {
                role.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated role {RoleName} (ID: {RoleId})", role.Name, role.Id);
            }

            var response = new RoleResponseDto
            {
                Id = role.Id,
                Name = role.Name,
                DisplayName = role.DisplayName,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt
            };

            return ServiceResult<RoleResponseDto>.Ok(response, "Rol muvaffaqiyatli yangilandi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleId}", roleId);
            return ServiceResult<RoleResponseDto>.ServerError("Rolni yangilashda xatolik yuz berdi");
        }
    }

    public async Task<ServiceResult<bool>> DeleteRoleAsync(long roleId)
    {
        try
        {
            var role = await _context.Roles
                .Where(r => r.Id == roleId && r.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (role == null)
                return ServiceResult<bool>.NotFound("Rol topilmadi");

            // Check if role has users
            var hasUsers = await _context.UserRoles
                .AnyAsync(ur => ur.RoleId == roleId);

            if (hasUsers)
                return ServiceResult<bool>.BadRequest("Ushbu rolga tayinlangan foydalanuvchilar mavjud. Avval ularni olib tashlang.");

            // Soft delete
            role.DeletedAt = DateTime.UtcNow;
            role.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Soft deleted role {RoleName} (ID: {RoleId})", role.Name, role.Id);

            return ServiceResult<bool>.Ok(true, "Rol muvaffaqiyatli o'chirildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId}", roleId);
            return ServiceResult<bool>.ServerError("Rolni o'chirishda xatolik yuz berdi");
        }
    }

    public async Task<ServiceResult<bool>> ToggleRoleStatusAsync(long roleId, bool isActive)
    {
        try
        {
            var role = await _context.Roles
                .Where(r => r.Id == roleId && r.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (role == null)
                return ServiceResult<bool>.NotFound("Rol topilmadi");

            role.IsActive = isActive;
            role.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var status = isActive ? "aktivlashtirildi" : "deaktivlashtirildi";
            _logger.LogInformation("Role {RoleName} (ID: {RoleId}) {Status}", role.Name, role.Id, status);

            return ServiceResult<bool>.Ok(true, $"Rol muvaffaqiyatli {status}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling role status {RoleId}", roleId);
            return ServiceResult<bool>.ServerError("Rol statusini o'zgartirishda xatolik yuz berdi");
        }
    }
}
