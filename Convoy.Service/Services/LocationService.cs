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
                locationData.RecordedAt = DateTime.UtcNow;
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
    /// User location'larini olish
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
                    query.EndDate.Value
                );
            }
            else
            {
                locations = await _locationRepository.GetLastLocationsAsync(
                    query.UserId,
                    query.Limit ?? 100
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

}
