using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Reflection;

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
            await RunMigrationsAsync(cancellationToken);
            _logger.LogInformation("✅ Database initialization completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during database initialization");
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

    /// <summary>
    /// Run embedded SQL migrations
    /// </summary>
    private async Task RunMigrationsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔄 Running database migrations...");

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Create migrations tracking table if not exists
            await EnsureMigrationsTableExistsAsync(connection, cancellationToken);

            // Get embedded SQL scripts from Migrations folder
            var assembly = Assembly.GetExecutingAssembly();
            var migrationResources = assembly.GetManifestResourceNames()
                .Where(name => name.Contains(".Migrations.") && name.EndsWith(".sql"))
                .OrderBy(name => name) // 001_, 002_, 003_ ordering
                .ToList();

            _logger.LogInformation("Found {Count} migration scripts", migrationResources.Count);

            foreach (var resourceName in migrationResources)
            {
                // Extract migration name (e.g., "001_database_setup")
                var migrationName = Path.GetFileNameWithoutExtension(resourceName.Split('.').Last());

                // Check if already applied
                if (await IsMigrationAppliedAsync(connection, migrationName, cancellationToken))
                {
                    _logger.LogInformation("⏭️  Migration '{MigrationName}' already applied, skipping", migrationName);
                    continue;
                }

                _logger.LogInformation("▶️  Applying migration: {MigrationName}", migrationName);

                // Read SQL from embedded resource
                await using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    _logger.LogWarning("⚠️  Could not read migration resource: {ResourceName}", resourceName);
                    continue;
                }

                using var reader = new StreamReader(stream);
                var sql = await reader.ReadToEndAsync(cancellationToken);

                // Execute SQL
                await using var command = new NpgsqlCommand(sql, connection);
                command.CommandTimeout = 300; // 5 minutes timeout for large migrations
                await command.ExecuteNonQueryAsync(cancellationToken);

                // Mark as applied
                await MarkMigrationAsAppliedAsync(connection, migrationName, cancellationToken);

                _logger.LogInformation("✅ Migration '{MigrationName}' applied successfully", migrationName);
            }

            _logger.LogInformation("✅ All migrations completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error running migrations");
            throw;
        }
    }

    /// <summary>
    /// Create migrations tracking table
    /// </summary>
    private async Task EnsureMigrationsTableExistsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = @"
            CREATE TABLE IF NOT EXISTS __migrations (
                id SERIAL PRIMARY KEY,
                migration_name VARCHAR(255) UNIQUE NOT NULL,
                applied_at TIMESTAMPTZ DEFAULT NOW()
            );
        ";

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Check if migration already applied
    /// </summary>
    private async Task<bool> IsMigrationAppliedAsync(NpgsqlConnection connection, string migrationName, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(*) FROM __migrations WHERE migration_name = @name";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("name", migrationName);

        var count = (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
        return count > 0;
    }

    /// <summary>
    /// Mark migration as applied
    /// </summary>
    private async Task MarkMigrationAsAppliedAsync(NpgsqlConnection connection, string migrationName, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO __migrations (migration_name) VALUES (@name)";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("name", migrationName);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
