using AutoMapper;
using Convoy.Data.DbContexts;
using Convoy.Data.IRepositories;
using Convoy.Domain.Entities;
using Convoy.Service.DTOs;
using Convoy.Service.Extensions;
using Convoy.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services;

public class UserStoppedReportService : IUserStoppedReportService
{
    private readonly AppDbConText _context;
    private readonly IRepository<UserStoppedReport> _repository;
    private readonly ILogger<UserStoppedReportService> _logger;
    private readonly IMapper _mapper;

    public UserStoppedReportService(
        AppDbConText context,
        IRepository<UserStoppedReport> repository,
        ILogger<UserStoppedReportService> logger,
        IMapper mapper)
    {
        _context = context;
        _repository = repository;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<UserStoppedReportDto> CreateAsync(CreateUserStoppedReportDto dto)
    {
        // Avval shu user uchun aktiv report bormi tekshirish
        var existingReport = await GetActiveStoppedReportAsync(dto.UserId);
        if (existingReport != null)
        {
            _logger.LogWarning("User {UserId} already has an active stopped report (ID: {ReportId})",
                dto.UserId, existingReport.Id);
            return existingReport;
        }

        var report = new UserStoppedReport
        {
            UserId = dto.UserId,
            LocationId = dto.LocationId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            StoppedAt = DateTimeExtensions.NowInApplicationTime(),
            StoppedDurationMinutes = dto.StoppedDurationMinutes,
            Reason = dto.Reason,
            IsResolved = false,
            CreatedAt = DateTimeExtensions.NowInApplicationTime()
        };

        await _repository.InsertAsync(report);
        await _repository.SaveAsync();

        _logger.LogInformation(
            "Created stopped report: User={UserId}, Duration={Duration}min, Reason={Reason}",
            dto.UserId, dto.StoppedDurationMinutes, dto.Reason);

        return _mapper.Map<UserStoppedReportDto>(report);
    }

    public async Task<UserStoppedReportDto?> GetActiveStoppedReportAsync(int userId)
    {
        var report = await _context.UserStoppedReports
            .Where(r => r.UserId == userId && !r.IsResolved)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (report == null)
            return null;

        var dto = _mapper.Map<UserStoppedReportDto>(report);

        // User name'ni alohida query orqali olish (UserId PHP API worker_id)
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        dto.UserName = user?.Name;

        return dto;
    }

    public async Task<bool> ResolveStoppedReportAsync(int userId)
    {
        var report = await _context.UserStoppedReports
            .Where(r => r.UserId == userId && !r.IsResolved)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (report == null)
        {
            _logger.LogWarning("No active stopped report found for User {UserId}", userId);
            return false;
        }

        report.IsResolved = true;
        report.ResolvedAt = DateTimeExtensions.NowInApplicationTime();
        report.UpdatedAt = DateTimeExtensions.NowInApplicationTime();

        _context.UserStoppedReports.Update(report);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Resolved stopped report {ReportId} for User {UserId}",
            report.Id, userId);

        return true;
    }

    public async Task<List<UserStoppedReportDto>> GetStoppedReportsByDateAsync(DateTime date)
    {
        var (startDate, endDate) = date.ToDateRange();

        var reports = await _context.UserStoppedReports
            .Where(r => r.CreatedAt >= startDate && r.CreatedAt < endDate)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var dtos = _mapper.Map<List<UserStoppedReportDto>>(reports);

        // User name'larni qo'shish (UserId -> PHP API worker_id)
        var userIds = reports.Select(r => r.UserId).Distinct().ToList();
        var users = await _context.Users
            .Where(u => u.UserId.HasValue && userIds.Contains(u.UserId.Value))
            .ToDictionaryAsync(u => u.UserId!.Value, u => u.Name);

        foreach (var dto in dtos)
        {
            if (users.TryGetValue(dto.UserId, out var userName))
            {
                dto.UserName = userName;
            }
        }

        return dtos;
    }

    public async Task<List<UserStoppedReportDto>> GetUserStoppedReportsAsync(int userId)
    {
        var reports = await _context.UserStoppedReports
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var dtos = _mapper.Map<List<UserStoppedReportDto>>(reports);

        // User name'ni olish (UserId -> PHP API worker_id)
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user != null)
        {
            foreach (var dto in dtos)
            {
                dto.UserName = user.Name;
            }
        }

        return dtos;
    }

    public async Task<List<int>> GetUserIdsWithActiveStoppedReportsAsync()
    {
        var userIds = await _context.UserStoppedReports
            .Where(r => !r.IsResolved)
            .Select(r => r.UserId)
            .Distinct()
            .ToListAsync();

        _logger.LogInformation("Found {Count} users with active stopped reports", userIds.Count);

        return userIds;
    }
}
