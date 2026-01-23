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
                battery_level, is_charging,
                distance_from_previous, created_at
            ) VALUES (
                @UserId, @RecordedAt, @Latitude, @Longitude,
                @Accuracy, @Speed, @Heading, @Altitude,
                @ActivityType, @ActivityConfidence, @IsMoving,
                @BatteryLevel, @IsCharging,
                @DistanceFromPrevious, @CreatedAt
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
    /// Batch insert - bir nechta location'larni bir vaqtda yozish va ID'lari bilan qaytarish
    /// </summary>
    public async Task<IEnumerable<Location>> InsertBatchAsync(IEnumerable<Location> locations)
    {
        const string sql = @"
            INSERT INTO locations (
                user_id, recorded_at, latitude, longitude,
                accuracy, speed, heading, altitude,
                ellipsoidal_altitude, heading_accuracy, speed_accuracy, altitude_accuracy, floor,
                activity_type, activity_confidence, is_moving,
                battery_level, is_charging,
                timestamp, age, event, mock, sample, odometer, uuid, extras,
                distance_from_previous, created_at
            ) VALUES (
                @UserId, @RecordedAt, @Latitude, @Longitude,
                @Accuracy, @Speed, @Heading, @Altitude,
                @ActivityType, @ActivityConfidence, @IsMoving,
                @BatteryLevel, @IsCharging,
                @DistanceFromPrevious, @CreatedAt
            )
            RETURNING id, user_id as UserId, recorded_at as RecordedAt,
                      latitude, longitude, accuracy, speed, heading, altitude,
                      activity_type as ActivityType, activity_confidence as ActivityConfidence,
                      is_moving as IsMoving, battery_level as BatteryLevel,
                      is_charging as IsCharging,
                      distance_from_previous as DistanceFromPrevious,
                      created_at as CreatedAt";

        try
        {
            var insertedLocations = new List<Location>();

            // Har bir location uchun alohida insert qilish (RETURNING ishlashi uchun)
            foreach (var location in locations)
            {
                var insertedLocation = await _connection.QuerySingleAsync<Location>(sql, location);
                insertedLocations.Add(insertedLocation);
            }

            _logger.LogInformation("Batch inserted {Count} locations", insertedLocations.Count);
            return insertedLocations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch inserting locations");
            throw;
        }
    }

    /// <summary>
    /// User'ning location'larini vaqt oralig'ida olish (vaqt string filtri bilan: "HH:MM")
    /// </summary>
    public async Task<IEnumerable<Location>> GetUserLocationsAsync(int userId, DateTime startDate, DateTime endDate, string? startTime = null, string? endTime = null)
    {
        var sqlBuilder = @"
            SELECT
                id, user_id as UserId, recorded_at as RecordedAt,
                latitude, longitude, accuracy, speed, heading, altitude,
                activity_type as ActivityType, activity_confidence as ActivityConfidence,
                is_moving as IsMoving, battery_level as BatteryLevel,
                is_charging as IsCharging,
                distance_from_previous as DistanceFromPrevious,
                created_at as CreatedAt
            FROM locations
            WHERE user_id = @UserId
                AND recorded_at >= @StartDate
                AND recorded_at < @EndDate";

        // Time string'larni parse qilish
        int? startHour = null, startMinute = null, endHour = null, endMinute = null;

        if (!string.IsNullOrWhiteSpace(startTime))
        {
            var parts = startTime.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var m))
            {
                startHour = h;
                startMinute = m;
            }
        }

        if (!string.IsNullOrWhiteSpace(endTime))
        {
            var parts = endTime.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var m))
            {
                endHour = h;
                endMinute = m;
            }
        }

        // Vaqt filtri qo'shish (agar berilgan bo'lsa) - Toshkent timezone'ida (UTC+5)
        // recorded_at'ni Toshkent vaqtiga konvertatsiya qilish uchun AT TIME ZONE ishlatish
        if (startHour.HasValue && startMinute.HasValue && endHour.HasValue && endMinute.HasValue)
        {
            // Ikkala vaqt ham berilgan: start_time >= X:Y AND end_time <= A:B
            sqlBuilder += @"
                AND (
                    EXTRACT(HOUR FROM recorded_at AT TIME ZONE 'Asia/Tashkent') * 60 + EXTRACT(MINUTE FROM recorded_at AT TIME ZONE 'Asia/Tashkent') >= @StartHour * 60 + @StartMinute
                    AND EXTRACT(HOUR FROM recorded_at AT TIME ZONE 'Asia/Tashkent') * 60 + EXTRACT(MINUTE FROM recorded_at AT TIME ZONE 'Asia/Tashkent') <= @EndHour * 60 + @EndMinute
                )";
        }
        else if (startHour.HasValue && startMinute.HasValue)
        {
            // Faqat start time berilgan: >= X:Y
            sqlBuilder += @"
                AND EXTRACT(HOUR FROM recorded_at AT TIME ZONE 'Asia/Tashkent') * 60 + EXTRACT(MINUTE FROM recorded_at AT TIME ZONE 'Asia/Tashkent') >= @StartHour * 60 + @StartMinute";
        }
        else if (endHour.HasValue && endMinute.HasValue)
        {
            // Faqat end time berilgan: <= A:B
            sqlBuilder += @"
                AND EXTRACT(HOUR FROM recorded_at AT TIME ZONE 'Asia/Tashkent') * 60 + EXTRACT(MINUTE FROM recorded_at AT TIME ZONE 'Asia/Tashkent') <= @EndHour * 60 + @EndMinute";
        }

        sqlBuilder += @"
            ORDER BY recorded_at DESC";

        try
        {
            var locations = await _connection.QueryAsync<Location>(sqlBuilder, new
            {
                UserId = userId,
                StartDate = startDate,
                EndDate = endDate,
                StartHour = startHour,
                StartMinute = startMinute,
                EndHour = endHour,
                EndMinute = endMinute
            });
            _logger.LogInformation("Retrieved {Count} locations for UserId={UserId} (StartTime={StartTime}, EndTime={EndTime})",
                locations.Count(), userId, startTime, endTime);
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
                is_charging as IsCharging,
                distance_from_previous as DistanceFromPrevious,
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
    /// Barcha userlarning oxirgi location'larini olish
    /// </summary>
    public async Task<IEnumerable<Location>> GetAllUsersLatestLocationsAsync()
    {
        const string sql = @"
            SELECT DISTINCT ON (user_id)
                id, user_id as UserId, recorded_at as RecordedAt,
                latitude, longitude, accuracy, speed, heading, altitude,
                activity_type as ActivityType, activity_confidence as ActivityConfidence,
                is_moving as IsMoving, battery_level as BatteryLevel,
                is_charging as IsCharging,
                distance_from_previous as DistanceFromPrevious,
                created_at as CreatedAt
            FROM locations
            ORDER BY user_id, recorded_at DESC";

        try
        {
            var locations = await _connection.QueryAsync<Location>(sql);
            _logger.LogInformation("Retrieved latest locations for {Count} users", locations.Count());
            return locations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users latest locations");
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
                is_charging as IsCharging,
                distance_from_previous as DistanceFromPrevious,
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

    /// <summary>
    /// Ko'p userlarning locationlarini vaqt oralig'ida olish (vaqt string filtri bilan: "HH:MM")
    /// </summary>
    public async Task<IEnumerable<Location>> GetMultipleUsersLocationsAsync(List<int> userIds, DateTime startDate, DateTime endDate, string? startTime = null, string? endTime = null, int? limitPerUser = null)
    {
        if (userIds == null || !userIds.Any())
        {
            _logger.LogWarning("GetMultipleUsersLocationsAsync called with empty userIds list");
            return Enumerable.Empty<Location>();
        }

        var sqlBuilder = @"
            SELECT * FROM (
                SELECT
                    id, user_id as UserId, recorded_at as RecordedAt,
                    latitude, longitude, accuracy, speed, heading, altitude,
                    activity_type as ActivityType, activity_confidence as ActivityConfidence,
                    is_moving as IsMoving, battery_level as BatteryLevel,
                    is_charging as IsCharging,
                    distance_from_previous as DistanceFromPrevious,
                    created_at as CreatedAt,
                    ROW_NUMBER() OVER (PARTITION BY user_id ORDER BY recorded_at DESC) as row_num
                FROM locations
                WHERE user_id = ANY(@UserIds)
                    AND recorded_at >= @StartDate
                    AND recorded_at < @EndDate";

        // Time string'larni parse qilish
        int? startHour = null, startMinute = null, endHour = null, endMinute = null;

        if (!string.IsNullOrWhiteSpace(startTime))
        {
            var parts = startTime.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var m))
            {
                startHour = h;
                startMinute = m;
            }
        }

        if (!string.IsNullOrWhiteSpace(endTime))
        {
            var parts = endTime.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var m))
            {
                endHour = h;
                endMinute = m;
            }
        }

        // Vaqt filtri qo'shish (Toshkent timezone'ida - UTC+5)
        // recorded_at'ni Toshkent vaqtiga konvertatsiya qilish uchun AT TIME ZONE ishlatish
        if (startHour.HasValue && startMinute.HasValue && endHour.HasValue && endMinute.HasValue)
        {
            sqlBuilder += @"
                    AND (
                        EXTRACT(HOUR FROM recorded_at AT TIME ZONE 'Asia/Tashkent') * 60 + EXTRACT(MINUTE FROM recorded_at AT TIME ZONE 'Asia/Tashkent') >= @StartHour * 60 + @StartMinute
                        AND EXTRACT(HOUR FROM recorded_at AT TIME ZONE 'Asia/Tashkent') * 60 + EXTRACT(MINUTE FROM recorded_at AT TIME ZONE 'Asia/Tashkent') <= @EndHour * 60 + @EndMinute
                    )";
        }
        else if (startHour.HasValue && startMinute.HasValue)
        {
            sqlBuilder += @"
                    AND EXTRACT(HOUR FROM recorded_at AT TIME ZONE 'Asia/Tashkent') * 60 + EXTRACT(MINUTE FROM recorded_at AT TIME ZONE 'Asia/Tashkent') >= @StartHour * 60 + @StartMinute";
        }
        else if (endHour.HasValue && endMinute.HasValue)
        {
            sqlBuilder += @"
                    AND EXTRACT(HOUR FROM recorded_at AT TIME ZONE 'Asia/Tashkent') * 60 + EXTRACT(MINUTE FROM recorded_at AT TIME ZONE 'Asia/Tashkent') <= @EndHour * 60 + @EndMinute";
        }

        sqlBuilder += @"
            ) AS ranked_locations
            WHERE 1=1";

        // Har bir user uchun limit
        if (limitPerUser.HasValue && limitPerUser.Value > 0)
        {
            sqlBuilder += @"
                AND row_num <= @LimitPerUser";
        }

        sqlBuilder += @"
            ORDER BY UserId, RecordedAt DESC";

        try
        {
            var locations = await _connection.QueryAsync<Location>(sqlBuilder, new
            {
                UserIds = userIds.ToArray(),
                StartDate = startDate,
                EndDate = endDate,
                StartHour = startHour,
                StartMinute = startMinute,
                EndHour = endHour,
                EndMinute = endMinute,
                LimitPerUser = limitPerUser
            });

            _logger.LogInformation("Retrieved {Count} locations for {UserCount} users (StartTime={StartTime}, EndTime={EndTime}, LimitPerUser={LimitPerUser})",
                locations.Count(), userIds.Count, startTime, endTime, limitPerUser);
            return locations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locations for multiple users (UserIds={UserIds})", string.Join(",", userIds));
            throw;
        }
    }
}
