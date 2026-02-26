using Convoy.Data.DbContexts;
using Convoy.Data.IRepositories;
using Convoy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Convoy.Data.Repositories;

/// <summary>
/// Kunlik masofa hisoboti repository - EF Core + Dapper (PostgreSQL functions)
/// </summary>
public class DailyDistanceReportRepository : Repository<DailyDistanceReport>, IDailyDistanceReportRepository
{
    private readonly AppDbConText _dbContext;
    private readonly NpgsqlConnection _connection;

    public DailyDistanceReportRepository(AppDbConText dbContext, NpgsqlConnection connection)
        : base(dbContext)
    {
        _dbContext = dbContext;
        _connection = connection;
    }

    /// <summary>
    /// Foydalanuvchining ma'lum sana uchun hisobotini olish
    /// </summary>
    public async Task<DailyDistanceReport?> GetByUserAndDateAsync(long userId, DateTime date)
    {
        var reportDate = date.Date; // Faqat sana qismini olish (vaqtni olib tashlash)

        return await _dbContext.DailyDistanceReports
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ReportDate.Date == reportDate);
    }

    /// <summary>
    /// Foydalanuvchining ma'lum sana oralig'idagi hisobotlarini olish
    /// </summary>
    public async Task<IList<DailyDistanceReport>> GetByUserAndDateRangeAsync(long userId, DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date;

        return await _dbContext.DailyDistanceReports
            .Include(r => r.User)
            .Where(r => r.UserId == userId && r.ReportDate.Date >= start && r.ReportDate.Date <= end)
            .OrderByDescending(r => r.ReportDate)
            .ToListAsync();
    }

    /// <summary>
    /// Barcha foydalanuvchilar uchun ma'lum sana oralig'idagi hisobotlarni olish
    /// </summary>
    public async Task<IList<DailyDistanceReport>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date;

        return await _dbContext.DailyDistanceReports
            .Include(r => r.User)
            .Where(r => r.ReportDate.Date >= start && r.ReportDate.Date <= end)
            .OrderByDescending(r => r.ReportDate)
            .ThenByDescending(r => r.TotalDistanceKm)
            .ToListAsync();
    }

    /// <summary>
    /// Ma'lum sana uchun eng ko'p masofa bosgan foydalanuvchilarni olish
    /// </summary>
    public async Task<IList<DailyDistanceReport>> GetTopDistancesByDateAsync(DateTime date, int topCount = 10)
    {
        var reportDate = date.Date;

        return await _dbContext.DailyDistanceReports
            .Include(r => r.User)
            .Where(r => r.ReportDate.Date == reportDate)
            .OrderByDescending(r => r.TotalDistanceKm)
            .Take(topCount)
            .ToListAsync();
    }

    /// <summary>
    /// Bitta foydalanuvchi uchun kunlik hisobotni yaratish yoki yangilash
    /// PostgreSQL function dan foydalanadi: upsert_daily_distance_report
    /// </summary>
    public async Task<long> UpsertDailyDistanceReportAsync(long userId, DateTime date)
    {
        var reportDate = date.Date;

        // PostgreSQL function ni chaqirish
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT upsert_daily_distance_report(@p_user_id, @p_date)";
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.Parameters.AddWithValue("p_date", reportDate);

        // Connection ochish (agar yopiq bo'lsa)
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    /// <summary>
    /// Barcha foydalanuvchilar uchun ma'lum sana uchun hisobotlarni yaratish
    /// PostgreSQL function dan foydalanadi: generate_daily_reports_for_date
    /// </summary>
    public async Task<IList<(long UserId, long ReportId, decimal DistanceKm)>> GenerateDailyReportsForDateAsync(DateTime date)
    {
        var reportDate = date.Date;
        var results = new List<(long, long, decimal)>();

        // PostgreSQL function ni chaqirish
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT user_id, report_id, distance_km FROM generate_daily_reports_for_date(@p_date)";
        cmd.Parameters.AddWithValue("p_date", reportDate);

        // Connection ochish (agar yopiq bo'lsa)
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var userId = reader.GetInt64(0);
            var reportId = reader.GetInt64(1);
            var distanceKm = reader.GetDecimal(2);
            results.Add((userId, reportId, distanceKm));
        }

        return results;
    }
}
