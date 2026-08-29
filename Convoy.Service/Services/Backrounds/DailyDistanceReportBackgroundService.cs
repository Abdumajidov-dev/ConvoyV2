using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services.Backrounds;

/// <summary>
/// Barcha userlarning kunlik masofa hisobotini yaratadi va yangilab turadi.
/// Bugungi kun har soatda qayta hisoblanadi (admin real vaqtda ko'rsin),
/// kechagi kun esa kun almashgandan keyin bir marta yakuniy hisoblanadi.
/// </summary>
public class DailyDistanceReportBackgroundService : BackgroundService
{
    private readonly ILogger<DailyDistanceReportBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    // Configuration settings
    private readonly int _refreshIntervalMinutes;
    private readonly bool _enableAutoGeneration;

    public DailyDistanceReportBackgroundService(
        ILogger<DailyDistanceReportBackgroundService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;

        // Bugungi hisobot qancha vaqtda bir yangilanadi (default: 60 daqiqa)
        _refreshIntervalMinutes = int.TryParse(_configuration["DailyReportSettings:RefreshIntervalMinutes"], out var interval) && interval > 0
            ? interval
            : 60;
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
        _logger.LogInformation("📅 Bugungi hisobot har {Interval} daqiqada yangilanadi, kechagi kun yarim tundan keyin yakunlanadi",
            _refreshIntervalMinutes);

        // 30 soniya kutish - application to'liq ishga tushgandan keyin ishlash uchun
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        // Qaysi kun oxirgi marta "yakunlangan" (o'sha kun tugagandan keyin qayta hisoblangan)
        DateTime? lastFinalizedDate = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var yesterday = today.AddDays(-1);

                // 1. Bugungi kun - har safar qayta hisoblanadi, admin real vaqtda ko'rsin
                await GenerateReportsForDateAsync(today, "bugungi kun", stoppingToken);

                // 2. Kechagi kun - kun almashgandan keyin bir marta yakuniy hisoblash
                if (lastFinalizedDate != yesterday)
                {
                    await GenerateReportsForDateAsync(yesterday, "kechagi kun (yakuniy)", stoppingToken);
                    lastFinalizedDate = yesterday;
                }

                await Task.Delay(TimeSpan.FromMinutes(_refreshIntervalMinutes), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("⏹ DailyDistanceReportBackgroundService task cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ DailyDistanceReportBackgroundService da xatolik");

                // Xatolik bo'lsa 10 daqiqa kutib qayta urinish
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        _logger.LogInformation("⏹ DailyDistanceReportBackgroundService stopped at {Time}", DateTime.UtcNow);
    }

    /// <summary>
    /// Berilgan sana uchun barcha userlarning hisobotini yaratish/yangilash
    /// </summary>
    private async Task GenerateReportsForDateAsync(DateTime date, string label, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dailyDistanceService = scope.ServiceProvider.GetRequiredService<IDailyDistanceReportService>();

        var result = await dailyDistanceService.GenerateDailyReportsForAllUsersAsync(date);

        if (!result.Success)
        {
            _logger.LogWarning("⚠️ {Label} ({Date}) hisoboti yaratilmadi: {Message}",
                label, date.ToString("yyyy-MM-dd"), result.Message);
            return;
        }

        var reports = result.Data ?? new List<DailyDistanceReportDto>();
        var totalKm = reports.Sum(r => r.TotalDistanceKm);

        _logger.LogInformation("✅ {Label} ({Date}): {Count} ta hisobot, jami {Distance:F2} km",
            label, date.ToString("yyyy-MM-dd"), reports.Count, totalKm);
    }

}
