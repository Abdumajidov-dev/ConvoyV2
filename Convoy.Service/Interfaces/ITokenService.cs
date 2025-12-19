using Convoy.Service.DTOs;

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
}
