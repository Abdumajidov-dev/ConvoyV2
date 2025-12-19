using Convoy.Service.DTOs;

namespace Convoy.Service.Interfaces;

/// <summary>
/// Location service interface
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Yangi location yaratish
    /// </summary>
    Task<LocationResponseDto> CreateLocationAsync(CreateLocationDto dto);

    /// <summary>
    /// Batch location yaratish
    /// </summary>
    Task<int> CreateLocationBatchAsync(CreateLocationBatchDto dto);

    /// <summary>
    /// User location'larini olish
    /// </summary>
    Task<IEnumerable<LocationResponseDto>> GetUserLocationsAsync(LocationQueryDto query);

    /// <summary>
    /// User'ning oxirgi location'larini olish
    /// </summary>
    Task<IEnumerable<LocationResponseDto>> GetLastLocationsAsync(int userId, int count = 100);

    /// <summary>
    /// Kunlik statistikalarni olish
    /// </summary>
    Task<IEnumerable<DailyStatisticsDto>> GetDailyStatisticsAsync(DailySummaryQueryDto query);

    /// <summary>
    /// Location by ID olish
    /// </summary>
    Task<LocationResponseDto?> GetLocationByIdAsync(long id, DateTime recordedAt);
}
