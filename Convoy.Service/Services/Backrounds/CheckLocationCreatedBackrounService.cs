using Convoy.Data.DbContexts;
using Convoy.Data.IRepositories;
using Convoy.Domain.Entities;
using Convoy.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services.Backrounds;

/// <summary>
/// User'larning so'nggi location post qilgan vaqtini tekshiradi
/// Agar user 20 daqiqadan ortiq vaqt davomida location post qilmasa, admin'larga notification yuboradi
/// Har 1 minutda ishga tushadi
/// </summary>
public class CheckLocationCreatedBackrounService : BackgroundService
{
    private readonly ILogger<CheckLocationCreatedBackrounService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Har 1 minutda tekshirish

    // Offline duration thresholds (daqiqalarda)
    private readonly int[] _notificationThresholds = { 20, 40, 60, 80, 100, 120 }; // 20min, 40min, 1h, 1h20min, 1h40min, 2h

    public CheckLocationCreatedBackrounService(
        ILogger<CheckLocationCreatedBackrounService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔄 CheckLocationCreatedBackrounService started at {Time}", DateTime.UtcNow);

        // 30 soniya kutish - application to'liq ishga tushgandan keyin ishlash uchun
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckUserLocationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CheckLocationCreatedBackrounService da xatolik");
            }

            // Keyingi tekshiruvgacha kutish (1 minut)
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("⏹ CheckLocationCreatedBackrounService stopped at {Time}", DateTime.UtcNow);
    }

    private async Task CheckUserLocationsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbConText>();
        var locationRepo = scope.ServiceProvider.GetRequiredService<ILocationRepository>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        try
        {
            _logger.LogInformation("🔍 Checking user locations at {Time}", DateTime.UtcNow);

            // Barcha active user'larni olish (faqat admin_unduruv bo'lmagan user'lar)
            var activeUsers = await context.Users
                .Where(u => u.IsActive && u.Role != "admin_unduruv")
                .ToListAsync(stoppingToken);

            _logger.LogInformation("📊 Active users count: {Count}", activeUsers.Count);

            foreach (var user in activeUsers)
            {
                if (!user.UserId.HasValue)
                    continue;

                try
                {
                    await CheckSingleUserLocationAsync(
                        (int)user.UserId.Value,
                        user.Name,
                        context,
                        locationRepo,
                        notificationService,
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "User {UserId} ni tekshirishda xatolik", user.UserId);
                }
            }

            _logger.LogInformation("✅ User location check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckUserLocationsAsync da xatolik");
        }
    }

    private async Task CheckSingleUserLocationAsync(
        int userId,
        string userName,
        AppDbConText context,
        ILocationRepository locationRepo,
        INotificationService notificationService,
        CancellationToken stoppingToken)
    {
        try
        {
            // User'ning so'nggi location'ini olish (Dapper orqali)
            var lastLocations = await locationRepo.GetLastLocationsAsync(userId, 1);
            var lastLocationTime = lastLocations?.FirstOrDefault()?.RecordedAt;

            if (lastLocationTime == null)
            {
                _logger.LogWarning("User {UserId} ({UserName}) uchun location topilmadi", userId, userName);
                return;
            }

            // Offline duration (daqiqalarda)
            var offlineDuration = (DateTime.UtcNow - lastLocationTime.Value).TotalMinutes;
            var offlineDurationInt = (int)Math.Floor(offlineDuration);

            // User status report'ni olish yoki yaratish
            var statusReport = await context.UserStatusReports
                .FirstOrDefaultAsync(usr => usr.UserId == (long)userId, stoppingToken);

            if (statusReport == null)
            {
                // Yangi status report yaratish
                statusReport = new UserStatusReport
                {
                    UserId = userId,
                    LastLocationTime = lastLocationTime,
                    LastNotifiedAt = null,
                    OfflineDurationMinutes = offlineDurationInt,
                    IsNotified = false,
                    NotificationCount = 0
                };
                context.UserStatusReports.Add(statusReport);
            }
            else
            {
                // Mavjud report'ni yangilash
                statusReport.LastLocationTime = lastLocationTime;
                statusReport.OfflineDurationMinutes = offlineDurationInt;
            }

            await context.SaveChangesAsync(stoppingToken);

            // Notification yuborish kerakligini aniqlash
            if (offlineDurationInt >= 20) // Minimum 20 minut offline
            {
                await SendNotificationIfNeededAsync(
                    userId,
                    userName,
                    offlineDurationInt,
                    statusReport,
                    context,
                    notificationService,
                    stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User {UserId} ni tekshirishda xatolik", userId);
        }
    }

    private async Task SendNotificationIfNeededAsync(
        int userId,
        string userName,
        int offlineDurationMinutes,
        UserStatusReport statusReport,
        AppDbConText context,
        INotificationService notificationService,
        CancellationToken stoppingToken)
    {
        try
        {
            // Eng yaqin threshold'ni topish (20, 40, 60, 80, 100, 120)
            var threshold = _notificationThresholds
                .Where(t => offlineDurationMinutes >= t)
                .OrderByDescending(t => t)
                .FirstOrDefault();

            if (threshold == 0)
                return; // Hali 20 minutga yetmagan

            // Agar bu threshold uchun notification yuborilmagan bo'lsa yoki
            // so'nggi notification'dan keyin yangi threshold'ga o'tgan bo'lsa
            var shouldNotify = false;

            if (statusReport.LastNotifiedAt == null)
            {
                // Birinchi notification
                shouldNotify = true;
            }
            else
            {
                // So'nggi notification'dan keyin qancha vaqt o'tganini tekshirish
                var minutesSinceLastNotification = (DateTime.UtcNow - statusReport.LastNotifiedAt.Value).TotalMinutes;

                // Agar so'nggi notification'dan keyin keyingi threshold'ga o'tgan bo'lsa
                var lastNotifiedThreshold = _notificationThresholds
                    .Where(t => t <= (offlineDurationMinutes - minutesSinceLastNotification))
                    .OrderByDescending(t => t)
                    .FirstOrDefault();

                shouldNotify = threshold > lastNotifiedThreshold;
            }

            if (shouldNotify)
            {
                // Notification yuborish
                await notificationService.SendUserOfflineNotificationAsync(userId, userName, offlineDurationMinutes);

                // Status report'ni yangilash
                statusReport.LastNotifiedAt = DateTime.UtcNow;
                statusReport.IsNotified = true;
                statusReport.NotificationCount++;

                await context.SaveChangesAsync(stoppingToken);

                _logger.LogWarning("🚨 NOTIFICATION YUBORILDI: User {UserId} ({UserName}) - {Duration} daqiqadan beri offline",
                    userId, userName, offlineDurationMinutes);
            }
            else
            {
                _logger.LogDebug("User {UserId} ({UserName}) offline: {Duration}min, lekin notification yuborilmadi (allaqachon yuborilgan)",
                    userId, userName, offlineDurationMinutes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification yuborishda xatolik. UserId: {UserId}", userId);
        }
    }
}

