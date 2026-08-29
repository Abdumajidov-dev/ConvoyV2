using Convoy.Data.DbContexts;
using Convoy.Data.IRepositories;
using Convoy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Dapper;

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
    /// report_date - timestamptz ustuni. DateTime.Date Kind=Unspecified qaytaradi va
    /// Npgsql uni timestamptz bilan solishtira olmay exception tashlaydi
    /// ("Cannot write DateTime with Kind=Unspecified"). Shuning uchun kalendar
    /// sanani o'zgartirmasdan Kind=Utc deb belgilaymiz.
    /// </summary>
    private static DateTime ToUtcDate(DateTime value)
        => DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    /// <summary>
    /// Foydalanuvchining ma'lum sana uchun hisobotini olish
    /// IMPORTANT: userId = users.user_id (external PHP worker_id)
    /// </summary>
    public async Task<DailyDistanceReport?> GetByUserAndDateAsync(int userId, DateTime date)
    {
        var reportDate = ToUtcDate(date); // Faqat sana qismi, Kind=Utc

        // PostgreSQL DATE column bilan TO'G'RI taqqoslash
        // reportDate allaqachon .Date qilingan (line 32)
        // r.ReportDate database'da DATE tipida, shuning uchun to'g'ridan-to'g'ri taqqoslaymiz
        return await _dbContext.DailyDistanceReports
            .Where(r => r.UserId == userId)
            .Where(r => r.ReportDate.Date == reportDate.Date)  // FIX: .Date.Date emas, to'g'ri comparison
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Foydalanuvchining ma'lum sana oralig'idagi hisobotlarini olish
    /// IMPORTANT: userId = users.user_id (external PHP worker_id)
    /// </summary>
    public async Task<IList<DailyDistanceReport>> GetByUserAndDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        var start = ToUtcDate(startDate);
        var end = ToUtcDate(endDate);

        // FIX: PostgreSQL DATE column bilan to'g'ri taqqoslash
        return await _dbContext.DailyDistanceReports
            .Where(r => r.UserId == userId)
            .Where(r => r.ReportDate >= start && r.ReportDate <= end)
            .OrderByDescending(r => r.ReportDate)
            .ToListAsync();
    }

    /// <summary>
    /// Barcha foydalanuvchilar uchun ma'lum sana oralig'idagi hisobotlarni olish
    /// </summary>
    public async Task<IList<DailyDistanceReport>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
     {
        var start = ToUtcDate(startDate);
        var end = ToUtcDate(endDate);

        return await _dbContext.DailyDistanceReports
            .Where(r => r.ReportDate >= start && r.ReportDate <= end)
            .OrderByDescending(r => r.ReportDate)
            .ThenByDescending(r => r.TotalDistanceKm)
            .ToListAsync();
    }

    /// <summary>
    /// Ma'lum sana uchun eng ko'p masofa bosgan foydalanuvchilarni olish
    /// </summary>
    public async Task<IList<DailyDistanceReport>> GetTopDistancesByDateAsync(DateTime date, int topCount = 10)
    {
        var reportDate = ToUtcDate(date);

        return await _dbContext.DailyDistanceReports
            .Where(r => r.ReportDate == reportDate)
            .OrderByDescending(r => r.TotalDistanceKm)
            .Take(topCount)
            .ToListAsync();
    }

    /// <summary>
    /// Bitta foydalanuvchi uchun kunlik hisobotni yaratish yoki yangilash
    /// PostgreSQL function dan foydalanadi: upsert_daily_distance_report
    /// IMPORTANT: userId = users.user_id (external PHP worker_id)
    /// </summary>
    public async Task<long> UpsertDailyDistanceReportAsync(int userId, DateTime date)
    {
        var reportDate = ToUtcDate(date);

        // PostgreSQL function ni chaqirish
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT upsert_daily_distance_report(@p_user_id, @p_date)";
        cmd.Parameters.AddWithValue("p_user_id", userId);
        cmd.Parameters.Add(
                        new NpgsqlParameter("p_date", NpgsqlDbType.Date)
                        {
                            Value = reportDate.Date
                        });

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
    public async Task<IList<(int UserId, long ReportId, decimal DistanceKm)>> GenerateDailyReportsForDateAsync(DateTime date)
    {
        var reportDate = ToUtcDate(date);
        var results = new List<(int, long, decimal)>();

        // PostgreSQL function ni chaqirish
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT user_id, report_id, distance_km FROM generate_daily_reports_for_date(@p_date)";
        cmd.Parameters.Add(
            new NpgsqlParameter("p_date", NpgsqlDbType.Date)
            {
                Value = reportDate.Date
            });

        // Connection ochish (agar yopiq bo'lsa)
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var userId = reader.GetInt32(0);  // int'ga o'zgardi
            var reportId = reader.GetInt64(1);
            var distanceKm = reader.GetDecimal(2);
            results.Add((userId, reportId, distanceKm));
        }

        return results;
    }

    /// <summary>
    /// Hisobotlarni filter qilish (branch_guid, user_ids, date range)
    /// </summary>
    public async Task<IList<DailyDistanceReport>> GetFilteredReportsAsync(
        string? branchGuid,
        List<int>? userIds,
        DateTime startDate,
        DateTime endDate)
    {
        var start = ToUtcDate(startDate);
        var end = ToUtcDate(endDate);

        var query = _dbContext.DailyDistanceReports.AsQueryable();

        // Filter by branch_guid
        if (!string.IsNullOrWhiteSpace(branchGuid))
        {
            query = query.Where(r => r.BranchGuid == branchGuid);
        }

        // Filter by user_ids
        if (userIds != null && userIds.Any())
        {
            query = query.Where(r => r.UserId.HasValue && userIds.Contains(r.UserId.Value));
        }

        // Filter by date range
        query = query.Where(r => r.ReportDate >= start && r.ReportDate <= end);

        // Order by date descending, then by distance descending
        query = query.OrderByDescending(r => r.ReportDate)
                     .ThenByDescending(r => r.TotalDistanceKm);

        return await query.ToListAsync();
    }

    /// <summary>
    /// Foydalanuvchining ma'lum vaqt oralig'idagi locationlarini olish (real-time hisoblash uchun)
    /// Dapper orqali to'g'ridan-to'g'ri SQL query
    /// IMPORTANT: Faqat database'da mavjud bo'lgan columnlarni SELECT qilish kerak
    /// </summary>
    public async Task<IEnumerable<Location>> GetUserLocationsForDateAsync(int userId, DateTime startDate, DateTime endDate)
    {
        // NOTE: Bu query faqat database'da MAVJUD bo'lgan columnlarni oladi
        // Entity'da ko'proq propertylar bo'lishi mumkin, lekin database'da yo'q
        const string sql = @"
            SELECT
                id as Id,
                user_id as UserId,
                recorded_at as RecordedAt,
                latitude as Latitude,
                longitude as Longitude,
                accuracy as Accuracy,
                speed as Speed,
                heading as Heading,
                altitude as Altitude,
                activity_type as ActivityType,
                activity_confidence as ActivityConfidence,
                is_moving as IsMoving,
                battery_level as BatteryLevel,
                is_charging as IsCharging,
                distance_from_previous as DistanceFromPrevious,
                created_at as CreatedAt
            FROM locations
            WHERE user_id = @UserId
                AND recorded_at >= @StartDate
                AND recorded_at <= @EndDate
            ORDER BY recorded_at ASC";

        // Connection ochish (agar yopiq bo'lsa)
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        var locations = await _connection.QueryAsync<Location>(sql, new
        {
            UserId = userId,
            StartDate = startDate,
            EndDate = endDate
        });

        return locations;
    }
}
