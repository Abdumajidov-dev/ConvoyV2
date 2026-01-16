using Convoy.Service.DTOs;
using System.Security.Claims;

namespace Convoy.Service.Interfaces;

/// <summary>
/// JWT token yaratish va validatsiya qilish uchun interfeys
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Worker ma'lumotlari asosida JWT token yaratadi
    /// </summary>
    /// <param name="worker">Worker ma'lumotlari</param>
    /// <returns>JWT token string</returns>
    string GenerateToken(PhpWorkerDto worker);

    /// <summary>
    /// Tokenni validate qiladi va worker ID qaytaradi
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Worker ID yoki null</returns>
    int? ValidateToken(string token);

    /// <summary>
    /// ClaimsPrincipal'dan user ID ni oladi
    /// </summary>
    /// <param name="user">ClaimsPrincipal (Controller.User)</param>
    /// <returns>User ID yoki null</returns>
    long? GetUserIdFromClaims(ClaimsPrincipal user);

    /// <summary>
    /// JWT token'dan JTI (JWT ID) claim'ini oladi
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>JTI yoki null</returns>
    string? GetJtiFromToken(string token);

    /// <summary>
    /// JWT token'dan expiry time'ni oladi
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Expiry time yoki null</returns>
    DateTime? GetExpiryFromToken(string token);

    /// <summary>
    /// Token blacklist'da ekanligini tekshiradi
    /// </summary>
    /// <param name="jti">JWT ID</param>
    /// <returns>True agar blacklisted bo'lsa</returns>
    Task<bool> IsTokenBlacklistedAsync(string jti);

    /// <summary>
    /// Tokenni blacklist'ga qo'shadi (logout)
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <param name="userId">User ID</param>
    /// <param name="reason">Blacklist qilish sababi</param>
    /// <returns>True agar muvaffaqiyatli bo'lsa</returns>
    Task<bool> BlacklistTokenAsync(string token, long userId, string reason = "logout");
}
