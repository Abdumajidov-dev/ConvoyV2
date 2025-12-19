using Convoy.Service.DTOs;

namespace Convoy.Service.Interfaces;

/// <summary>
/// PHP API bilan ishlash uchun interfeys
/// </summary>
public interface IPhpApiService
{
    /// <summary>
    /// Telefon raqam orqali userni PHP API dan tekshiradi
    /// </summary>
    /// <param name="phoneNumber">Telefon raqam</param>
    /// <returns>PHP Worker ma'lumotlari yoki null</returns>
    Task<PhpWorkerDto?> VerifyUserAsync(string phoneNumber);
}
