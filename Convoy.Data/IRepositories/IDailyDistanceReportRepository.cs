using Convoy.Domain.Entities;

namespace Convoy.Data.IRepositories;

/// <summary>
/// Kunlik masofa hisoboti repository interface
/// </summary>
public interface IDailyDistanceReportRepository : IRepository<DailyDistanceReport>
{
    /// <summary>
    /// Foydalanuvchining ma'lum sana uchun hisobotini olish
    /// </summary>
    Task<DailyDistanceReport?> GetByUserAndDateAsync(long userId, DateTime date);

    /// <summary>
    /// Foydalanuvchining ma'lum sana oralig'idagi hisobotlarini olish
    /// </summary>
    Task<IList<DailyDistanceReport>> GetByUserAndDateRangeAsync(long userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Barcha foydalanuvchilar uchun ma'lum sana oralig'idagi hisobotlarni olish
    /// </summary>
    Task<IList<DailyDistanceReport>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Ma'lum sana uchun eng ko'p masofa bosgan foydalanuvchilarni olish
    /// </summary>
    Task<IList<DailyDistanceReport>> GetTopDistancesByDateAsync(DateTime date, int topCount = 10);

    /// <summary>
    /// Bitta foydalanuvchi uchun kunlik hisobotni yaratish yoki yangilash
    /// PostgreSQL function dan foydalanadi: upsert_daily_distance_report
    /// </summary>
    Task<long> UpsertDailyDistanceReportAsync(long userId, DateTime date);

    /// <summary>
    /// Barcha foydalanuvchilar uchun ma'lum sana uchun hisobotlarni yaratish
    /// PostgreSQL function dan foydalanadi: generate_daily_reports_for_date
    /// </summary>
    Task<IList<(long UserId, long ReportId, decimal DistanceKm)>> GenerateDailyReportsForDateAsync(DateTime date);
}
