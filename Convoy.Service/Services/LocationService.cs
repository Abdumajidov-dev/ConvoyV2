using AutoMapper;
using Convoy.Data.IRepositories;
using Convoy.Domain.Entities;
using Convoy.Service.Common;
using Convoy.Service.DTOs;
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
    private readonly object? _locationHubContext;
    private readonly ITelegramService? _telegramService;

    public LocationService(
        ILocationRepository locationRepository,
        IMapper mapper,
        ILogger<LocationService> logger,
        object? locationHubContext = null,
        ITelegramService? telegramService = null)
    {
        _locationRepository = locationRepository;
        _mapper = mapper;
        _logger = logger;
        _locationHubContext = locationHubContext;
        _telegramService = telegramService;
    }


    /// <summary>
    /// Bitta user uchun bitta location yaratish (userId controller'dan, location data body'dan)
    /// </summary>
    public async Task<ServiceResult<LocationResponseDto>> CreateUserLocationAsync(int userId, LocationDataDto locationData)
    {
        try
        {
            // RecordedAt bo'lmasa - hozirgi vaqtni set qilish
            if (!locationData.RecordedAt.HasValue)
            {
                //locationData.RecordedAt = DateTime.UtcNow;
                throw new CustomException(400, "recorded vaqtini berish majburish");
            }

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
                RecordedAt = locationData.RecordedAt.Value,

                // Core location properties (REQUIRED)
                Latitude = locationData.Latitude,
                Longitude = locationData.Longitude,

                // Core location properties (OPTIONAL)
                Accuracy = locationData.Accuracy,
                Speed = locationData.Speed,
                Heading = locationData.Heading,
                Altitude = locationData.Altitude,

                // Flutter Background Geolocation - Extended Coords (OPTIONAL)
                EllipsoidalAltitude = locationData.EllipsoidalAltitude,
                HeadingAccuracy = locationData.HeadingAccuracy,
                SpeedAccuracy = locationData.SpeedAccuracy,
                AltitudeAccuracy = locationData.AltitudeAccuracy,
                Floor = locationData.Floor,

                // Activity (OPTIONAL)
                ActivityType = locationData.ActivityType,
                ActivityConfidence = locationData.ActivityConfidence,
                IsMoving = locationData.IsMoving ?? false,

                // Battery (OPTIONAL)
                BatteryLevel = locationData.BatteryLevel,
                IsCharging = locationData.IsCharging ?? false,

                // Flutter Background Geolocation - Location metadata (OPTIONAL)
                Timestamp = locationData.Timestamp,
                Age = locationData.Age,
                Event = locationData.Event,
                Mock = locationData.Mock,
                Sample = locationData.Sample,
                Odometer = locationData.Odometer,
                Uuid = locationData.Uuid,
                Extras = locationData.Extras,

                // Calculated fields
                DistanceFromPrevious = distanceFromPrevious,
                CreatedAt = DateTime.UtcNow
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
                    await _telegramService.SendLocationDataAsync(
                        userId,
                        $"User {userId}",
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
            var startDate = query.Date.Date;  // 00:00:00
            var endDate = startDate.AddDays(1);  // Keyingi kunning 00:00:00

            var locations = await _locationRepository.GetUserLocationsAsync(
                userId,
                startDate,
                endDate,
                query.StartTime,
                query.EndTime
            );

            var result = _mapper.Map<IEnumerable<LocationResponseDto>>(locations)
                        .OrderByDescending(l => l.RecordedAt) // ?? reverse
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

            // Date string'ni parse qilish (format: "2026-01-07 03:54:32.302400" yoki "2026-01-07")
            if (string.IsNullOrWhiteSpace(query.Date))
            {
                return ServiceResult<IEnumerable<UserWithLocationsDto>>.BadRequest(
                    "date field bo'sh bo'lmasligi kerak");
            }

            DateTime parsedDate;
            try
            {
                // Agar date ichida space bo'lsa (timestamp format), faqat date qismini olish
                var dateString = query.Date.Contains(' ')
                    ? query.Date.Split(' ')[0]
                    : query.Date;

                parsedDate = DateTime.Parse(dateString);
            }
            catch (FormatException)
            {
                return ServiceResult<IEnumerable<UserWithLocationsDto>>.BadRequest(
                    "date formati noto'g'ri (kutilgan format: 'yyyy-MM-dd' yoki 'yyyy-MM-dd HH:mm:ss')");
            }

            List<int> userIds;

            // user_ids, branch_guid yoki barcha userlar bo'yicha userlarni aniqlash
            if (query.UserIds != null && query.UserIds.Any())
            {
                // user_ids berilgan - to'g'ridan-to'g'ri ishlatish
                userIds = query.UserIds;
                _logger.LogInformation("Using provided user_ids: {UserIds}", string.Join(",", userIds));
            }
            else if (!string.IsNullOrWhiteSpace(query.BranchGuid))
            {
                // branch_guid berilgan - branch'ga tegishli userlarni topish
                userIds = await userService.GetUserIdsByBranchGuidAsync(query.BranchGuid);

                if (!userIds.Any())
                {
                    _logger.LogWarning("No users found for BranchGuid={BranchGuid}", query.BranchGuid);
                    return ServiceResult<IEnumerable<UserWithLocationsDto>>.Ok(
                        Enumerable.Empty<UserWithLocationsDto>(),
                        $"BranchGuid={query.BranchGuid} uchun userlar topilmadi");
                }

                _logger.LogInformation("Found {Count} users for BranchGuid={BranchGuid}", userIds.Count, query.BranchGuid);
            }
            else
            {
                // Ikkalasi ham null - BARCHA active userlarni olish
                var allUsers = await userService.GetAllActiveUsersAsync();
                userIds = allUsers.Select(u => (int)u.Id).ToList();

                if (!userIds.Any())
                {
                    _logger.LogWarning("No active users found in database");
                    return ServiceResult<IEnumerable<UserWithLocationsDto>>.Ok(
                        Enumerable.Empty<UserWithLocationsDto>(),
                        "Active userlar topilmadi");
                }

                _logger.LogInformation("Fetching locations for ALL {Count} active users", userIds.Count);
            }

            // Bir kunlik oraliq: parsedDate kunining 00:00:00 dan 23:59:59 gacha
            var startDate = parsedDate.Date;  // 00:00:00
            var endDate = startDate.AddDays(1);  // Keyingi kunning 00:00:00

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
                                            .OrderByDescending(l => l.RecordedAt) // ?? reverse
                                            .ToList()
                                    );


            // Har bir user uchun ma'lumotlarni va locationlarni birlashtirish
            var result = new List<UserWithLocationsDto>();

            foreach (var userId in userIds)
            {
                var user = await userService.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: UserId={UserId}", userId);
                    continue;
                }

                var userWithLocations = new UserWithLocationsDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Phone = user.Phone,
                    BranchGuid = user.BranchGuid,
                    Branch = user.Branch,
                    Image = user.Image,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    Locations = locationsByUser.GetValueOrDefault(userId, new List<LocationResponseDto>())
                };

                result.Add(userWithLocations);
            }

            // Filter info uchun message
            string filterInfo;
            if (query.UserIds != null && query.UserIds.Any())
            {
                filterInfo = $"{userIds.Count} ta user (user_ids)";
            }
            else if (!string.IsNullOrWhiteSpace(query.BranchGuid))
            {
                filterInfo = $"{userIds.Count} ta user (branch_guid={query.BranchGuid})";
            }
            else
            {
                filterInfo = $"BARCHA {userIds.Count} ta active user";
            }

            var totalLocations = result.Sum(u => u.Locations.Count);

            _logger.LogInformation("Retrieved {Count} locations for {FilterInfo} on Date={Date}",
                totalLocations, filterInfo, parsedDate.ToString("yyyy-MM-dd"));

            return ServiceResult<IEnumerable<UserWithLocationsDto>>.Ok(
                result,
                $"{filterInfo} uchun {parsedDate:yyyy-MM-dd} kunida {totalLocations} ta location olindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locations for multiple users");
            return ServiceResult<IEnumerable<UserWithLocationsDto>>.ServerError(
                "Ko'p userlarning locationlarini olishda xatolik yuz berdi");
        }
    }

}
