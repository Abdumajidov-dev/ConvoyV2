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
            // FIXED: RecordedAt UTC formatda bo'lishi kerak, lekin agar kelajakdagi vaqt bo'lsa - manfiy bo'ladi
            // Shuning uchun hozirgi vaqtni ham UTC'ga o'tkazamiz va manfiy qiymatni 0 ga o'rnatamiz
            var currentTime = DateTime.UtcNow;
            var lastTime = lastLocationTime.Value.Kind == DateTimeKind.Utc
                ? lastLocationTime.Value
                : DateTime.SpecifyKind(lastLocationTime.Value, DateTimeKind.Utc);

            var offlineDuration = (currentTime - lastTime).TotalMinutes;

            // Agar manfiy bo'lsa (kelajakdagi vaqt), 0 deb hisoblaymiz
            if (offlineDuration < 0)
            {
                _logger.LogWarning("User {UserId} ({UserName}) uchun lastLocationTime kelajakda: {LastTime}, CurrentTime: {CurrentTime}. Manfiy qiymat: {Offline}min",
                    userId, userName, lastTime, currentTime, offlineDuration);
                offlineDuration = 0;
            }

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

            // 1 SOATDAN KO'P TO'XTAB QOLGAN BO'LSA - UserStoppedReport yaratish
            // IMPORTANT: Har 1 soatda YANGI report yaratamiz (update emas!)
            // Bu hisobot table bo'lgani uchun tarixni saqlash kerak
            //
            // Ikki xil holatni tekshiramiz:
            // 1) User 1 soatdan ortiq location yubormaganu (offline)
            // 2) User location yuboryapti lekin 1 soat davomida bir joyda turgan (stopped)

            var shouldCreateStoppedReport = false;
            string stoppedReason = "";

            // So'nggi stopped report'ni olish (takrorlanmasligi uchun)
            var lastStoppedReport = await context.UserStoppedReports
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(stoppingToken);

            // Agar so'nggi report kamida 1 soat oldin yaratilgan bo'lsa yoki umuman bo'lmasa
            var canCreateNewReport = lastStoppedReport == null ||
                                    (DateTime.UtcNow - lastStoppedReport.CreatedAt).TotalMinutes >= 60;

            if (canCreateNewReport)
            {
                // HOLATNI 1: User 1 soatdan ortiq offline (location yubormaganu)
                if (offlineDurationInt >= 60)
                {
                    shouldCreateStoppedReport = true;
                    stoppedReason = $"User {offlineDurationInt} daqiqadan beri location yubormaganu (offline)";
                }
                // HOLATNI 2: User aktiv lekin bir joyda turgan (1 soat davomida 100m radiusda)
                else if (offlineDurationInt < 20) // User aktiv (20 daqiqa ichida location yuborgan)
                {
                    // So'nggi 1 soat davomidagi barcha locationlarni olish
                    var oneHourAgo = currentTime.AddHours(-1);
                    var recentLocations = (await locationRepo.GetUserLocationsAsync(
                        userId, oneHourAgo, currentTime)).ToList();

                    if (recentLocations != null && recentLocations.Count >= 3)
                    {
                        // Barcha locationlar bir joyda (100m radiusda) ekanligini tekshirish
                        var firstLocation = recentLocations.OrderBy(l => l.RecordedAt).First();
                        var allWithinRadius = recentLocations.All(loc =>
                        {
                            var distance = CalculateDistance(
                                (double)firstLocation.Latitude,
                                (double)firstLocation.Longitude,
                                (double)loc.Latitude,
                                (double)loc.Longitude);
                            return distance <= 100; // 100 metr radius
                        });

                        if (allWithinRadius)
                        {
                            var stoppedDuration = (int)(currentTime - recentLocations.Min(l => l.RecordedAt)).TotalMinutes;
                            if (stoppedDuration >= 60)
                            {
                                shouldCreateStoppedReport = true;
                                stoppedReason = $"1 soat davomida bir joyda turgan ({recentLocations.Count} ta location, 100m radiusda)";
                            }
                        }
                    }
                }

                // YANGI Stopped report yaratish (agar kerak bo'lsa)
                // CRITICAL: Har safar YANGI record yaratamiz, update qilmaymiz!
                if (shouldCreateStoppedReport)
                {
                    var lastLocation = lastLocations.FirstOrDefault();
                    if (lastLocation != null)
                    {
                        var stoppedReport = new UserStoppedReport
                        {
                            UserId = userId,
                            LocationId = lastLocation.Id,
                            Latitude = lastLocation.Latitude,
                            Longitude = lastLocation.Longitude,
                            StoppedAt = lastLocationTime.Value,
                            StoppedDurationMinutes = offlineDurationInt >= 60 ? offlineDurationInt : 60,
                            Reason = stoppedReason,
                            IsResolved = false,
                            CreatedAt = DateTime.UtcNow
                        };

                        context.UserStoppedReports.Add(stoppedReport);
                        await context.SaveChangesAsync(stoppingToken);

                        _logger.LogWarning(
                            "🛑 YANGI STOPPED REPORT YARATILDI: User {UserId} ({UserName}) - {Duration} daqiqa to'xtab qolgan. " +
                            "Sabab: {Reason}. Report ID: {ReportId}",
                            userId, userName, offlineDurationInt, stoppedReason, stoppedReport.Id);
                    }
                }
            }
            else
            {
                // So'nggi report 1 soat ichida yaratilgan, yangi report yaratmaymiz
                var minutesSinceLastReport = (DateTime.UtcNow - lastStoppedReport.CreatedAt).TotalMinutes;
                _logger.LogDebug(
                    "User {UserId} ({UserName}) to'xtab qolgan, lekin so'nggi report {Minutes:F0} daqiqa oldin yaratilgan. " +
                    "Yangi report yaratish uchun {Remaining:F0} daqiqa kutish kerak.",
                    userId, userName, minutesSinceLastReport, 60 - minutesSinceLastReport);
            }

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
            // VAQT CHEGARASI: Faqat 9:00 dan 19:00 gacha notification yuborish
            var currentHour = DateTime.Now.Hour; // Local vaqt (server time zone)
            if (currentHour < 9 || currentHour >= 19)
            {
                _logger.LogDebug("⏰ Notification yuborilmadi: Hozirgi vaqt {Hour}:00. Notification faqat 9:00-19:00 oralig'ida yuboriladi.",
                    currentHour);
                return;
            }

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

    /// <summary>
    /// Haversine formula orqali ikki nuqta orasidagi masofani hisoblash (metrda)
    /// </summary>
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371.0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distanceKm = EarthRadiusKm * c;

        return distanceKm * 1000; // Metrga o'tkazish
    }

    private double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}

