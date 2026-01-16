using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Convoy.Api.Authorization;

/// <summary>
/// Permission-based authorization handler
/// Foydalanuvchining permission'larini tekshiradi
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // User authenticated emas bo'lsa, fail
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("User is not authenticated");
            return;
        }

        // User ID ni olish
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)
                          ?? context.User.FindFirst("user_id");

        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid");
            return;
        }

        // Scoped service'dan foydalanish uchun scope yaratish
        using var scope = _serviceScopeFactory.CreateScope();
        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

        try
        {
            // Foydalanuvchining permission'ini tekshirish
            var hasPermission = await permissionService.UserHasPermissionAsync(userId, requirement.Permission);

            if (hasPermission)
            {
                _logger.LogInformation(
                    "User {UserId} has permission '{Permission}'",
                    userId, requirement.Permission);

                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning(
                    "User {UserId} does NOT have permission '{Permission}'",
                    userId, requirement.Permission);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission '{Permission}' for user {UserId}",
                requirement.Permission, userId);
        }
    }
}
