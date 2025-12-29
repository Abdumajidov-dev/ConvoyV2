using Convoy.Service.Common;
using Convoy.Service.DTOs;

namespace Convoy.Service.Interfaces;

/// <summary>
/// Location service interface
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// User uchun bitta location yaratish (userId JWT tokendan, location data body'dan)
    /// Body to'g'ridan-to'g'ri LocationDataDto (encryption middleware yechib beradi)
    /// </summary>
    Task<ServiceResult<LocationResponseDto>> CreateUserLocationAsync(int userId, LocationDataDto locationData);

    /// <summary>
    /// User location'larini olish
    /// </summary>
    Task<ServiceResult<IEnumerable<LocationResponseDto>>> GetUserLocationsAsync(LocationQueryDto query);

    /// <summary>
    /// User'ning oxirgi location'larini olish
    /// </summary>
    Task<ServiceResult<IEnumerable<LocationResponseDto>>> GetLastLocationsAsync(int userId, int count = 100);

    /// <summary>
    /// Barcha userlarning oxirgi location'larini olish
    /// </summary>
    Task<ServiceResult<IEnumerable<LocationResponseDto>>> GetAllUsersLatestLocationsAsync();

    /// <summary>
    /// Kunlik statistikalarni olish
    /// </summary>
    Task<ServiceResult<IEnumerable<DailyStatisticsDto>>> GetDailyStatisticsAsync(DailySummaryQueryDto query);

    /// <summary>
    /// Location by ID olish
    /// </summary>
    Task<ServiceResult<LocationResponseDto>> GetLocationByIdAsync(long id, DateTime recordedAt);
}
