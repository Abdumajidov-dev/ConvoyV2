using Convoy.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services.Backrounds;

/// <summary>
/// TEST FAQAT! Har 2 daqiqada ishga tushib hisobotni yaratadi
/// Production'da ISHLATILMAYDI
/// </summary>
public class TestDailyDistanceReportService : BackgroundService
{
    private readonly ILogger<TestDailyDistanceReportService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly bool _enableTestMode;

    public TestDailyDistanceReportService(
        ILogger<TestDailyDistanceReportService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;

        // Faqat Development environment'da yoqiladi
        _enableTestMode = bool.TryParse(
            _configuration["DailyReportSettings:EnableTestMode"],
            out var enabled) ? enabled : false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enableTestMode)
        {
            _logger.LogInformation("⚠️ TestDailyDistanceReportService DISABLED (not in test mode)");
            return;
        }

        _logger.LogWarning("🧪 TEST MODE: DailyDistanceReportService running every 2 MINUTES");
        _logger.LogWarning("⚠️ This is for TESTING ONLY - do NOT use in production!");

        // 10 soniya kutish - application ishga tushishi uchun
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("🔄 [TEST] Starting report generation at {Time}", DateTime.Now);

                await GenerateTestReportsAsync(stoppingToken);

                _logger.LogInformation("✅ [TEST] Report generation completed");
                _logger.LogInformation("⏰ [TEST] Next run in 2 minutes...");

                // 2 daqiqa kutish
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("⏹ TestDailyDistanceReportService stopped");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [TEST] Error in test report generation");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task GenerateTestReportsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dailyDistanceService = scope.ServiceProvider.GetRequiredService<IDailyDistanceReportService>();

        try
        {
            // Kechagi kun uchun hisobot
            var reportDate = DateTime.Today.AddDays(-1);

            _logger.LogInformation("📊 [TEST] Generating reports for: {Date}", reportDate.ToString("yyyy-MM-dd"));

            var result = await dailyDistanceService.GenerateDailyReportsForAllUsersAsync(reportDate);

            if (result.Success && result.Data != null)
            {
                _logger.LogInformation("✅ [TEST] Generated {Count} reports", result.Data.Count);

                if (result.Data.Any())
                {
                    var totalDistance = result.Data.Sum(r => r.TotalDistanceKm);
                    var avgDistance = result.Data.Average(r => r.TotalDistanceKm);

                    _logger.LogInformation("📈 [TEST] Total: {Total:F2} km | Avg: {Avg:F2} km",
                        totalDistance, avgDistance);
                }
            }
            else
            {
                _logger.LogWarning("⚠️ [TEST] Generation completed with warnings: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [TEST] Error generating test reports");
        }
    }
}
