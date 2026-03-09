using Convoy.Data.DbContexts;
using Convoy.Data.IRepositories;
using Convoy.Domain.Entities;
using Convoy.Service.Common;
using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services;

/// <summary>
/// Kunlik masofa hisoboti service implementation (YANGILANGAN)
/// </summary>
public class DailyDistanceReportService : IDailyDistanceReportService
{
    private readonly IDailyDistanceReportRepository _reportRepository;
    private readonly AppDbConText _dbContext;
    private readonly ILogger<DailyDistanceReportService> _logger;

    public DailyDistanceReportService(
        IDailyDistanceReportRepository reportRepository,
        AppDbConText dbContext,
        ILogger<DailyDistanceReportService> logger)
    {
        _reportRepository = reportRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Bitta foydalanuvchi uchun kunlik hisobotni yaratish yoki yangilash (TEST UCHUN)
    /// </summary>
    public async Task<ServiceResult<DailyDistanceReportDto>> GenerateDailyReportAsync(int userId, DateTime date)
    {
        try
        {
            date = date.ToUniversalTime();
            _logger.LogInformation("Generating daily report for external user_id {UserId} on {Date}", userId, date);

            // IMPORTANT: userId bu EXTERNAL ID (users.user_id - PHP worker_id)
            // PostgreSQL function ham EXTERNAL ID kutadi (endi!)
            // User'ni tekshirish uchun olish
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return ServiceResult<DailyDistanceReportDto>.NotFound(
                    $"User topilmadi: user_id={userId}");
            }

            _logger.LogInformation("📍 User found: Name={Name}, ExternalId={ExternalId}",
                user.Name, user.UserId);

            // PostgreSQL function orqali hisobot yaratish (EXTERNAL ID yuborish - DIRECT!)
            // Function: users.user_id={ExternalId} → locations.user_id={ExternalId}
            var reportId = await _reportRepository.UpsertDailyDistanceReportAsync(userId, date);
            _logger.LogInformation("📍 Report created with ID: {ReportId}", reportId);

            // Yaratilgan hisobotni olish (EXTERNAL ID bilan)
            var report = await _reportRepository.GetByUserAndDateAsync(userId, date);

            if (report == null)
            {
                return ServiceResult<DailyDistanceReportDto>.ServerError(
                    "Hisobot yaratildi lekin qayta o'qishda xatolik yuz berdi");
            }

            var dto = MapToDto(report, user);
            _logger.LogInformation("Daily report generated successfully for user {ExternalUserId}: {DistanceKm} km",
                userId, dto.TotalDistanceKm);

            return ServiceResult<DailyDistanceReportDto>.Ok(dto, "Hisobot muvaffaqiyatli yaratildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily report for user {UserId} on {Date}", userId, date);
            return ServiceResult<DailyDistanceReportDto>.ServerError("Hisobot yaratishda xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Barcha foydalanuvchilar uchun ma'lum sana uchun hisobotlarni yaratish (TEST UCHUN)
    /// </summary>
    public async Task<ServiceResult<List<DailyDistanceReportDto>>> GenerateDailyReportsForAllUsersAsync(DateTime date)
    {
        try
        {
            _logger.LogInformation("Generating daily reports for all users on {Date}", date);

            // PostgreSQL function orqali barcha hisobotlarni yaratish
            var results = await _reportRepository.GenerateDailyReportsForDateAsync(date);

            _logger.LogInformation("{Count} ta foydalanuvchi uchun hisobot yaratildi", results.Count);

            // Yaratilgan hisobotlarni olish
            var reports = await _reportRepository.GetByDateRangeAsync(date, date);

            // User ma'lumotlarini bir marta olish (performance uchun)
            // IMPORTANT: reports.UserId bu EXTERNAL ID (users.user_id) - UPDATED!
            var externalUserIds = reports.Select(r => r.UserId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            var users = await _dbContext.Users
                .Where(u => u.UserId.HasValue && externalUserIds.Contains(u.UserId.Value))
                .ToDictionaryAsync(u => u.UserId!.Value, u => u);

            var dtos = reports.Select(r => MapToDto(r, users.GetValueOrDefault(r.UserId ?? 0))).ToList();

            return ServiceResult<List<DailyDistanceReportDto>>.Ok(
                dtos,
                $"{dtos.Count} ta hisobot muvaffaqiyatli yaratildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily reports for all users on {Date}", date);
            return ServiceResult<List<DailyDistanceReportDto>>.ServerError(
                "Hisobotlarni yaratishda xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Hisobotlarni filter qilish (ASOSIY ENDPOINT - PRODUCTION UCHUN)
    /// </summary>
    public async Task<ServiceResult<List<DailyDistanceReportDto>>> GetFilteredReportsAsync(DailyDistanceReportFilterDto filter)
    {
        try
        {
            _logger.LogInformation("Getting filtered reports: BranchGuid={BranchGuid}, UserIds={UserIds}, StartDate={StartDate}, EndDate={EndDate}",
                filter.BranchGuid, string.Join(",", filter.UserIds ?? new List<int>()), filter.StartDate, filter.EndDate);

            // Repository orqali filter qilish
            var reports = await _reportRepository.GetFilteredReportsAsync(
                filter.BranchGuid,
                filter.UserIds,
                filter.StartDate,
                filter.EndDate);

            if (!reports.Any())
            {
                return ServiceResult<List<DailyDistanceReportDto>>.Ok(
                    new List<DailyDistanceReportDto>(),
                    "Hisobotlar topilmadi");
            }

            // User ma'lumotlarini bir marta olish (performance uchun)
            // IMPORTANT: reports.UserId bu EXTERNAL ID (users.user_id) - UPDATED!
            var externalUserIds = reports.Select(r => r.UserId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            var users = await _dbContext.Users
                .Where(u => u.UserId.HasValue && externalUserIds.Contains(u.UserId.Value))
                .ToDictionaryAsync(u => u.UserId!.Value, u => u);

            var dtos = reports.Select(r => MapToDto(r, users.GetValueOrDefault(r.UserId ?? 0))).ToList();

            // If date range is more than 1 day, group by user
            if (filter.StartDate.Date != filter.EndDate.Date)
            {
                _logger.LogInformation("Multi-day range detected ({StartDate} to {EndDate}). Grouping results by user.", filter.StartDate.Date, filter.EndDate.Date);
                dtos = AggregateReportsByUser(dtos);
            }

            _logger.LogInformation("Found {Count} reports (after grouping if applicable)", dtos.Count);

            return ServiceResult<List<DailyDistanceReportDto>>.Ok(
                dtos,
                $"{dtos.Count} ta hisobot topildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered reports");
            return ServiceResult<List<DailyDistanceReportDto>>.ServerError("Xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Bugungi kun hozirgi vaqtgacha bo'lgan masofani hisoblash (REAL-TIME TEST UCHUN)
    /// Database'ga saqlanmaydi, faqat hisoblangan natijani qaytaradi
    /// </summary>
    public async Task<ServiceResult<DailyDistanceReportDto>> CalculateCurrentDayDistanceAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Calculating current day distance for user {UserId} up to now", userId);

            // Bugungi kun boshidan hozirgi vaqtgacha bo'lgan locationlarni olish
            var startOfDay = DateTime.Today; // Local vaqt
            var endOfDay = DateTime.Now;     // Hozirgi vaqt

            _logger.LogInformation("Start: {Start}, End: {End}", startOfDay, endOfDay);

            // LocationRepository orqali locationlarni olish (Dapper)
            var locations = (await _reportRepository.GetUserLocationsForDateAsync(userId, startOfDay, endOfDay)).ToList();

            if (!locations.Any())
            {
                _logger.LogWarning("User {UserId} uchun bugungi locationlar topilmadi", userId);
                return ServiceResult<DailyDistanceReportDto>.Ok(
                    new DailyDistanceReportDto
                    {
                        UserId = userId,
                        ReportDate = startOfDay,
                        TotalDistanceMeters = 0,
                        TotalDistanceKm = 0,
                        LocationCount = 0,
                        FirstLocationTime = null,
                        LastLocationTime = null
                    },
                    "Bugungi locationlar topilmadi");
            }

            // Masofani hisoblash (distance_from_previous yig'indisi)
            var totalDistanceMeters = locations
                .Where(l => l.DistanceFromPrevious.HasValue)
                .Sum(l => l.DistanceFromPrevious!.Value);

            var firstLocation = locations.OrderBy(l => l.RecordedAt).First();
            var lastLocation = locations.OrderByDescending(l => l.RecordedAt).First();

            // User ma'lumotlarini olish
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            var dto = new DailyDistanceReportDto
            {
                Id = 0, // Temporary ID (database'da yo'q)
                UserId = userId,
                UserName = user?.Name,
                Phone = user?.Phone,
                UserImage = user?.Image,
                BranchGuid = user?.BranchGuid,
                BranchName = user?.BranchName,
                ReportDate = startOfDay,
                TotalDistanceMeters = totalDistanceMeters,
                TotalDistanceKm = Math.Round(totalDistanceMeters / 1000, 2),
                LocationCount = locations.Count,
                FirstLocationTime = firstLocation.RecordedAt,
                LastLocationTime = lastLocation.RecordedAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "✅ User {UserId} ({UserName}) - Bugungi masofa: {DistanceKm} km ({DistanceM} m), Locationlar: {Count}, " +
                "Birinchi: {First}, Oxirgi: {Last}",
                userId, user?.Name, dto.TotalDistanceKm, dto.TotalDistanceMeters, dto.LocationCount,
                dto.FirstLocationTime, dto.LastLocationTime);

            return ServiceResult<DailyDistanceReportDto>.Ok(
                dto,
                $"Hozirgi vaqtgacha {dto.TotalDistanceKm} km masofa bosilgan");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating current day distance for user {UserId}", userId);
            return ServiceResult<DailyDistanceReportDto>.ServerError("Masofani hisoblashda xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Entity ni DTO ga mapping qilish (user ma'lumotlari bilan)
    /// </summary>
    private DailyDistanceReportDto MapToDto(DailyDistanceReport report, User? user)
    {
        return new DailyDistanceReportDto
        {
            Id = report.Id,
            // SIMPLE: report.UserId allaqachon EXTERNAL ID (users.user_id)
            UserId = report.UserId,  // EXTERNAL ID (PHP worker_id)
            UserName = user?.Name,
            Phone = user?.Phone,
            UserImage = user?.Image,
            BranchGuid = report.BranchGuid ?? user?.BranchGuid,
            BranchName = user?.BranchName,
            ReportDate = report.ReportDate,
            TotalDistanceMeters = report.TotalDistanceMeters,
            TotalDistanceKm = report.TotalDistanceKm,
            LocationCount = report.LocationCount,
            FirstLocationTime = report.FirstLocationTime,
            LastLocationTime = report.LastLocationTime,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    /// <summary>
    /// Bir necha kunlik hisobotlarni user bo'yicha guruhlash va hisoblash
    /// </summary>
    private List<DailyDistanceReportDto> AggregateReportsByUser(List<DailyDistanceReportDto> reports)
    {
        return reports
            .GroupBy(r => r.UserId)
            .Select(g => new DailyDistanceReportDto
            {
                Id = 0, // Grouped result doesn't represent a single DB record
                UserId = g.Key,
                UserName = g.First().UserName,
                Phone = g.First().Phone,
                UserImage = g.First().UserImage,
                BranchGuid = g.First().BranchGuid,
                BranchName = g.First().BranchName,
                ReportDate = g.Min(r => r.ReportDate), // Start date of the period
                TotalDistanceMeters = g.Sum(r => r.TotalDistanceMeters),
                TotalDistanceKm = g.Sum(r => r.TotalDistanceKm),
                LocationCount = g.Sum(r => r.LocationCount),
                FirstLocationTime = g.Min(r => r.FirstLocationTime),
                LastLocationTime = g.Max(r => r.LastLocationTime),
                CreatedAt = g.Min(r => r.CreatedAt),
                UpdatedAt = g.Max(r => r.UpdatedAt)
            })
            .OrderByDescending(r => r.TotalDistanceKm)
            .ToList();
    }
}
