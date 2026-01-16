using Convoy.Data.IRepositories;
using Convoy.Service.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services;

/// <summary>
/// Partition maintenance background service
/// Startup'da va har oy partition'larni tekshiradi va yaratadi
/// </summary>
public class PartitionMaintenanceService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PartitionMaintenanceService> _logger;

    public PartitionMaintenanceService(
        IServiceScopeFactory scopeFactory,
        ILogger<PartitionMaintenanceService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Service start bo'lganda ishga tushadi
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PartitionMaintenanceService starting...");

        try
        {
            await EnsurePartitionsExistAsync(cancellationToken);
            _logger.LogInformation("PartitionMaintenanceService started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during PartitionMaintenanceService startup");
            // Service to'xtamaydi, faqat log qilinadi
        }
    }

    /// <summary>
    /// Service to'xtaganda ishga tushadi
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PartitionMaintenanceService stopping...");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Kerakli partition'larni yaratish
    /// Hozirgi oy, keyingi 3 oy va oldingi 1 oyni yaratadi
    /// </summary>
    private async Task EnsurePartitionsExistAsync(CancellationToken cancellationToken)
    {
        // Scope yaratish - scoped service'larni olish uchun
        using var scope = _scopeFactory.CreateScope();
        var locationRepository = scope.ServiceProvider.GetRequiredService<ILocationRepository>();

        try
        {
            // Mavjud partition'larni olish
            var existingPartitions = await locationRepository.GetExistingPartitionsAsync();
            var existingPartitionSet = new HashSet<string>(existingPartitions);

            _logger.LogInformation("Found {Count} existing partitions", existingPartitionSet.Count);

            // Yaratish kerak bo'lgan oylar
            var monthsToCreate = new List<DateTime>();

            // Oldingi 1 oy
            monthsToCreate.Add(DateTimeExtensions.NowInApplicationTime().AddMonths(-1));

            // Hozirgi oy
            monthsToCreate.Add(DateTimeExtensions.NowInApplicationTime());

            // Keyingi 3 oy
            for (int i = 1; i <= 3; i++)
            {
                monthsToCreate.Add(DateTimeExtensions.NowInApplicationTime().AddMonths(i));
            }

            // Har bir oy uchun partition yaratish
            foreach (var month in monthsToCreate)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var partitionName = $"locations_{month:MM_yyyy}";

                if (existingPartitionSet.Contains(partitionName))
                {
                    _logger.LogInformation("Partition {PartitionName} already exists, skipping", partitionName);
                    continue;
                }

                try
                {
                    var result = await locationRepository.CreatePartitionAsync(month);
                    _logger.LogInformation("Partition creation result: {Result}", result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating partition for {Month}", month.ToString("yyyy-MM"));
                    // Davom ettiramiz, boshqa partition'larni yaratamiz
                }
            }

            _logger.LogInformation("Partition maintenance completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in EnsurePartitionsExistAsync");
            throw;
        }
    }
}
