using Convoy.Domain.Entities;

namespace Convoy.Data.IRepositories;

/// <summary>
/// Location repository interface - Dapper bilan ishleydi
/// </summary>
public interface ILocationRepository
{
    /// <summary>
    /// Yangi location yozish (partitioned table'ga)
    /// </summary>
    Task<long> InsertAsync(Location location);

    /// <summary>
    /// Bir nechta location'larni batch insert - yaratilgan location'larni ID bilan qaytaradi
    /// </summary>
    Task<IEnumerable<Location>> InsertBatchAsync(IEnumerable<Location> locations);

    /// <summary>
    /// User'ning location'larini vaqt oralig'ida olish
    /// </summary>
    Task<IEnumerable<Location>> GetUserLocationsAsync(int userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// User'ning oxirgi N ta location'ini olish
    /// </summary>
    Task<IEnumerable<Location>> GetLastLocationsAsync(int userId, int count = 100);

    /// <summary>
    /// User'ning kunlik yo'l masofalari (summary statistics)
    /// </summary>
    Task<Dictionary<DateTime, decimal>> GetDailyDistancesAsync(int userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// ID orqali bitta location olish
    /// </summary>
    Task<Location?> GetByIdAsync(long id, DateTime recordedAt);

    /// <summary>
    /// Partition yaratish
    /// </summary>
    Task<string> CreatePartitionAsync(DateTime targetMonth);

    /// <summary>
    /// Mavjud partition'larni tekshirish
    /// </summary>
    Task<IEnumerable<string>> GetExistingPartitionsAsync();

    /// <summary>
    /// Ikki nuqta orasidagi masofani hisoblash (Haversine formula)
    /// </summary>
    double CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2);
}
