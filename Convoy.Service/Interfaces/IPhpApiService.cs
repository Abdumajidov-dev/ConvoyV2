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

    /// <summary>
    /// PHP API dan filiallar ro'yxatini oladi (search bilan yoki search'siz)
    /// </summary>
    /// <param name="searchTerm">Qidiruv matni (optional)</param>
    /// <returns>Filiallar ro'yxati</returns>
    Task<List<BranchDto>> GetBranchesAsync(string? searchTerm = null);
}
