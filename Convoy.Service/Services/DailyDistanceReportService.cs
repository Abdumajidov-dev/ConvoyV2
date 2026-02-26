using Convoy.Data.IRepositories;
using Convoy.Domain.Entities;
using Convoy.Service.Common;
using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services;

/// <summary>
/// Kunlik masofa hisoboti service implementation
/// </summary>
public class DailyDistanceReportService : IDailyDistanceReportService
{
    private readonly IDailyDistanceReportRepository _reportRepository;
    private readonly ILogger<DailyDistanceReportService> _logger;

    public DailyDistanceReportService(
        IDailyDistanceReportRepository reportRepository,
        ILogger<DailyDistanceReportService> logger)
    {
        _reportRepository = reportRepository;
        _logger = logger;
    }

    /// <summary>
    /// Foydalanuvchining ma'lum sana uchun hisobotini olish
    /// </summary>
    public async Task<ServiceResult<DailyDistanceReportDto>> GetByUserAndDateAsync(long userId, DateTime date)
    {
        try
        {
            var report = await _reportRepository.GetByUserAndDateAsync(userId, date);

            if (report == null)
            {
                return ServiceResult<DailyDistanceReportDto>.NotFound(
                    $"User {userId} uchun {date:yyyy-MM-dd} sanasi uchun hisobot topilmadi");
            }

            var dto = MapToDto(report);
            return ServiceResult<DailyDistanceReportDto>.Ok(dto, "Hisobot muvaffaqiyatli olindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily report for user {UserId} on {Date}", userId, date);
            return ServiceResult<DailyDistanceReportDto>.ServerError("Xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Foydalanuvchining ma'lum sana oralig'idagi hisobotlarini olish
    /// </summary>
    public async Task<ServiceResult<List<DailyDistanceReportDto>>> GetByUserAndDateRangeAsync(
        long userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var reports = await _reportRepository.GetByUserAndDateRangeAsync(userId, startDate, endDate);

            var dtos = reports.Select(MapToDto).ToList();
            return ServiceResult<List<DailyDistanceReportDto>>.Ok(
                dtos,
                $"{dtos.Count} ta hisobot topildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily reports for user {UserId} from {StartDate} to {EndDate}",
                userId, startDate, endDate);
            return ServiceResult<List<DailyDistanceReportDto>>.ServerError("Xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Barcha foydalanuvchilar uchun ma'lum sana oralig'idagi hisobotlarni olish
    /// </summary>
    public async Task<ServiceResult<List<DailyDistanceReportDto>>> GetByDateRangeAsync(
        DateTime startDate, DateTime endDate)
    {
        try
        {
            var reports = await _reportRepository.GetByDateRangeAsync(startDate, endDate);

            var dtos = reports.Select(MapToDto).ToList();
            return ServiceResult<List<DailyDistanceReportDto>>.Ok(
                dtos,
                $"{dtos.Count} ta hisobot topildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily reports from {StartDate} to {EndDate}",
                startDate, endDate);
            return ServiceResult<List<DailyDistanceReportDto>>.ServerError("Xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Ma'lum sana uchun eng ko'p masofa bosgan foydalanuvchilarni olish
    /// </summary>
    public async Task<ServiceResult<List<DailyDistanceReportDto>>> GetTopDistancesByDateAsync(
        DateTime date, int topCount = 10)
    {
        try
        {
            var reports = await _reportRepository.GetTopDistancesByDateAsync(date, topCount);

            var dtos = reports.Select(MapToDto).ToList();
            return ServiceResult<List<DailyDistanceReportDto>>.Ok(
                dtos,
                $"Top {dtos.Count} foydalanuvchi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top distances for {Date}", date);
            return ServiceResult<List<DailyDistanceReportDto>>.ServerError("Xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Bitta foydalanuvchi uchun kunlik hisobotni yaratish yoki yangilash
    /// </summary>
    public async Task<ServiceResult<DailyDistanceReportDto>> GenerateDailyReportAsync(long userId, DateTime date)
    {
        try
        {
            _logger.LogInformation("Generating daily report for user {UserId} on {Date}", userId, date);

            // PostgreSQL function orqali hisobot yaratish
            var reportId = await _reportRepository.UpsertDailyDistanceReportAsync(userId, date);

            // Yaratilgan hisobotni olish
            var report = await _reportRepository.GetByUserAndDateAsync(userId, date);

            if (report == null)
            {
                return ServiceResult<DailyDistanceReportDto>.ServerError(
                    "Hisobot yaratildi lekin qayta o'qishda xatolik yuz berdi");
            }

            var dto = MapToDto(report);
            _logger.LogInformation("Daily report generated successfully for user {UserId}: {DistanceKm} km",
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
    /// Barcha foydalanuvchilar uchun ma'lum sana uchun hisobotlarni yaratish
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
            var dtos = reports.Select(MapToDto).ToList();

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
    /// Kunlik statistikani olish
    /// </summary>
    public async Task<ServiceResult<DailyDistanceStatisticsDto>> GetDailyStatisticsAsync(DateTime date)
    {
        try
        {
            var reports = await _reportRepository.GetByDateRangeAsync(date, date);

            if (!reports.Any())
            {
                return ServiceResult<DailyDistanceStatisticsDto>.NotFound(
                    $"{date:yyyy-MM-dd} sanasi uchun hisobotlar topilmadi");
            }

            var statistics = new DailyDistanceStatisticsDto
            {
                Date = date,
                TotalUsers = reports.Count,
                TotalDistanceKm = reports.Sum(r => r.TotalDistanceKm),
                AverageDistanceKm = reports.Average(r => r.TotalDistanceKm),
                MaxDistanceKm = reports.Max(r => r.TotalDistanceKm),
                MinDistanceKm = reports.Min(r => r.TotalDistanceKm)
            };

            var topUser = reports.OrderByDescending(r => r.TotalDistanceKm).FirstOrDefault();
            if (topUser?.User != null)
            {
                statistics.TopUserName = topUser.User.Name;
                statistics.TopUserDistanceKm = topUser.TotalDistanceKm;
            }

            return ServiceResult<DailyDistanceStatisticsDto>.Ok(
                statistics,
                "Statistika muvaffaqiyatli hisoblandi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating daily statistics for {Date}", date);
            return ServiceResult<DailyDistanceStatisticsDto>.ServerError("Xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Foydalanuvchi umumiy statistikasini olish
    /// </summary>
    public async Task<ServiceResult<UserDistanceSummaryDto>> GetUserSummaryAsync(
        long userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var reports = await _reportRepository.GetByUserAndDateRangeAsync(userId, startDate, endDate);

            if (!reports.Any())
            {
                return ServiceResult<UserDistanceSummaryDto>.NotFound(
                    $"User {userId} uchun hisobotlar topilmadi");
            }

            var summary = new UserDistanceSummaryDto
            {
                UserId = userId,
                UserName = reports.First().User?.Name ?? "Unknown",
                Phone = reports.First().User?.Phone,
                TotalDays = reports.Count,
                TotalDistanceKm = reports.Sum(r => r.TotalDistanceKm),
                AverageDistancePerDayKm = reports.Average(r => r.TotalDistanceKm)
            };

            var maxDistanceReport = reports.OrderByDescending(r => r.TotalDistanceKm).FirstOrDefault();
            if (maxDistanceReport != null)
            {
                summary.MaxDistanceDay = maxDistanceReport.ReportDate;
                summary.MaxDistanceKm = maxDistanceReport.TotalDistanceKm;
            }

            return ServiceResult<UserDistanceSummaryDto>.Ok(
                summary,
                "Umumiy statistika muvaffaqiyatli hisoblandi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating user summary for user {UserId} from {StartDate} to {EndDate}",
                userId, startDate, endDate);
            return ServiceResult<UserDistanceSummaryDto>.ServerError("Xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Entity ni DTO ga mapping qilish
    /// </summary>
    private DailyDistanceReportDto MapToDto(DailyDistanceReport report)
    {
        return new DailyDistanceReportDto
        {
            Id = report.Id,
            UserId = report.UserId,
            UserName = report.User?.Name,
            Phone = report.User?.Phone,
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
}
