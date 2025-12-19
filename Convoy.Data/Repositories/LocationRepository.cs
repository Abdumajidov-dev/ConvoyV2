using Convoy.Data.IRepositories;
using Convoy.Domain.Entities;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Convoy.Data.Repositories;

/// <summary>
/// Location repository implementation - Dapper bilan partitioned table'lar
/// </summary>
public class LocationRepository : ILocationRepository
{
    private readonly NpgsqlConnection _connection;
    private readonly ILogger<LocationRepository> _logger;

    public LocationRepository(NpgsqlConnection connection, ILogger<LocationRepository> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    /// <summary>
    /// Yangi location yozish
    /// </summary>
    public async Task<long> InsertAsync(Location location)
    {
        const string sql = @"
            INSERT INTO locations (
                user_id, recorded_at, latitude, longitude,
                accuracy, speed, heading, altitude,
                activity_type, activity_confidence, is_moving,
                battery_level, is_charging, distance_from_previous,
                created_at
            ) VALUES (
                @UserId, @RecordedAt, @Latitude, @Longitude,
                @Accuracy, @Speed, @Heading, @Altitude,
                @ActivityType, @ActivityConfidence, @IsMoving,
                @BatteryLevel, @IsCharging, @DistanceFromPrevious,
                @CreatedAt
            ) RETURNING id";

        try
        {
            var id = await _connection.ExecuteScalarAsync<long>(sql, location);
            _logger.LogInformation("Location inserted: UserId={UserId}, Id={Id}", location.UserId, id);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting location for UserId={UserId}", location.UserId);
            throw;
        }
    }

    /// <summary>
    /// Batch insert - bir nechta location'larni bir vaqtda yozish
    /// </summary>
    public async Task<int> InsertBatchAsync(IEnumerable<Location> locations)
    {
        const string sql = @"
            INSERT INTO locations (
                user_id, recorded_at, latitude, longitude,
                accuracy, speed, heading, altitude,
                activity_type, activity_confidence, is_moving,
                battery_level, is_charging, distance_from_previous,
                created_at
            ) VALUES (
                @UserId, @RecordedAt, @Latitude, @Longitude,
                @Accuracy, @Speed, @Heading, @Altitude,
                @ActivityType, @ActivityConfidence, @IsMoving,
                @BatteryLevel, @IsCharging, @DistanceFromPrevious,
                @CreatedAt
            )";

        try
        {
            var rowsAffected = await _connection.ExecuteAsync(sql, locations);
            _logger.LogInformation("Batch inserted {Count} locations", rowsAffected);
            return rowsAffected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch inserting locations");
            throw;
        }
    }

    /// <summary>
    /// User'ning location'larini vaqt oralig'ida olish
    /// </summary>
    public async Task<IEnumerable<Location>> GetUserLocationsAsync(int userId, DateTime startDate, DateTime endDate)
    {
        const string sql = @"
            SELECT
                id, user_id as UserId, recorded_at as RecordedAt,
                latitude, longitude, accuracy, speed, heading, altitude,
                activity_type as ActivityType, activity_confidence as ActivityConfidence,
                is_moving as IsMoving, battery_level as BatteryLevel,
                is_charging as IsCharging, distance_from_previous as DistanceFromPrevious,
                created_at as CreatedAt
            FROM locations
            WHERE user_id = @UserId
                AND recorded_at >= @StartDate
                AND recorded_at < @EndDate
            ORDER BY recorded_at DESC";

        try
        {
            var locations = await _connection.QueryAsync<Location>(sql, new { UserId = userId, StartDate = startDate, EndDate = endDate });
            _logger.LogInformation("Retrieved {Count} locations for UserId={UserId}", locations.Count(), userId);
            return locations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locations for UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// User'ning oxirgi N ta location'ini olish
    /// </summary>
    public async Task<IEnumerable<Location>> GetLastLocationsAsync(int userId, int count = 100)
    {
        const string sql = @"
            SELECT
                id, user_id as UserId, recorded_at as RecordedAt,
                latitude, longitude, accuracy, speed, heading, altitude,
                activity_type as ActivityType, activity_confidence as ActivityConfidence,
                is_moving as IsMoving, battery_level as BatteryLevel,
                is_charging as IsCharging, distance_from_previous as DistanceFromPrevious,
                created_at as CreatedAt
            FROM locations
            WHERE user_id = @UserId
            ORDER BY recorded_at DESC
            LIMIT @Count";

        try
        {
            var locations = await _connection.QueryAsync<Location>(sql, new { UserId = userId, Count = count });
            _logger.LogInformation("Retrieved last {Count} locations for UserId={UserId}", locations.Count(), userId);
            return locations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last locations for UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Kunlik yo'l masofalarini olish (summary statistics)
    /// </summary>
    public async Task<Dictionary<DateTime, decimal>> GetDailyDistancesAsync(int userId, DateTime startDate, DateTime endDate)
    {
        const string sql = @"
            SELECT
                DATE(recorded_at) as date,
                COALESCE(SUM(distance_from_previous), 0) as total_distance
            FROM locations
            WHERE user_id = @UserId
                AND recorded_at >= @StartDate
                AND recorded_at < @EndDate
            GROUP BY DATE(recorded_at)
            ORDER BY date";

        try
        {
            var results = await _connection.QueryAsync<(DateTime date, decimal total_distance)>(sql,
                new { UserId = userId, StartDate = startDate, EndDate = endDate });

            var dailyDistances = results.ToDictionary(r => r.date.Date, r => r.total_distance);
            _logger.LogInformation("Retrieved daily distances for UserId={UserId}, Days={Count}", userId, dailyDistances.Count);
            return dailyDistances;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily distances for UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// ID va partition key orqali location olish
    /// </summary>
    public async Task<Location?> GetByIdAsync(long id, DateTime recordedAt)
    {
        const string sql = @"
            SELECT
                id, user_id as UserId, recorded_at as RecordedAt,
                latitude, longitude, accuracy, speed, heading, altitude,
                activity_type as ActivityType, activity_confidence as ActivityConfidence,
                is_moving as IsMoving, battery_level as BatteryLevel,
                is_charging as IsCharging, distance_from_previous as DistanceFromPrevious,
                created_at as CreatedAt
            FROM locations
            WHERE id = @Id AND recorded_at = @RecordedAt";

        try
        {
            var location = await _connection.QuerySingleOrDefaultAsync<Location>(sql, new { Id = id, RecordedAt = recordedAt });
            return location;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location by Id={Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Partition yaratish (PostgreSQL function chaqirish)
    /// </summary>
    public async Task<string> CreatePartitionAsync(DateTime targetMonth)
    {
        const string sql = "SELECT create_location_partition(@TargetMonth)";

        try
        {
            var result = await _connection.ExecuteScalarAsync<string>(sql, new { TargetMonth = targetMonth });
            _logger.LogInformation("Partition creation result: {Result}", result);
            return result ?? "Unknown result";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating partition for {Month}", targetMonth.ToString("yyyy-MM"));
            throw;
        }
    }

    /// <summary>
    /// Mavjud partition'larni olish
    /// </summary>
    public async Task<IEnumerable<string>> GetExistingPartitionsAsync()
    {
        const string sql = @"
            SELECT tablename
            FROM pg_tables
            WHERE tablename LIKE 'locations_%'
                AND schemaname = 'public'
            ORDER BY tablename";

        try
        {
            var partitions = await _connection.QueryAsync<string>(sql);
            _logger.LogInformation("Found {Count} existing partitions", partitions.Count());
            return partitions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting existing partitions");
            throw;
        }
    }

    /// <summary>
    /// Haversine formula - ikki GPS nuqta orasidagi masofani hisoblash (metrda)
    /// </summary>
    public double CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const double earthRadiusKm = 6371.0;

        var dLat = DegreesToRadians((double)(lat2 - lat1));
        var dLon = DegreesToRadians((double)(lon2 - lon1));

        var lat1Rad = DegreesToRadians((double)lat1);
        var lat2Rad = DegreesToRadians((double)lat2);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) *
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        var distanceKm = earthRadiusKm * c;
        return distanceKm * 1000; // Metrga o'girish
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
