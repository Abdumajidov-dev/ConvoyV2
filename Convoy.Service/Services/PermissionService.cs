using Convoy.Data.DbContexts;
using Convoy.Domain.Entities;
using Convoy.Service.Common;
using Convoy.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services;

/// <summary>
/// Permission service implementation
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly AppDbConText _context;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(AppDbConText context, ILogger<PermissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> UserHasPermissionAsync(long userId, string permissionName)
    {
        try
        {
            // User'ning role'lari orqali permission'ini tekshirish
            var hasPermission = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.RolePermissions,
                    ur => ur.RoleId,
                    rp => rp.RoleId,
                    (ur, rp) => rp)
                .Join(_context.Permissions,
                    rp => rp.PermissionId,
                    p => p.Id,
                    (rp, p) => p)
                .AnyAsync(p => p.Name == permissionName && p.IsActive);

            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permissionName, userId);
            return false;
        }
    }

    public async Task<List<string>> GetUserPermissionsAsync(long userId)
    {
        try
        {
            // User'ning barcha permission'larini olish (role'lar orqali)
            var permissions = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.RolePermissions,
                    ur => ur.RoleId,
                    rp => rp.RoleId,
                    (ur, rp) => rp)
                .Join(_context.Permissions,
                    rp => rp.PermissionId,
                    p => p.Id,
                    (rp, p) => p)
                .Where(p => p.IsActive)
                .Select(p => p.Name)
                .Distinct()
                .ToListAsync();

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
            return new List<string>();
        }
    }

    public async Task<ServiceResult<UserRole>> AssignRoleToUserAsync(long userId, long roleId, long? assignedBy = null)
    {
        try
        {
            // User mavjudligini tekshirish
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult<UserRole>.NotFound("Foydalanuvchi topilmadi");

            // Role mavjudligini tekshirish
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
                return ServiceResult<UserRole>.NotFound("Rol topilmadi");

            // Allaqachon assign qilingan bo'lsa
            var existingUserRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (existingUserRole != null)
                return ServiceResult<UserRole>.BadRequest("Bu rol allaqachon foydalanuvchiga biriktirilgan");

            // Yangi UserRole yaratish
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = assignedBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Role {RoleId} assigned to user {UserId}", roleId, userId);

            return ServiceResult<UserRole>.Ok(userRole, "Rol muvaffaqiyatli biriktirildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", roleId, userId);
            return ServiceResult<UserRole>.ServerError("Rolni biriktirishda xatolik yuz berdi");
        }
    }

    public async Task<ServiceResult<bool>> RemoveRoleFromUserAsync(long userId, long roleId)
    {
        try
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null)
                return ServiceResult<bool>.NotFound("UserRole topilmadi");

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Role {RoleId} removed from user {UserId}", roleId, userId);

            return ServiceResult<bool>.Ok(true, "Rol muvaffaqiyatli olib tashlandi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
            return ServiceResult<bool>.ServerError("Rolni olib tashlashda xatolik yuz berdi");
        }
    }

    public async Task<ServiceResult<RolePermission>> AssignPermissionToRoleAsync(long roleId, long permissionId, long? grantedBy = null)
    {
        try
        {
            // Role mavjudligini tekshirish
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
                return ServiceResult<RolePermission>.NotFound("Rol topilmadi");

            // Permission mavjudligini tekshirish
            var permission = await _context.Permissions.FindAsync(permissionId);
            if (permission == null)
                return ServiceResult<RolePermission>.NotFound("Ruxsat topilmadi");

            // Allaqachon assign qilingan bo'lsa
            var existingRolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (existingRolePermission != null)
                return ServiceResult<RolePermission>.BadRequest("Bu ruxsat allaqachon rolga biriktirilgan");

            // Yangi RolePermission yaratish
            var rolePermission = new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = grantedBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.RolePermissions.Add(rolePermission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Permission {PermissionId} assigned to role {RoleId}", permissionId, roleId);

            return ServiceResult<RolePermission>.Ok(rolePermission, "Ruxsat muvaffaqiyatli biriktirildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission {PermissionId} to role {RoleId}", permissionId, roleId);
            return ServiceResult<RolePermission>.ServerError("Ruxsatni biriktirishda xatolik yuz berdi");
        }
    }

    public async Task<ServiceResult<bool>> RemovePermissionFromRoleAsync(long roleId, long permissionId)
    {
        try
        {
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (rolePermission == null)
                return ServiceResult<bool>.NotFound("RolePermission topilmadi");

            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Permission {PermissionId} removed from role {RoleId}", permissionId, roleId);

            return ServiceResult<bool>.Ok(true, "Ruxsat muvaffaqiyatli olib tashlandi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission {PermissionId} from role {RoleId}", permissionId, roleId);
            return ServiceResult<bool>.ServerError("Ruxsatni olib tashlashda xatolik yuz berdi");
        }
    }

    public async Task<ServiceResult<List<Role>>> GetAllRolesAsync()
    {
        try
        {
            var roles = await _context.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            return ServiceResult<List<Role>>.Ok(roles, "Rollar muvaffaqiyatli olindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all roles");
            return ServiceResult<List<Role>>.ServerError("Rollarni olishda xatolik yuz berdi");
        }
    }

    public async Task<ServiceResult<List<Permission>>> GetAllPermissionsAsync()
    {
        try
        {
            var permissions = await _context.Permissions
                .Where(p => p.IsActive)
                .OrderBy(p => p.Resource)
                .ThenBy(p => p.Action)
                .ToListAsync();

            return ServiceResult<List<Permission>>.Ok(permissions, "Ruxsatlar muvaffaqiyatli olindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all permissions");
            return ServiceResult<List<Permission>>.ServerError("Ruxsatlarni olishda xatolik yuz berdi");
        }
    }

    public async Task<ServiceResult<List<Permission>>> GetRolePermissionsAsync(long roleId)
    {
        try
        {
            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Join(_context.Permissions,
                    rp => rp.PermissionId,
                    p => p.Id,
                    (rp, p) => p)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Resource)
                .ThenBy(p => p.Action)
                .ToListAsync();

            return ServiceResult<List<Permission>>.Ok(permissions, "Rol ruxsatlari muvaffaqiyatli olindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for role {RoleId}", roleId);
            return ServiceResult<List<Permission>>.ServerError("Rol ruxsatlarini olishda xatolik yuz berdi");
        }
    }

    public async Task<ServiceResult<List<Role>>> GetUserRolesAsync(long userId)
    {
        try
        {
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r)
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            return ServiceResult<List<Role>>.Ok(roles, "Foydalanuvchi rollari muvaffaqiyatli olindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
            return ServiceResult<List<Role>>.ServerError("Foydalanuvchi rollarini olishda xatolik yuz berdi");
        }
    }
}
