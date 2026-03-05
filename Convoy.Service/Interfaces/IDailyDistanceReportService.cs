using Convoy.Service.DTOs;
using Convoy.Service.Common;

namespace Convoy.Service.Interfaces;

/// <summary>
/// Kunlik masofa hisoboti service interface
/// </summary>
public interface IDailyDistanceReportService
{
    /// <summary>
    /// Bitta foydalanuvchi uchun kunlik hisobotni yaratish yoki yangilash (TEST UCHUN)
    /// </summary>
    Task<ServiceResult<DailyDistanceReportDto>> GenerateDailyReportAsync(int userId, DateTime date);

    /// <summary>
    /// Barcha foydalanuvchilar uchun ma'lum sana uchun hisobotlarni yaratish (TEST UCHUN)
    /// </summary>
    Task<ServiceResult<List<DailyDistanceReportDto>>> GenerateDailyReportsForAllUsersAsync(DateTime date);

    /// <summary>
    /// Hisobotlarni filter qilish (asosiy endpoint - Production uchun)
    /// </summary>
    Task<ServiceResult<List<DailyDistanceReportDto>>> GetFilteredReportsAsync(DailyDistanceReportFilterDto filter);

    /// <summary>
    /// Bugungi kun hozirgi vaqtgacha bo'lgan masofani hisoblash (REAL-TIME TEST UCHUN)
    /// Database'ga saqlanmaydi, faqat hisoblangan natijani qaytaradi
    /// </summary>
    Task<ServiceResult<DailyDistanceReportDto>> CalculateCurrentDayDistanceAsync(int userId);
}
