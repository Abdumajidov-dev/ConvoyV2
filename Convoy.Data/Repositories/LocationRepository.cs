using Convoy.Data.IRepositories;
using Convoy.Domain.Entities;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Npgsql.Internal;

namespace Convoy.Data.Repositories;

/// <summary>
/// Location repository implementation - Dapper bilan partitioned table'lar
/// </summary>
public class LocationRepository : ILocationRepository
{
    private readonly NpgsqlConnection _connection;
    private readonly string _connectionString;
    private readonly ILogger<LocationRepository> _logger;

    public LocationRepository(IConfiguration configuration,NpgsqlConnection connection, ILogger<LocationRepository> logger)
    {
        _connection = connection;
        _connectionString = configuration.GetConnectionString("DefaultConnection");
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
    /// IMPORTANT: Faqat database'da MAVJUD bo'lgan columnlarni INSERT qilish kerak
    /// </summary>
    public async Task<IEnumerable<Location>> InsertBatchAsync(IEnumerable<Location> locations)
    {
        // NOTE: Bu query faqat database'da MAVJUD bo'lgan columnlarga INSERT qiladi
        // Entity'da ko'proq propertylar bo'lishi mumkin, lekin database'da yo'q
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
    /// <summary>
    /// Vaqt bo'yicha oldingi location (masofa hisoblash uchun).
    /// 2 kunlik oyna: partition pruning ishlashi uchun va uzoq tanaffusdan keyin
    /// mantiqsiz katta masofa chiqmasligi uchun.
    /// </summary>
    public async Task<Location?> GetPreviousLocationAsync(int userId, DateTime beforeUtc)
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
                AND recorded_at < @Before
                AND recorded_at >= @WindowStart
            ORDER BY recorded_at DESC
            LIMIT 1";

        try
        {
            return await _connection.QueryFirstOrDefaultAsync<Location>(sql, new
            {
                UserId = userId,
                Before = beforeUtc,
                WindowStart = beforeUtc.AddDays(-2)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting previous location for UserId={UserId}, Before={Before}", userId, beforeUtc);
            throw;
        }
    }

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
            var locationsList = locations.ToList();
            _logger.LogInformation("Retrieved last {Count} locations for UserId={UserId}", locationsList.Count, userId);

            // DEBUG: Log first location details if exists
            if (locationsList.Any())
            {
                var first = locationsList.First();
                _logger.LogInformation("  → First location: ID={Id}, Lat={Lat}, Lon={Lon}, Distance={Dist}, RecordedAt={Time}",
                    first.Id, first.Latitude, first.Longitude, first.DistanceFromPrevious, first.RecordedAt);
            }

            return locationsList;
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
        // IMPORTANT: Cast to DATE type because PostgreSQL function expects DATE, not TIMESTAMP
        const string sql = "SELECT create_location_partition(@TargetMonth::DATE)";

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
    /// <summary>
    /// Userlar bo'yicha umumiy masofa (metr).
    /// GetMultipleUsersLocationsAsync limitPerUser bilan nuqtalarni qisqartiradi va
    /// clustering ham bir qismini tashlab yuboradi - shuning uchun jami masofani
    /// javobdagi nuqtalardan hisoblab bo'lmaydi, alohida aggregate kerak.
    /// </summary>
    public async Task<IDictionary<int, decimal>> GetTotalDistanceByUsersAsync(
        List<int> userIds,
        DateTime startDate,
        DateTime endDate,
        string? startTime = null,
        string? endTime = null)
    {
        if (userIds == null || !userIds.Any())
            return new Dictionary<int, decimal>();

        var sql = @"
            SELECT user_id AS UserId,
                   COALESCE(SUM(distance_from_previous), 0) AS TotalMeters
            FROM locations
            WHERE user_id = ANY(@UserIds)
                AND recorded_at >= @StartDate
                AND recorded_at < @EndDate";

        var (startHour, startMinute) = ParseTimeParts(startTime);
        var (endHour, endMinute) = ParseTimeParts(endTime);

        // Vaqt filtri - GetMultipleUsersLocationsAsync bilan bir xil mantiq (Toshkent vaqti)
        if (startHour.HasValue && endHour.HasValue)
        {
            sql += @"
                AND (
                    EXTRACT(HOUR FROM recorded_at AT TIME ZONE 'Asia/Tashkent') * 60 + EXTRACT(MINUTE FROM recorded_at AT TIME ZONE 'Asia/Tashkent') >= @StartHour * 60 + @StartMinute
                    AND EXTRACT(HOUR FROM recorded_at AT TIME ZONE 'Asia/Tashkent') * 60 + EXTRACT(MINUTE FROM recorded_at AT TIME ZONE 'Asia/Tashkent') <= @EndHour * 60 + @EndMinute
                )";
        }
        else if (startHour.HasValue)
        {
            sql += @"
                AND EXTRACT(HOUR FROM recorded_at AT TIME ZONE 'Asia/Tashkent') * 60 + EXTRACT(MINUTE FROM recorded_at AT TIME ZONE 'Asia/Tashkent') >= @StartHour * 60 + @StartMinute";
        }
        else if (endHour.HasValue)
        {
            sql += @"
                AND EXTRACT(HOUR FROM recorded_at AT TIME ZONE 'Asia/Tashkent') * 60 + EXTRACT(MINUTE FROM recorded_at AT TIME ZONE 'Asia/Tashkent') <= @EndHour * 60 + @EndMinute";
        }

        sql += @"
            GROUP BY user_id";

        try
        {
            var rows = await _connection.QueryAsync<(int UserId, decimal TotalMeters)>(sql, new
            {
                UserIds = userIds.ToArray(),
                StartDate = startDate,
                EndDate = endDate,
                StartHour = startHour,
                StartMinute = startMinute,
                EndHour = endHour,
                EndMinute = endMinute
            });

            return rows.ToDictionary(r => r.UserId, r => r.TotalMeters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total distance for users (UserIds={UserIds})", string.Join(",", userIds));
            throw;
        }
    }

    /// <summary>
    /// "HH:MM" formatidagi vaqtni soat va daqiqaga ajratish
    /// </summary>
    private static (int? Hour, int? Minute) ParseTimeParts(string? time)
    {
        if (string.IsNullOrWhiteSpace(time))
            return (null, null);

        var parts = time.Split(':');
        if (parts.Length == 2 && int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var m))
            return (h, m);

        return (null, null);
    }

    public async Task<IList<long>> BulkInsertAsync(IList<Location> locations)
    {
        if (locations == null || !locations.Any())
            return new List<long>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1️⃣ Temporary table yarating (FAQAT MAVJUD COLUMNLAR)
            await connection.ExecuteAsync(@"
            CREATE TEMP TABLE locations_tmp (
                user_id int,
                recorded_at timestamptz,
                latitude numeric,
                longitude numeric,
                accuracy numeric,
                speed numeric,
                heading numeric,
                altitude numeric,
                activity_type varchar,
                activity_confidence int,
                is_moving boolean,
                battery_level int,
                is_charging boolean,
                distance_from_previous numeric,
                created_at timestamptz
            ) ON COMMIT DROP;
        ", transaction: transaction);

            // 2️⃣ COPY BINARY bilan tez yozish (FAQAT MAVJUD COLUMNLAR)
            await using (var writer = connection.BeginBinaryImport(
                @"COPY locations_tmp (
                    user_id, recorded_at, latitude, longitude, accuracy, speed, heading, altitude,
                    activity_type, activity_confidence, is_moving, battery_level, is_charging,
                    distance_from_previous, created_at
                ) FROM STDIN (FORMAT BINARY)"))
            {
                foreach (var loc in locations)
                {
                    writer.StartRow();
                    writer.Write(loc.UserId, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(loc.RecordedAt, NpgsqlTypes.NpgsqlDbType.TimestampTz);
                    writer.Write(loc.Latitude, NpgsqlTypes.NpgsqlDbType.Numeric);
                    writer.Write(loc.Longitude, NpgsqlTypes.NpgsqlDbType.Numeric);
                    writer.Write(loc.Accuracy, NpgsqlTypes.NpgsqlDbType.Numeric);
                    writer.Write(loc.Speed, NpgsqlTypes.NpgsqlDbType.Numeric);
                    writer.Write(loc.Heading, NpgsqlTypes.NpgsqlDbType.Numeric);
                    writer.Write(loc.Altitude, NpgsqlTypes.NpgsqlDbType.Numeric);
                    writer.Write(loc.ActivityType, NpgsqlTypes.NpgsqlDbType.Varchar);
                    writer.Write(loc.ActivityConfidence, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(loc.IsMoving, NpgsqlTypes.NpgsqlDbType.Boolean);
                    writer.Write(loc.BatteryLevel, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(loc.IsCharging, NpgsqlTypes.NpgsqlDbType.Boolean);
                    writer.Write(loc.DistanceFromPrevious, NpgsqlTypes.NpgsqlDbType.Numeric);
                    writer.Write(loc.CreatedAt, NpgsqlTypes.NpgsqlDbType.TimestampTz);
                }
                await writer.CompleteAsync();
            }

            // 3️⃣ Temporary table'dan original table'ga yozish va IDs olish (FAQAT MAVJUD COLUMNLAR)
            var ids = (await connection.QueryAsync<long>(
                @"INSERT INTO locations (
                    user_id, recorded_at, latitude, longitude, accuracy, speed, heading, altitude,
                    activity_type, activity_confidence, is_moving, battery_level, is_charging,
                    distance_from_previous, created_at
                )
                SELECT
                    user_id, recorded_at, latitude, longitude, accuracy, speed, heading, altitude,
                    activity_type, activity_confidence, is_moving, battery_level, is_charging,
                    distance_from_previous, created_at
                FROM locations_tmp
                RETURNING id;",
                transaction: transaction
            )).ToList();

            await transaction.CommitAsync();
            return ids;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BulkInsertAsync error");
            await transaction.RollbackAsync();
            throw;
        }
    }
}
