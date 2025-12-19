using Convoy.Data.IRepositories;
using Convoy.Domain.Entities;
using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services;

/// <summary>
/// Location service implementation
/// </summary>
public class LocationService : ILocationService
{
    private readonly ILocationRepository _locationRepository;
    private readonly ILogger<LocationService> _logger;

    public LocationService(
        ILocationRepository locationRepository,
        ILogger<LocationService> logger)
    {
        _locationRepository = locationRepository;
        _logger = logger;
    }

    /// <summary>
    /// Yangi location yaratish
    /// </summary>
    public async Task<LocationResponseDto> CreateLocationAsync(CreateLocationDto dto)
    {
        try
        {
            // Oldingi location'ni olish (masofa hisoblash uchun)
            var previousLocations = await _locationRepository.GetLastLocationsAsync(dto.UserId, 1);
            var previousLocation = previousLocations.FirstOrDefault();

            decimal? distanceFromPrevious = null;
            if (previousLocation != null)
            {
                var distance = _locationRepository.CalculateDistance(
                    previousLocation.Latitude,
                    previousLocation.Longitude,
                    dto.Latitude,
                    dto.Longitude
                );
                distanceFromPrevious = (decimal)distance;
            }

            // Location entity yaratish
            var location = new Location
            {
                UserId = dto.UserId,
                RecordedAt = dto.RecordedAt,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Accuracy = dto.Accuracy,
                Speed = dto.Speed,
                Heading = dto.Heading,
                Altitude = dto.Altitude,
                ActivityType = dto.ActivityType,
                ActivityConfidence = dto.ActivityConfidence,
                IsMoving = dto.IsMoving,
                BatteryLevel = dto.BatteryLevel,
                IsCharging = dto.IsCharging,
                DistanceFromPrevious = distanceFromPrevious,
                CreatedAt = DateTime.UtcNow
            };

            var id = await _locationRepository.InsertAsync(location);
            location.Id = id;

            _logger.LogInformation("Location created for UserId={UserId}, Distance={Distance}m",
                dto.UserId, distanceFromPrevious);

            return MapToDto(location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating location for UserId={UserId}", dto.UserId);
            throw;
        }
    }

    /// <summary>
    /// Batch location yaratish
    /// </summary>
    public async Task<int> CreateLocationBatchAsync(CreateLocationBatchDto dto)
    {
        try
        {
            if (!dto.Locations.Any())
            {
                return 0;
            }

            // Vaqt bo'yicha sort qilish
            var sortedLocations = dto.Locations.OrderBy(l => l.RecordedAt).ToList();

            var locations = new List<Location>();
            Location? previousLocation = null;

            // Birinchi user'ning oldingi location'ini olish
            var firstUserId = sortedLocations.First().UserId;
            var lastLocations = await _locationRepository.GetLastLocationsAsync(firstUserId, 1);
            previousLocation = lastLocations.FirstOrDefault();

            foreach (var locDto in sortedLocations)
            {
                decimal? distanceFromPrevious = null;

                if (previousLocation != null && previousLocation.UserId == locDto.UserId)
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
                    UserId = locDto.UserId,
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
                    IsCharging = locDto.IsCharging,
                    DistanceFromPrevious = distanceFromPrevious,
                    CreatedAt = DateTime.UtcNow
                };

                locations.Add(location);
                previousLocation = location;
            }

            var count = await _locationRepository.InsertBatchAsync(locations);
            _logger.LogInformation("Batch created {Count} locations", count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating batch locations");
            throw;
        }
    }

    /// <summary>
    /// User location'larini olish
    /// </summary>
    public async Task<IEnumerable<LocationResponseDto>> GetUserLocationsAsync(LocationQueryDto query)
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

            return locations.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locations for UserId={UserId}", query.UserId);
            throw;
        }
    }

    /// <summary>
    /// Oxirgi location'larni olish
    /// </summary>
    public async Task<IEnumerable<LocationResponseDto>> GetLastLocationsAsync(int userId, int count = 100)
    {
        try
        {
            var locations = await _locationRepository.GetLastLocationsAsync(userId, count);
            return locations.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last locations for UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Kunlik statistikalarni olish
    /// </summary>
    public async Task<IEnumerable<DailyStatisticsDto>> GetDailyStatisticsAsync(DailySummaryQueryDto query)
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
            }).OrderBy(s => s.Date);

            _logger.LogInformation("Retrieved daily statistics for UserId={UserId}, Days={Days}",
                query.UserId, statistics.Count());

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily statistics for UserId={UserId}", query.UserId);
            throw;
        }
    }

    /// <summary>
    /// ID orqali location olish
    /// </summary>
    public async Task<LocationResponseDto?> GetLocationByIdAsync(long id, DateTime recordedAt)
    {
        try
        {
            var location = await _locationRepository.GetByIdAsync(id, recordedAt);
            return location != null ? MapToDto(location) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location by Id={Id}", id);
            throw;
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
