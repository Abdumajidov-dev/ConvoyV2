using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Convoy.Api.Authorization;

/// <summary>
/// PHP token'larni authenticate qiladigan custom authentication handler
/// .NET JWT authentication o'rniga PHP token decode qiladi
/// </summary>
public class PhpTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IPhpTokenService _phpTokenService;

    public PhpTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IPhpTokenService phpTokenService)
        : base(options, logger, encoder)
    {
        _phpTokenService = phpTokenService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // Authorization header'dan token olish
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                return Task.FromResult(AuthenticateResult.Fail("Authorization header not found"));
            }

            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid authorization header format"));
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            // Token'ni decode qilish
            var phpUser = _phpTokenService.DecodeToken(token);
            if (phpUser == null || phpUser.WorkerId <= 0)
            {
                Logger.LogWarning("Failed to decode token or invalid worker_id");
                return Task.FromResult(AuthenticateResult.Fail("Invalid token"));
            }

            // Token muddatini tekshirish
            if (!_phpTokenService.IsTokenValid(token))
            {
                Logger.LogWarning("Token expired for worker_id={WorkerId}", phpUser.WorkerId);
                return Task.FromResult(AuthenticateResult.Fail("Token expired"));
            }

            // Claims yaratish (Controller'da User.Claims orqali foydalanish uchun)
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

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            Logger.LogInformation("PHP token authenticated successfully for worker_id={WorkerId}", phpUser.WorkerId);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error authenticating PHP token");
            return Task.FromResult(AuthenticateResult.Fail($"Authentication error: {ex.Message}"));
        }
    }
}
