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
    private readonly ILogger<LocationService> _logger;
    private readonly object? _locationHubContext;

    public LocationService(
        ILocationRepository locationRepository,
        ILogger<LocationService> logger,
        object? locationHubContext = null)
    {
        _locationRepository = locationRepository;
        _logger = logger;
        _locationHubContext = locationHubContext;
    }


    /// <summary>
    /// Bitta user uchun ko'p location yaratish (userId + locations array)
    /// </summary>
    public async Task<ServiceResult<IEnumerable<LocationResponseDto>>> CreateUserLocationBatchAsync(UserLocationBatchDto dto)
    {
        try
        {
            if (!dto.Locations.Any())
            {
                return ServiceResult<IEnumerable<LocationResponseDto>>.BadRequest(
                    "Locations array bo'sh bo'lmasligi kerak");
            }

            // Vaqt bo'yicha sort qilish
            var sortedLocations = dto.Locations.OrderBy(l => l.RecordedAt).ToList();

            var locations = new List<Location>();
            var responseDtos = new List<LocationResponseDto>();
            Location? previousLocation = null;

            // User'ning oldingi location'ini olish
            var lastLocations = await _locationRepository.GetLastLocationsAsync(dto.UserId, 1);
            previousLocation = lastLocations.FirstOrDefault();

            foreach (var locDto in sortedLocations)
            {
                decimal? distanceFromPrevious = null;

                if (previousLocation != null)
                {
                    var distance = _locationRepository.CalculateDistance(
                        previousLocation.Latitude,
                        previousLocation.Longitude,
                        locDto.Latitude,
                        locDto.Longitude
                    );
                    distanceFromPrevious = (decimal)distance;
                }

                var location = new Location
                {
                    UserId = dto.UserId,
                    RecordedAt = locDto.RecordedAt,
                    Latitude = locDto.Latitude,
                    Longitude = locDto.Longitude,
                    Accuracy = locDto.Accuracy,
                    Speed = locDto.Speed,
                    Heading = locDto.Heading,
                    Altitude = locDto.Altitude,
                    ActivityType = locDto.ActivityType,
                    ActivityConfidence = locDto.ActivityConfidence,
                    IsMoving = locDto.IsMoving,
                    BatteryLevel = locDto.BatteryLevel,
                    IsCharging = locDto.IsCharging ?? false,
                    DistanceFromPrevious = distanceFromPrevious,
                    CreatedAt = DateTime.UtcNow
                };

                locations.Add(location);
                previousLocation = location;
            }

            // Database'ga saqlash - ID'lari bilan qaytadi
            var insertedLocations = await _locationRepository.InsertBatchAsync(locations);
            var insertedLocationsList = insertedLocations.ToList();
            _logger.LogInformation("User {UserId} uchun {Count} ta location yaratildi", dto.UserId, insertedLocationsList.Count);

            // Response DTO'lar yaratish - database'dan qaytgan ID'lar bilan
            foreach (var location in insertedLocationsList)
            {
                var responseDto = MapToDto(location);
                responseDtos.Add(responseDto);

                // SignalR orqali real-time broadcast
                if (_locationHubContext != null)
                {
                    try
                    {
                        dynamic hubContext = _locationHubContext;

                        // Specific user'ni track qilayotganlarga
                        await hubContext.Clients.Group($"user_{dto.UserId}")
                            .SendAsync("LocationUpdated", responseDto);

                        // Barcha user'larni track qilayotganlarga
                        await hubContext.Clients.Group("all_users")
                            .SendAsync("LocationUpdated", responseDto);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to broadcast location via SignalR for UserId={UserId}", dto.UserId);
                    }
                }
            }

            var message = responseDtos.Count == 1
                ? "Location muvaffaqiyatli yaratildi"
                : $"{responseDtos.Count} ta location muvaffaqiyatli yaratildi";

            return ServiceResult<IEnumerable<LocationResponseDto>>.Created(responseDtos, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user location batch for UserId={UserId}", dto.UserId);
            return ServiceResult<IEnumerable<LocationResponseDto>>.ServerError(
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

            var result = locations.Select(MapToDto);
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
            var result = locations.Select(MapToDto).ToList();

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
                MapToDto(location),
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
    /// Location entity'ni DTO'ga mapping
    /// </summary>
    private static LocationResponseDto MapToDto(Location location)
    {
        return new LocationResponseDto
        {
            Id = location.Id,
            UserId = location.UserId,
            RecordedAt = location.RecordedAt,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Accuracy = location.Accuracy,
            Speed = location.Speed,
            Heading = location.Heading,
            Altitude = location.Altitude,
            ActivityType = location.ActivityType,
            ActivityConfidence = location.ActivityConfidence,
            IsMoving = location.IsMoving,
            BatteryLevel = location.BatteryLevel,
            IsCharging = location.IsCharging,
            DistanceFromPrevious = location.DistanceFromPrevious,
            CreatedAt = location.CreatedAt
        };
    }
}
