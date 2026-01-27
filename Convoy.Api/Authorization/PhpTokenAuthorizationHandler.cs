using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Convoy.Api.Authorization;

/// <summary>
/// PHP token'larni validate qiladigan custom authorization handler
/// .NET JWT validation o'rniga PHP token decode qiladi
/// </summary>
public class PhpTokenAuthorizationHandler : AuthorizationHandler<PhpTokenRequirement>
{
    private readonly IPhpTokenService _phpTokenService;
    private readonly ILogger<PhpTokenAuthorizationHandler> _logger;

    public PhpTokenAuthorizationHandler(
        IPhpTokenService phpTokenService,
        ILogger<PhpTokenAuthorizationHandler> logger)
    {
        _phpTokenService = phpTokenService;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PhpTokenRequirement requirement)
    {
        try
        {
            // HttpContext'dan token olish
            if (context.Resource is not HttpContext httpContext)
            {
                _logger.LogWarning("HttpContext not found in authorization context");
                return Task.CompletedTask;
            }

            // Authorization header'dan token olish
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Authorization header not found or invalid format");
                return Task.CompletedTask;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            // Token'ni decode qilish
            var phpUser = _phpTokenService.DecodeToken(token);
            if (phpUser == null || phpUser.WorkerId <= 0)
            {
                _logger.LogWarning("Failed to decode token or invalid worker_id");
                return Task.CompletedTask;
            }

            // Token muddatini tekshirish
            if (!_phpTokenService.IsTokenValid(token))
            {
                _logger.LogWarning("Token expired for worker_id={WorkerId}", phpUser.WorkerId);
                return Task.CompletedTask;
            }

            // Token valid - Claims yaratish (controller'da User.Claims orqali foydalanish uchun)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, phpUser.WorkerId.ToString()),
                new Claim("worker_id", phpUser.WorkerId.ToString()),
                new Claim("user_id", phpUser.UserId.ToString()),
                new Claim(ClaimTypes.Name, phpUser.Name),
                new Claim(ClaimTypes.MobilePhone, phpUser.Phone ?? string.Empty),
                new Claim("worker_guid", phpUser.WorkerGuid ?? string.Empty),
                new Claim("branch_guid", phpUser.FilialGuid ?? string.Empty),
                new Claim("branch_name", phpUser.FilialName ?? string.Empty),
                new Claim(ClaimTypes.Role, phpUser.Role ?? string.Empty),
                new Claim("user_type", phpUser.Type ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, "PhpToken");
            var principal = new ClaimsPrincipal(identity);

            // HttpContext.User'ni yangilash
            httpContext.User = principal;

            _logger.LogInformation("PHP token validated successfully for worker_id={WorkerId}", phpUser.WorkerId);

            // Authorization muvaffaqiyatli
            context.Succeed(requirement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PHP token");
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// PHP token requirement (bo'sh requirement, faqat marker)
/// </summary>
public class PhpTokenRequirement : IAuthorizationRequirement
{
    // Bo'sh requirement - faqat marker sifatida ishlatiladi
}
