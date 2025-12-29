using Convoy.Data.DbContexts;
using Convoy.Domain.Constants;
using Convoy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services;

/// <summary>
/// Permission sistemasi uchun seed data yaratish
/// Dastlabki role va permission'larni database'ga qo'shadi
/// </summary>
public class PermissionSeedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PermissionSeedService> _logger;

    public PermissionSeedService(
        IServiceProvider serviceProvider,
        ILogger<PermissionSeedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🌱 Permission seed service started");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbConText>();

        try
        {
            // Database connectivity'ni tekshirish
            await context.Database.CanConnectAsync(cancellationToken);

            // Permissions'larni seed qilish
            await SeedPermissionsAsync(context, cancellationToken);

            // Roles'larni seed qilish (permissions bilan bog'lamay)
            await SeedRolesAsync(context, cancellationToken);

            _logger.LogInformation("✅ Permission seed completed successfully");
            _logger.LogInformation("ℹ️  Role-Permission assignments should be done via Admin Panel");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error seeding permission data");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Permission seed service stopped");
        return Task.CompletedTask;
    }

    private async Task SeedPermissionsAsync(AppDbConText context, CancellationToken cancellationToken)
    {
        try
        {
            var allPermissions = Permissions.GetAll();

            foreach (var (name, displayName, resource, action, description) in allPermissions)
            {
                // Mavjud permission'ni tekshirish
                var existingPermission = await context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == name, cancellationToken);

                if (existingPermission == null)
                {
                    // Yangi permission yaratish
                    var permission = new Permission
                    {
                        Name = name,
                        DisplayName = displayName,
                        Resource = resource,
                        Action = action,
                        Description = description,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Permissions.Add(permission);
                    _logger.LogInformation("➕ Created permission: {Name}", name);
                }
                else
                {
                    // Mavjud permission'ni yangilash (agar o'zgargan bo'lsa)
                    bool updated = false;

                    if (existingPermission.DisplayName != displayName)
                    {
                        existingPermission.DisplayName = displayName;
                        updated = true;
                    }

                    if (existingPermission.Description != description)
                    {
                        existingPermission.Description = description;
                        updated = true;
                    }

                    if (updated)
                    {
                        existingPermission.UpdatedAt = DateTime.UtcNow;
                        _logger.LogInformation("🔄 Updated permission: {Name}", name);
                    }
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("✅ Permissions seeded: {Count} permissions", allPermissions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding permissions");
            throw;
        }
    }

    private async Task SeedRolesAsync(AppDbConText context, CancellationToken cancellationToken)
    {
        try
        {
            var allRoles = Roles.GetAll();

            foreach (var (name, displayName, description) in allRoles)
            {
                // Mavjud role'ni tekshirish
                var existingRole = await context.Roles
                    .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

                if (existingRole == null)
                {
                    // Yangi role yaratish
                    var role = new Role
                    {
                        Name = name,
                        DisplayName = displayName,
                        Description = description,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Roles.Add(role);
                    _logger.LogInformation("➕ Created role: {Name}", name);
                }
                else
                {
                    // Mavjud role'ni yangilash (agar o'zgargan bo'lsa)
                    bool updated = false;

                    if (existingRole.DisplayName != displayName)
                    {
                        existingRole.DisplayName = displayName;
                        updated = true;
                    }

                    if (existingRole.Description != description)
                    {
                        existingRole.Description = description;
                        updated = true;
                    }

                    if (updated)
                    {
                        existingRole.UpdatedAt = DateTime.UtcNow;
                        _logger.LogInformation("🔄 Updated role: {Name}", name);
                    }
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("✅ Roles seeded: {Count} roles (NO permissions assigned)", allRoles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding roles");
            throw;
        }
    }

}
