using Convoy.Service.DTOs;

namespace Convoy.Service.Interfaces;

/// <summary>
/// User 1 soatdan ko'p to'xtab qolganlarni boshqarish servisi
/// </summary>
public interface IUserStoppedReportService
{
    /// <summary>
    /// Yangi stopped report yaratish
    /// </summary>
    Task<UserStoppedReportDto> CreateAsync(CreateUserStoppedReportDto dto);

    /// <summary>
    /// User uchun aktiv (resolved=false) stopped report borligini tekshirish
    /// </summary>
    Task<UserStoppedReportDto?> GetActiveStoppedReportAsync(int userId);

    /// <summary>
    /// User'ning stopped report'ini resolve qilish (harakat davom etganda)
    /// </summary>
    Task<bool> ResolveStoppedReportAsync(int userId);

    /// <summary>
    /// Sanaga qarab stopped reportlarni olish
    /// </summary>
    Task<List<UserStoppedReportDto>> GetStoppedReportsByDateAsync(DateTime date);

    /// <summary>
    /// User ID'ga qarab barcha stopped reportlarni olish
    /// </summary>
    Task<List<UserStoppedReportDto>> GetUserStoppedReportsAsync(int userId);

    /// <summary>
    /// Aktiv (resolved=false) stopped reportlar bo'lgan user ID'larni olish
    /// Filter uchun kerak
    /// </summary>
    Task<List<int>> GetUserIdsWithActiveStoppedReportsAsync();
}
