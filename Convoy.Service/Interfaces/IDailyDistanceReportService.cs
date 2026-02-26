using Convoy.Service.DTOs;
using Convoy.Service.Common;

namespace Convoy.Service.Interfaces;

/// <summary>
/// Kunlik masofa hisoboti service interface
/// </summary>
public interface IDailyDistanceReportService
{
    /// <summary>
    /// Foydalanuvchining ma'lum sana uchun hisobotini olish
    /// </summary>
    Task<ServiceResult<DailyDistanceReportDto>> GetByUserAndDateAsync(long userId, DateTime date);

    /// <summary>
    /// Foydalanuvchining ma'lum sana oralig'idagi hisobotlarini olish
    /// </summary>
    Task<ServiceResult<List<DailyDistanceReportDto>>> GetByUserAndDateRangeAsync(long userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Barcha foydalanuvchilar uchun ma'lum sana oralig'idagi hisobotlarni olish
    /// </summary>
    Task<ServiceResult<List<DailyDistanceReportDto>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Ma'lum sana uchun eng ko'p masofa bosgan foydalanuvchilarni olish
    /// </summary>
    Task<ServiceResult<List<DailyDistanceReportDto>>> GetTopDistancesByDateAsync(DateTime date, int topCount = 10);

    /// <summary>
    /// Bitta foydalanuvchi uchun kunlik hisobotni yaratish yoki yangilash
    /// </summary>
    Task<ServiceResult<DailyDistanceReportDto>> GenerateDailyReportAsync(long userId, DateTime date);

    /// <summary>
    /// Barcha foydalanuvchilar uchun ma'lum sana uchun hisobotlarni yaratish
    /// </summary>
    Task<ServiceResult<List<DailyDistanceReportDto>>> GenerateDailyReportsForAllUsersAsync(DateTime date);

    /// <summary>
    /// Kunlik statistikani olish
    /// </summary>
    Task<ServiceResult<DailyDistanceStatisticsDto>> GetDailyStatisticsAsync(DateTime date);

    /// <summary>
    /// Foydalanuvchi umumiy statistikasini olish
    /// </summary>
    Task<ServiceResult<UserDistanceSummaryDto>> GetUserSummaryAsync(long userId, DateTime startDate, DateTime endDate);
}
