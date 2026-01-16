using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Convoy.Service.Services;

/// <summary>
/// Database initialization service - startup'da database'ni tekshiradi
/// Docker yoki yangi environment'da avtomatik database setup
/// </summary>
public class DatabaseInitializerService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseInitializerService> _logger;
    private readonly string _connectionString;

    public DatabaseInitializerService(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseInitializerService> logger,
        NpgsqlConnection connection)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _connectionString = connection.ConnectionString;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DatabaseInitializerService starting - checking database...");

        try
        {
            await EnsureDatabaseExistsAsync(cancellationToken);
            await WaitForDatabaseAsync(cancellationToken);
            _logger.LogInformation("Database is ready!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database initialization");
            // Continue - PartitionMaintenanceService will handle partition creation
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DatabaseInitializerService stopping...");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Database mavjudligini tekshirish
    /// </summary>
    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            _logger.LogInformation("Database connection successful: {Database}", connection.Database);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot connect to database");
            throw;
        }
    }

    /// <summary>
    /// Database tayyor bo'lishini kutish (Docker environment uchun)
    /// </summary>
    private async Task WaitForDatabaseAsync(CancellationToken cancellationToken)
    {
        const int maxRetries = 10;
        const int delaySeconds = 3;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                // Test query
                await using var command = new NpgsqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync(cancellationToken);

                _logger.LogInformation("Database is ready after {Attempts} attempts", i + 1);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Database not ready (attempt {Attempt}/{MaxRetries}): {Error}",
                    i + 1, maxRetries, ex.Message);

                if (i < maxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                }
                else
                {
                    _logger.LogError("Database not ready after {MaxRetries} attempts", maxRetries);
                    throw;
                }
            }
        }
    }
}
