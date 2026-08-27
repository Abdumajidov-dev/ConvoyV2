namespace Convoy.Service.Interfaces;

public interface IOsrmService
{
    /// <summary>
    /// Yo'l bo'yicha masofani hisoblash (OSRM orqali, metrda)
    /// Agar OSRM ishlamasa - null qaytaradi (Haversine fallback uchun)
    /// </summary>
    Task<double?> GetRoadDistanceAsync(decimal lat1, decimal lon1, decimal lat2, decimal lon2);
}
