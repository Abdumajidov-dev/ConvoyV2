using Convoy.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services.Backrounds;

/// <summary>
/// Har kuni avtomatik ravishda barcha userlarning kunlik masofa hisobotini yaratadi
/// Default: Har kuni soat 00:05 da ishga tushadi (yarim tundan 5 daqiqa keyin)
/// Kecha kunning hisobotini yaratadi
/// </summary>
public class DailyDistanceReportBackgroundService : BackgroundService
{
    private readonly ILogger<DailyDistanceReportBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    // Configuration settings
    private readonly int _runTimeHour;
    private readonly int _runTimeMinute;
    private readonly bool _enableAutoGeneration;

    public DailyDistanceReportBackgroundService(
        ILogger<DailyDistanceReportBackgroundService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;

        // Read configuration (default: 00:05 AM)
        _runTimeHour = int.TryParse(_configuration["DailyReportSettings:RunTimeHour"], out var hour) ? hour : 0;
        _runTimeMinute = int.TryParse(_configuration["DailyReportSettings:RunTimeMinute"], out var minute) ? minute : 5;
        _enableAutoGeneration = bool.TryParse(_configuration["DailyReportSettings:EnableAutoGeneration"], out var enabled) ? enabled : true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enableAutoGeneration)
        {
            _logger.LogWarning("⚠️ DailyDistanceReportBackgroundService is DISABLED in configuration");
            return;
        }

        _logger.LogInformation("🔄 DailyDistanceReportBackgroundService started at {Time}", DateTime.UtcNow);
        _logger.LogInformation("📅 Scheduled to run daily at {Hour:D2}:{Minute:D2} (UTC)", _runTimeHour, _runTimeMinute);

        // 30 soniya kutish - application to'liq ishga tushgandan keyin ishlash uchun
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRunTime = CalculateNextRunTime(now);
                var delay = nextRunTime - now;

                _logger.LogInformation("⏰ Next report generation scheduled for: {NextRunTime} (UTC)", nextRunTime);
                _logger.LogInformation("⌛ Waiting {Hours}h {Minutes}m {Seconds}s until next run",
                    (int)delay.TotalHours, delay.Minutes, delay.Seconds);

                // Keyingi ishga tushish vaqtigacha kutish
                await Task.Delay(delay, stoppingToken);

                // Hisobot yaratish
                await GenerateDailyReportsAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("⏹ DailyDistanceReportBackgroundService task cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ DailyDistanceReportBackgroundService da xatolik");

                // Xatolik bo'lsa 1 soat kutish va qayta urinish
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("⏹ DailyDistanceReportBackgroundService stopped at {Time}", DateTime.UtcNow);
    }

    /// <summary>
    /// Keyingi ishga tushish vaqtini hisoblash
    /// Agar bugun soat allaqachon o'tgan bo'lsa, ertangi kunga o'tkazadi
    /// </summary>
    private DateTime CalculateNextRunTime(DateTime currentTime)
    {
        var scheduledTime = new DateTime(
            currentTime.Year,
            currentTime.Month,
            currentTime.Day,
            _runTimeHour,
            _runTimeMinute,
            0,
            DateTimeKind.Utc
        );

        // Agar bugun soat allaqachon o'tgan bo'lsa, ertangi kunga o'tkazish
        if (scheduledTime <= currentTime)
        {
            scheduledTime = scheduledTime.AddDays(1);
        }

        return scheduledTime;
    }

    /// <summary>
    /// Kechagi kun uchun barcha userlarning hisobotini yaratish
    /// </summary>
    private async Task GenerateDailyReportsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dailyDistanceService = scope.ServiceProvider.GetRequiredService<IDailyDistanceReportService>();

        try
        {
            var reportDate = DateTime.UtcNow.Date.AddDays(-1); // Kechagi kun

            _logger.LogInformation("📊 Starting daily distance report generation for date: {Date}", reportDate.ToString("yyyy-MM-dd"));
            _logger.LogInformation("⏱️ Generation started at: {Time}", DateTime.UtcNow);

            // Barcha userlar uchun hisobot yaratish
            var result = await dailyDistanceService.GenerateDailyReportsForAllUsersAsync(reportDate);

            if (result.Success)
            {
                _logger.LogInformation("✅ Daily distance reports generated successfully!");
                _logger.LogInformation("📈 Reports created: {Count}", result.Data?.Count ?? 0);
                _logger.LogInformation("⏱️ Generation completed at: {Time}", DateTime.UtcNow);

                // Statistika log qilish
                if (result.Data != null && result.Data.Any())
                {
                    var totalDistance = result.Data.Sum(r => r.TotalDistanceKm);
                    var avgDistance = result.Data.Average(r => r.TotalDistanceKm);
                    var maxDistance = result.Data.Max(r => r.TotalDistanceKm);
                    var topUser = result.Data.OrderByDescending(r => r.TotalDistanceKm).FirstOrDefault();

                    _logger.LogInformation("📊 Statistics for {Date}:", reportDate.ToString("yyyy-MM-dd"));
                    _logger.LogInformation("   - Total users: {Count}", result.Data.Count);
                    _logger.LogInformation("   - Total distance: {Distance:F2} km", totalDistance);
                    _logger.LogInformation("   - Average distance: {Distance:F2} km", avgDistance);
                    _logger.LogInformation("   - Max distance: {Distance:F2} km", maxDistance);

                    if (topUser != null)
                    {
                        _logger.LogInformation("   - Top user: {UserName} ({UserId}) - {Distance:F2} km",
                            topUser.UserName, topUser.UserId, topUser.TotalDistanceKm);
                    }
                }
            }
            else
            {
                _logger.LogWarning("⚠️ Daily distance report generation completed with warnings: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generating daily distance reports for date: {Date}", DateTime.UtcNow.Date.AddDays(-1));
        }
    }
}
