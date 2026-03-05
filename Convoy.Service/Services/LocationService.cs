using AutoMapper;
using Convoy.Data.IRepositories;
using Convoy.Domain.Entities;
using Convoy.Service.Common;
using Convoy.Service.DTOs;
using Convoy.Service.Extensions;
using Convoy.Service.Interfaces;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services;

/// <summary>
/// Location service implementation with real-time SignalR broadcasting
/// </summary>
public class LocationService : ILocationService
{
    private readonly ILocationRepository _locationRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<LocationService> _logger;
    private readonly IRepository<User> _userRepository;
    private readonly object? _locationHubContext;
    private readonly ITelegramService? _telegramService;
    private readonly LocationClusteringService _clusteringService;

    public LocationService(
        IRepository<User> userRepository,
        ILocationRepository locationRepository,
        IMapper mapper,
        ILogger<LocationService> logger,
        LocationClusteringService clusteringService,
        object? locationHubContext = null,
        ITelegramService? telegramService = null)
    {
        _locationRepository = locationRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
        _clusteringService = clusteringService;
        _locationHubContext = locationHubContext;
        _telegramService = telegramService;
    }
    public async Task<ServiceResult<IList<LocationResponseDto>>>
        CreateUserLocationsAsync(int userId, IList<LocationDataDto> locationsData)
    {
        try
        {
            if (locationsData == null || !locationsData.Any())
                return ServiceResult<IList<LocationResponseDto>>
                    .BadRequest("Locations data bo'sh bo'lmasligi kerak");

            var orderedLocations = locationsData
                .OrderBy(x => x.RecordedAt)
                .ToList();

            // Oldingi oxirgi location
            var lastLocations = await _locationRepository
                .GetLastLocationsAsync(userId, 1);

            var previousLocation = lastLocations.FirstOrDefault();

            var newLocations = new List<Location>();

            foreach (var locationData in orderedLocations)
            {
                if (!locationData.RecordedAt.HasValue)
                    throw new CustomException(400, "RecordedAt majburiy");

                var recordedAtUtc = locationData.RecordedAt.Value
                    .ToApplicationTime();

                decimal? distanceFromPrevious = null;

                if (previousLocation != null)
                {
                    var distance = _locationRepository.CalculateDistance(
                        previousLocation.Latitude,
                        previousLocation.Longitude,
                        locationData.Latitude,
                        locationData.Longitude
                    );

                    distanceFromPrevious = (decimal)distance;
                }

                var location = new Location
                {
                    UserId = userId,
                    RecordedAt = recordedAtUtc,
                    Latitude = locationData.Latitude,
                    Longitude = locationData.Longitude,
                    DistanceFromPrevious = distanceFromPrevious,
                    CreatedAt = DateTimeExtensions.NowInApplicationTime()
                };

                newLocations.Add(location);

                // Keyingi hisob uchun previous yangilanadi
                previousLocation = location;
            }

            // 🔥 MUHIM: bulk insert
            await _locationRepository.BulkInsertAsync(newLocations);

            var response = _mapper
                .Map<IList<LocationResponseDto>>(newLocations);

            // SignalR – batch broadcast
            if (_locationHubContext != null)
            {
                dynamic hubContext = _locationHubContext;

                await hubContext.Clients
                    .Group($"user_{userId}")
                    .SendAsync("LocationsUpdated", response);
            }

            return ServiceResult<IList<LocationResponseDto>>
                .Created(response, "Locations muvaffaqiyatli yaratildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating bulk locations for UserId={UserId}", userId);

            return ServiceResult<IList<LocationResponseDto>>
                .ServerError("Locations yaratishda xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Bitta user uchun bitta location yaratish (userId controller'dan, location data body'dan)
    /// </summary>
    public async Task<ServiceResult<LocationResponseDto>> CreateUserLocationAsync(int userId, LocationDataDto locationData)
    {
        try
        {
            _logger.LogInformation("🚀 DEBUG: CreateUserLocationAsync STARTED for UserId={UserId}", userId);

            // RecordedAt bo'lmasa - hozirgi vaqtni set qilish
            if (!locationData.RecordedAt.HasValue)
            {
                //locationData.RecordedAt =
                //Time.UtcNow;
                throw new CustomException(400, "recorded vaqtini berish majburish");
            }

            // FIXED: RecordedAt ni to'g'ri UTC'ga konvertatsiya qilish
            // Flutter client local vaqt (UTC+5) yuboradi, biz uni UTC'ga o'tkazamiz
            var recordedAtUtc = locationData.RecordedAt.Value.ToApplicationTime();

            // User'ning oldingi location'ini olish (distance hisoblash uchun)
            var lastLocations = await _locationRepository.GetLastLocationsAsync(userId, 1);
            var lastLocationsList = lastLocations.ToList(); // Convert to list for count
            _logger.LogInformation("🔍 DEBUG: GetLastLocationsAsync returned {Count} locations for UserId={UserId}",
                lastLocationsList.Count, userId);

            var previousLocation = lastLocationsList.FirstOrDefault();

            decimal? distanceFromPrevious = null;

            if (previousLocation != null)
            {
                _logger.LogInformation("🔍 Previous location found: ID={PrevId}, Lat={PrevLat}, Lon={PrevLon}, RecordedAt={PrevTime}",
                    previousLocation.Id, previousLocation.Latitude, previousLocation.Longitude, previousLocation.RecordedAt);

                var distance = _locationRepository.CalculateDistance(
                    previousLocation.Latitude,
                    previousLocation.Longitude,
                    locationData.Latitude,
                    locationData.Longitude
                );

                distanceFromPrevious = (decimal)distance;

                _logger.LogInformation("📏 Distance calculated: {Distance} meters (Previous→New)", distanceFromPrevious);
                _logger.LogInformation("📍 New location: Lat={NewLat}, Lon={NewLon}",
                    locationData.Latitude, locationData.Longitude);
            }
            else
            {
                _logger.LogWarning("⚠️ No previous location found for user {UserId} - this is the FIRST location (or query returned empty)", userId);
            }

            // Validation warnings for out-of-range values
            if (locationData.Accuracy.HasValue && locationData.Accuracy.Value > 9999.99m)
                _logger.LogWarning("Accuracy clamped: {Original} → 9999.99", locationData.Accuracy.Value);
            if (locationData.Speed.HasValue && locationData.Speed.Value > 9999.99m)
                _logger.LogWarning("Speed clamped: {Original} → 9999.99", locationData.Speed.Value);
            if (locationData.Heading.HasValue && locationData.Heading.Value > 999.99m)
                _logger.LogWarning("Heading clamped: {Original} → 999.99", locationData.Heading.Value);
            if (locationData.Age.HasValue && locationData.Age.Value > 99999999.99m)
                _logger.LogWarning("Age clamped: {Original} → 99999999.99", locationData.Age.Value);

            // DEBUG: Log the distance value before creating entity
            _logger.LogInformation("🔢 DEBUG: distanceFromPrevious value = {Distance} (will be inserted to DB)", distanceFromPrevious);

            // Location entity yaratish
            var location = new Location
            {
                UserId = userId,
                RecordedAt = recordedAtUtc,

                // Core location properties (REQUIRED)
                Latitude = locationData.Latitude,
                Longitude = locationData.Longitude,

                // Core location properties (OPTIONAL) - with validation to prevent overflow
                // accuracy: max 9999.99 (DECIMAL(6,2))
                Accuracy = locationData.Accuracy.HasValue && locationData.Accuracy.Value > 9999.99m
                    ? 9999.99m
                    : locationData.Accuracy,

                // speed: max 9999.99 (DECIMAL(6,2))
                Speed = locationData.Speed.HasValue && locationData.Speed.Value > 9999.99m
                    ? 9999.99m
                    : locationData.Speed,

                // heading: max 999.99 (DECIMAL(5,2))
                Heading = locationData.Heading.HasValue && locationData.Heading.Value > 999.99m
                    ? 999.99m
                    : locationData.Heading,

                // altitude: max 999999.99 (DECIMAL(8,2))
                Altitude = locationData.Altitude.HasValue && locationData.Altitude.Value > 999999.99m
                    ? 999999.99m
                    : locationData.Altitude,

                // Flutter Background Geolocation - Extended Coords (OPTIONAL) - with validation
                // ellipsoidal_altitude: max 9999.999999 (DECIMAL(10,6))
                EllipsoidalAltitude = locationData.EllipsoidalAltitude.HasValue && locationData.EllipsoidalAltitude.Value > 9999.999999m
                    ? 9999.999999m
                    : locationData.EllipsoidalAltitude,

                // heading_accuracy: max 9999.999999 (DECIMAL(10,6))
                HeadingAccuracy = locationData.HeadingAccuracy.HasValue && locationData.HeadingAccuracy.Value > 9999.999999m
                    ? 9999.999999m
                    : locationData.HeadingAccuracy,

                // speed_accuracy: max 9999.999999 (DECIMAL(10,6))
                SpeedAccuracy = locationData.SpeedAccuracy.HasValue && locationData.SpeedAccuracy.Value > 9999.999999m
                    ? 9999.999999m
                    : locationData.SpeedAccuracy,

                // altitude_accuracy: max 9999.999999 (DECIMAL(10,6))
                AltitudeAccuracy = locationData.AltitudeAccuracy.HasValue && locationData.AltitudeAccuracy.Value > 9999.999999m
                    ? 9999.999999m
                    : locationData.AltitudeAccuracy,

                Floor = locationData.Floor,

                // Activity (OPTIONAL)
                ActivityType = locationData.ActivityType,
                ActivityConfidence = locationData.ActivityConfidence,
                IsMoving = locationData.IsMoving ?? false,

                // Battery (OPTIONAL)
                BatteryLevel = locationData.BatteryLevel,
                IsCharging = locationData.IsCharging ?? false,

                // Flutter Background Geolocation - Location metadata (OPTIONAL) - with validation
                Timestamp = locationData.Timestamp,

                // age: max 99999999.99 (DECIMAL(10,2) - milliseconds)
                Age = locationData.Age.HasValue && locationData.Age.Value > 99999999.99m
                    ? 99999999.99m
                    : locationData.Age,

                Event = locationData.Event,
                Mock = locationData.Mock,
                Sample = locationData.Sample,

                // odometer: max 9999999999999.99 (DECIMAL(15,2) - meters)
                Odometer = locationData.Odometer.HasValue && locationData.Odometer.Value > 9999999999999.99m
                    ? 9999999999999.99m
                    : locationData.Odometer,

                Uuid = locationData.Uuid,
                Extras = locationData.Extras,

                // Calculated fields
                DistanceFromPrevious = distanceFromPrevious,
                CreatedAt = DateTimeExtensions.NowInApplicationTime()
            };

            // Database'ga saqlash - ID bilan qaytadi
            var insertedId = await _locationRepository.InsertAsync(location);
            location.Id = insertedId;

            _logger.LogInformation("User {UserId} uchun location yaratildi, ID={LocationId}", userId, insertedId);

            // Response DTO yaratish
            var responseDto = _mapper.Map<LocationResponseDto>(location);

            // SignalR orqali real-time broadcast
            if (_locationHubContext != null)
            {
                try
                {
                    dynamic hubContext = _locationHubContext;

                    // Specific user'ni track qilayotganlarga
                    await hubContext.Clients.Group($"user_{userId}")
                        .SendAsync("LocationUpdated", responseDto);

                    // Barcha user'larni track qilayotganlarga
                    await hubContext.Clients.Group("all_users")
                        .SendAsync("LocationUpdated", responseDto);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to broadcast location via SignalR for UserId={UserId}", userId);
                }
            }

            // Telegram kanalga xabar yuborish
            if (_telegramService != null)
            {
                try
                {
                    var user = await _userRepository.SelectAsync(u => u.UserId == userId);
                    await _telegramService.SendLocationDataAsync(
                        userId,
                        $"User {user.Name}",
                        double.Parse(responseDto.Latitude.ToString()),
                        double.Parse(responseDto.Longitude.ToString()),
                        responseDto.RecordedAt
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send Telegram notification for UserId={UserId}", userId);
                }
            }

            return ServiceResult<LocationResponseDto>.Created(responseDto, "Location muvaffaqiyatli yaratildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating location for UserId={UserId}", userId);
            return ServiceResult<LocationResponseDto>.ServerError(
                "Location yaratishda xatolik yuz berdi");
        }
    }


    /// <summary>
    /// User location'larini olish (vaqt string filtri bilan: "HH:MM") - query params
    /// </summary>
    public async Task<ServiceResult<IEnumerable<LocationResponseDto>>> GetUserLocationsAsync(LocationQueryDto query)
    {
        try
        {
            IEnumerable<Location> locations;

            if (query.StartDate.HasValue && query.EndDate.HasValue)
            {
                locations = await _locationRepository.GetUserLocationsAsync(
                    query.UserId,
                    query.StartDate.Value,
                    query.EndDate.Value,
                    query.StartTime,
                    query.EndTime
                );
            }
            else
            {
                locations = await _locationRepository.GetLastLocationsAsync(
                    query.UserId,
                    query.Limit ?? 1000
                );
            }

            var result = _mapper.Map<IEnumerable<LocationResponseDto>>(locations);
            return ServiceResult<IEnumerable<LocationResponseDto>>.Ok(
                result,
                "Location ma'lumotlari muvaffaqiyatli olindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locations for UserId={UserId}", query.UserId);
            return ServiceResult<IEnumerable<LocationResponseDto>>.ServerError(
                "Location ma'lumotlarini olishda xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Bitta userning locationlarini olish (body orqali filterlar, user_id route'da)
    /// FAQAT BIR KUNLIK locationlar
    /// </summary>
    public async Task<ServiceResult<IEnumerable<LocationResponseDto>>> GetSingleUserLocationsAsync(int userId, SingleUserLocationQueryDto query)
    {
        try
        {
            // Bir kunlik oraliq: query.Date kunining 00:00:00 dan 23:59:59 gacha
            // DateTimeExtensions orqali markazlashtirilgan timezone management
            var (startDate, endDate) = query.Date.ToDateRange();

            var locations = await _locationRepository.GetUserLocationsAsync(
                userId,
                startDate,
                endDate,
                query.StartTime,
                query.EndTime
            );

            var result = _mapper.Map<IEnumerable<LocationResponseDto>>(locations)
                        .OrderBy(l => l.RecordedAt) // ?? reverse
                        .ToList();


            _logger.LogInformation("Retrieved {Count} locations for UserId={UserId} on Date={Date}",
                result.Count(), userId, query.Date.ToString("yyyy-MM-dd"));

            return ServiceResult<IEnumerable<LocationResponseDto>>.Ok(
                result,
                $"{query.Date:yyyy-MM-dd} uchun {result.Count()} ta location olindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locations for UserId={UserId}", userId);
            return ServiceResult<IEnumerable<LocationResponseDto>>.ServerError(
                "Location ma'lumotlarini olishda xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Oxirgi location'larni olish
    /// </summary>
    public async Task<ServiceResult<IEnumerable<LocationResponseDto>>> GetLastLocationsAsync(int userId, int count = 100)
    {
        try
        {
            var locations = await _locationRepository.GetLastLocationsAsync(userId, count);
            var result = _mapper.Map<List<LocationResponseDto>>(locations);

            return ServiceResult<IEnumerable<LocationResponseDto>>.Ok(
                result,
                $"Oxirgi {result.Count} ta location olindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last locations for UserId={UserId}", userId);
            return ServiceResult<IEnumerable<LocationResponseDto>>.ServerError(
                "Location ma'lumotlarini olishda xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Barcha userlarning oxirgi location'larini olish (user ma'lumotlari bilan birga)
    /// Bu method LocationService'da bo'lmasligi kerak, chunki u UserService'ga bog'liq
    /// Shuning uchun faqat locationlarni qaytaramiz
    /// </summary>
    public async Task<ServiceResult<IEnumerable<LocationResponseDto>>> GetAllUsersLatestLocationsAsync()
    {
        try
        {
            var locations = await _locationRepository.GetAllUsersLatestLocationsAsync();
            var result = _mapper.Map<List<LocationResponseDto>>(locations);

            _logger.LogInformation("Retrieved latest locations for {Count} users", result.Count);

            return ServiceResult<IEnumerable<LocationResponseDto>>.Ok(
                result,
                $"Barcha userlarning oxirgi location'lari ({result.Count} ta user)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users latest locations");
            return ServiceResult<IEnumerable<LocationResponseDto>>.ServerError(
                "Barcha userlarning location'larini olishda xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Kunlik statistikalarni olish
    /// </summary>
    public async Task<ServiceResult<IEnumerable<DailyStatisticsDto>>> GetDailyStatisticsAsync(DailySummaryQueryDto query)
    {
        try
        {
            var dailyDistances = await _locationRepository.GetDailyDistancesAsync(
                query.UserId,
                query.StartDate,
                query.EndDate
            );

            // Har bir kun uchun location count olish
            var allLocations = await _locationRepository.GetUserLocationsAsync(
                query.UserId,
                query.StartDate,
                query.EndDate
            );

            var locationsByDate = allLocations
                .GroupBy(l => l.RecordedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var statistics = dailyDistances.Select(kvp => new DailyStatisticsDto
            {
                Date = kvp.Key,
                TotalDistanceMeters = kvp.Value,
                LocationCount = locationsByDate.GetValueOrDefault(kvp.Key, 0)
            }).OrderBy(s => s.Date).ToList();

            _logger.LogInformation("Retrieved daily statistics for UserId={UserId}, Days={Days}",
                query.UserId, statistics.Count);

            return ServiceResult<IEnumerable<DailyStatisticsDto>>.Ok(
                statistics,
                "Kunlik statistikalar muvaffaqiyatli olindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily statistics for UserId={UserId}", query.UserId);
            return ServiceResult<IEnumerable<DailyStatisticsDto>>.ServerError(
                "Statistikalarni olishda xatolik yuz berdi");
        }
    }

    /// <summary>
    /// ID orqali location olish
    /// </summary>
    public async Task<ServiceResult<LocationResponseDto>> GetLocationByIdAsync(long id, DateTime recordedAt)
    {
        try
        {
            var location = await _locationRepository.GetByIdAsync(id, recordedAt);

            if (location == null)
            {
                return ServiceResult<LocationResponseDto>.NotFound("Location topilmadi");
            }

            return ServiceResult<LocationResponseDto>.Ok(
                _mapper.Map<LocationResponseDto>(location),
                "Location muvaffaqiyatli olindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location by Id={Id}", id);
            return ServiceResult<LocationResponseDto>.ServerError(
                "Location ma'lumotini olishda xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Ko'p userlarning locationlarini olish (body orqali user_ids va filterlar)
    /// FAQAT BIR KUNLIK locationlar
    /// user_ids YOKI branch_guid berilishi kerak
    /// User ma'lumotlari bilan birga locations array qaytaradi
    /// </summary>
    public async Task<ServiceResult<IEnumerable<UserWithLocationsDto>>> GetMultipleUsersLocationsAsync(MultipleUsersLocationQueryDto query, IUserService? userService = null)
    {
        try
        {
            // UserService ni tekshirish
            if (userService == null)
            {
                return ServiceResult<IEnumerable<UserWithLocationsDto>>.ServerError(
                    "UserService not available");
            }

            // Date string'ni parse qilish - DateTimeExtensions orqali
            if (string.IsNullOrWhiteSpace(query.Date))
            {
                return ServiceResult<IEnumerable<UserWithLocationsDto>>.BadRequest(
                    "date field bo'sh bo'lmasligi kerak");
            }

            DateTime parsedDate;
            try
            {
                // DateTimeExtensions orqali har qanday formatdagi sanani parse qilish
                parsedDate = query.Date.ParseToApplicationTime();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse date: {Date}", query.Date);
                return ServiceResult<IEnumerable<UserWithLocationsDto>>.BadRequest(
                    $"date formati noto'g'ri: {query.Date}");
            }

            List<int> userIds;

            // FILTER PRIORITY:
            // 1. Agar user_ids berilgan bo'lsa - faqat shu userlar, lekin filterlar qo'llaniladi
            // 2. Agar user_ids yo'q bo'lsa - filterlar bo'yicha barcha userlar tanlanadi

            // Parse time range if provided
            DateTime? startDateTime = null;
            DateTime? endDateTime = null;

            if (!string.IsNullOrWhiteSpace(query.StartTime))
            {
                try
                {
                    var timeParts = query.StartTime.Split(':');
                    if (timeParts.Length == 2)
                    {
                        var hour = int.Parse(timeParts[0]);
                        var minute = int.Parse(timeParts[1]);
                        startDateTime = new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day, hour, minute, 0, DateTimeKind.Utc);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse start_hour: {StartHour}", query.StartTime);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.EndTime))
            {
                try
                {
                    var timeParts = query.EndTime.Split(':');
                    if (timeParts.Length == 2)
                    {
                        var hour = int.Parse(timeParts[0]);
                        var minute = int.Parse(timeParts[1]);
                        endDateTime = new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day, hour, minute, 0, DateTimeKind.Utc);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse end_hour: {EndHour}", query.EndTime);
                }
            }

            if (query.UserIds != null && query.UserIds.Any())
            {
                // user_ids berilgan - bu userlarni filterlar bilan tekshirish
                _logger.LogInformation("Provided user_ids: {UserIds}, applying filters...", string.Join(",", query.UserIds));

                // Agar filterlar mavjud bo'lsa, shu user_ids ichidan filter qilish
                if (query.IsActive.HasValue || query.IsStopped.HasValue)
                {
                    // GetFilteredUserIdsAsync barcha userlardan filterlar bo'yicha tanlaydi
                    var allFilteredUserIds = await userService.GetFilteredUserIdsAsync(
                        query.IsActive,
                        query.IsStopped,
                        query.MinStoppedMinutes ?? 60, // Default: 1 soat
                        parsedDate,
                        startDateTime,
                        endDateTime,
                        query.BranchGuid);

                    // Faqat provided user_ids va filtered user_ids kesishmasini olish
                    userIds = query.UserIds.Intersect(allFilteredUserIds).ToList();

                    _logger.LogInformation(
                        "After filtering: {Count}/{Total} users matched (is_active={IsActive}, is_stopped={IsStopped})",
                        userIds.Count, query.UserIds.Count, query.IsActive, query.IsStopped);
                }
                else
                {
                    // Filterlar yo'q - faqat user_ids
                    userIds = query.UserIds;
                    _logger.LogInformation("Using provided user_ids without filters: {Count} users", userIds.Count);
                }
            }
            else
            {
                // user_ids berilmagan - filterlar bo'yicha BARCHA userlarni tanlash
                _logger.LogInformation(
                    "No user_ids provided, filtering all users: is_active={IsActive}, is_stopped={IsStopped}, branch={Branch}",
                    query.IsActive, query.IsStopped, query.BranchGuid);

                userIds = await userService.GetFilteredUserIdsAsync(
                    query.IsActive,
                    query.IsStopped,
                    query.MinStoppedMinutes ?? 60, // Default: 1 soat
                    parsedDate,
                    startDateTime,
                    endDateTime,
                    query.BranchGuid);

                if (!userIds.Any())
                {
                    _logger.LogWarning("No users found matching filters");
                    return ServiceResult<IEnumerable<UserWithLocationsDto>>.Ok(
                        Enumerable.Empty<UserWithLocationsDto>(),
                        "Filterlar bo'yicha userlar topilmadi");
                }

                _logger.LogInformation("Found {Count} users matching filters", userIds.Count);
            }

            // Bir kunlik oraliq: parsedDate kunining 00:00:00 dan 23:59:59 gacha
            // DateTimeExtensions orqali markazlashtirilgan timezone management
            var (startDate, endDate) = parsedDate.ToDateRange();

            // Locationlarni olish
            var locations = await _locationRepository.GetMultipleUsersLocationsAsync(
                userIds,
                startDate,
                endDate,
                query.StartTime,
                query.EndTime,
                query.Limit
            );

            var locationDtos = _mapper.Map<IEnumerable<LocationResponseDto>>(locations);

            // Locationlarni user_id bo'yicha group qilish
            var locationsByUser = locationDtos
                                    .GroupBy(l => l.UserId)
                                    .ToDictionary(
                                        g => g.Key,
                                        g => g
                                            .OrderBy(l => l.RecordedAt) // ?? reverse
                                            .ToList()
                                    );


            // Har bir user uchun ma'lumotlarni va locationlarni birlashtirish
            var result = new List<UserWithLocationsDto>();

            foreach (var userId in userIds)
            {
                var user = await userService.GetByUserIdDtoAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: UserId={UserId}", userId);
                    continue;
                }

                // User'ning locationlarini olish
                var userLocations = locationsByUser.GetValueOrDefault(userId, new List<LocationResponseDto>());

                // Locationlarni clustering qilib, faqat cluster markazidagi locationlarni olish
                // Har bir cluster uchun 1 ta location qaytaradi (stopped_time bilan)
                var filteredLocations = userLocations.Any()
                    ? _clusteringService.GetFilteredLocationsWithStoppedTime(userLocations, userId)
                    : new List<LocationResponseDto>();

                var userWithLocations = new UserWithLocationsDto
                {
                    Id = user.Id,
                    UserId = user.UserId,  // ADDED: user_id field
                    Name = user.Name,
                    Phone = user.Phone,
                    BranchGuid = user.BranchGuid,
                    Branch = user.Branch,
                    Image = user.Image,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    Locations = filteredLocations  // Faqat cluster markazidagi locationlar (stopped_time bilan)
                };

                result.Add(userWithLocations);
            }

            // Filter info uchun message
            var filterParts = new List<string>();

            if (query.UserIds != null && query.UserIds.Any())
            {
                filterParts.Add($"user_ids={query.UserIds.Count}");
            }

            if (!string.IsNullOrWhiteSpace(query.BranchGuid))
            {
                filterParts.Add($"branch={query.BranchGuid}");
            }

            if (query.IsActive.HasValue)
            {
                filterParts.Add($"is_active={query.IsActive.Value}");
            }

            if (query.IsStopped.HasValue)
            {
                var stoppedText = query.IsStopped.Value ? "to'xtab turgan" : "harakat qilayotgan";
                filterParts.Add($"{stoppedText} ({query.MinStoppedMinutes ?? 20}min)");
            }

            if (!string.IsNullOrWhiteSpace(query.StartTime) && !string.IsNullOrWhiteSpace(query.EndTime))
            {
                filterParts.Add($"soat {query.StartTime}-{query.EndTime}");
            }

            string filterInfo = filterParts.Any()
                ? $"{userIds.Count} ta user ({string.Join(", ", filterParts)})"
                : $"BARCHA {userIds.Count} ta user";

            var totalFilteredLocations = result.Sum(u => u.Locations.Count);

            _logger.LogInformation(
                "Retrieved {FilteredCount} filtered locations (cluster representatives with stopped_time) for {FilterInfo} on Date={Date}",
                totalFilteredLocations, filterInfo, parsedDate.ToString("yyyy-MM-dd"));

            return ServiceResult<IEnumerable<UserWithLocationsDto>>.Ok(
                result,
                $"{filterInfo} uchun {parsedDate:yyyy-MM-dd} kunida {totalFilteredLocations} ta location olindi (clustering bilan)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locations for multiple users");
            return ServiceResult<IEnumerable<UserWithLocationsDto>>.ServerError(
                "Ko'p userlarning locationlarini olishda xatolik yuz berdi");
        }
    }

    public async Task<ServiceResult<LocationResponseDto>> CreateUserLocationAsync(int userId, ForTest locationData)
    {
        try
        {
            // FIXED: RecordedAt ni to'g'ri UTC'ga konvertatsiya qilish
            var recordedAtUtc = locationData.RecordedAt.ToApplicationTime();

            // User'ning oldingi location'ini olish (distance hisoblash uchun)
            var lastLocations = await _locationRepository.GetLastLocationsAsync(userId, 1);
            var previousLocation = lastLocations.FirstOrDefault();

            decimal? distanceFromPrevious = null;

            if (previousLocation != null)
            {
                var distance = _locationRepository.CalculateDistance(
                    previousLocation.Latitude,
                    previousLocation.Longitude,
                    locationData.Latitude,
                    locationData.Longitude
                );
                distanceFromPrevious = (decimal)distance;
            }



            // Location entity yaratish
            var location = new Location
            {
                UserId = userId,
                RecordedAt = recordedAtUtc,

                // Core location properties (REQUIRED)
                Latitude = locationData.Latitude,
                Longitude = locationData.Longitude,
                Speed = locationData.Speed,

                // Calculated fields
                DistanceFromPrevious = distanceFromPrevious,
                CreatedAt = DateTimeExtensions.NowInApplicationTime()
            };

            // Database'ga saqlash - ID bilan qaytadi
            var insertedId = await _locationRepository.InsertAsync(location);
            location.Id = insertedId;

            _logger.LogInformation("User {UserId} uchun location yaratildi, ID={LocationId}", userId, insertedId);

            // Response DTO yaratish
            var responseDto = _mapper.Map<LocationResponseDto>(location);

            // Telegram kanalga xabar yuborish
            if (_telegramService != null)
            {
                try
                {
                    var user = await _userRepository.SelectAsync(u => u.UserId == userId);
                    await _telegramService.SendLocationDataAsync(
                        userId,
                        $"User {user.Name}",
                        double.Parse(responseDto.Latitude.ToString()),
                        double.Parse(responseDto.Longitude.ToString()),
                        responseDto.RecordedAt
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send Telegram notification for UserId={UserId}", userId);
                }
            }

            return ServiceResult<LocationResponseDto>.Created(responseDto, "Location muvaffaqiyatli yaratildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating location for UserId={UserId}", userId);
            return ServiceResult<LocationResponseDto>.ServerError(
                "Location yaratishda xatolik yuz berdi");
        }
    }

    public async Task<ServiceResult<IList<LocationResponseDto>>>
        CreateUserLocationsAsync(int userId, IList<ForTest> locationsData)
    {
        try
        {
            if (locationsData == null || !locationsData.Any())
                return ServiceResult<IList<LocationResponseDto>>
                    .BadRequest("Locations ro'yxati bo'sh");

            // RecordedAt bo‘yicha tartiblash
            var ordered = locationsData
                .OrderBy(x => x.RecordedAt)
                .ToList();

            // Oxirgi mavjud location
            var lastLocations = await _locationRepository
                .GetLastLocationsAsync(userId, 1);

            var previousLocation = lastLocations.FirstOrDefault();

            var newLocations = new List<Location>();

            foreach (var item in ordered)
            {
                var recordedAtUtc = item.RecordedAt.ToApplicationTime();

                decimal? distanceFromPrevious = null;

                if (previousLocation != null)
                {
                    var distance = _locationRepository.CalculateDistance(
                        previousLocation.Latitude,
                        previousLocation.Longitude,
                        item.Latitude,
                        item.Longitude
                    );

                    distanceFromPrevious = (decimal)distance;
                }

                var location = new Location
                {
                    UserId = userId,
                    Mock = item.Mock,
                    RecordedAt = recordedAtUtc,
                    Latitude = item.Latitude,
                    Longitude = item.Longitude,
                    Speed = item.Speed,
                    DistanceFromPrevious = distanceFromPrevious,
                    CreatedAt = DateTimeExtensions.NowInApplicationTime()
                };

                newLocations.Add(location);

                // keyingi aylanish uchun previous yangilanadi
                previousLocation = location;
            }

            // 🔥 Bulk insert
            var ids = await _locationRepository.BulkInsertAsync(newLocations);

            for (int i = 0; i < newLocations.Count; i++)
            {
                newLocations[i].Id = ids[i];
            }

            var response = _mapper
                .Map<IList<LocationResponseDto>>(newLocations);

            // Telegram faqat oxirgi location uchun
            if (_telegramService != null && response.Any())
            {
                try
                {
                    var last = response.Last();
                    var user = await _userRepository
                        .SelectAsync(u => u.UserId == userId);

                    await _telegramService.SendLocationDataAsync(
                        userId,
                        $"User {user.Name}",
                        double.Parse(last.Latitude.ToString()),
                        double.Parse(last.Longitude.ToString()),
                        last.RecordedAt
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to send Telegram for bulk UserId={UserId}",
                        userId);
                }
            }

            return ServiceResult<IList<LocationResponseDto>>
                .Created(response, "Locations muvaffaqiyatli yaratildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating bulk locations for UserId={UserId}",
                userId);

            return ServiceResult<IList<LocationResponseDto>>
                .ServerError("Locations yaratishda xatolik yuz berdi");
        }
    }
}
