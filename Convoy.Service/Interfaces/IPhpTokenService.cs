using Convoy.Service.DTOs;

namespace Convoy.Service.Interfaces;

/// <summary>
/// PHP JWT token'larni decode qilish va validate qilish uchun service
/// </summary>
public interface IPhpTokenService
{
    /// <summary>
    /// JWT token'ni decode qilib, ichidagi user ma'lumotlarini oladi
    /// Signature validate qilmaydi (faqat decode qiladi)
    /// </summary>
    /// <param name="token">JWT token (Bearer prefix'siz)</param>
    /// <returns>Token ichidagi user ma'lumotlari yoki null</returns>
    PhpUserDto? DecodeToken(string token);

    /// <summary>
    /// Token'ning amal qilish muddatini tekshiradi
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Token amal qilmoqda (true) yoki muddati tugagan (false)</returns>
    bool IsTokenValid(string token);

    /// <summary>
    /// Token'dan worker_id ni oladi
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Worker ID yoki null</returns>
    int? GetWorkerIdFromToken(string token);
}
