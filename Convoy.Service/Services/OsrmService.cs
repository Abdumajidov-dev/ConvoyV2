using System.Text.Json;
using Convoy.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services;

public class OsrmService : IOsrmService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly ILogger<OsrmService> _logger;

    public OsrmService(HttpClient httpClient, IConfiguration configuration, ILogger<OsrmService> logger)
    {
        _httpClient = httpClient;
        _baseUrl = (configuration["Osrm:BaseUrl"] ?? "http://router.project-osrm.org").TrimEnd('/');
        _logger = logger;
    }

    public async Task<double?> GetRoadDistanceAsync(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        try
        {
            // OSRM koordinata formati: longitude,latitude (avval lon, keyin lat!)
            var url = $"{_baseUrl}/route/v1/driving/{lon1},{lat1};{lon2},{lat2}?overview=false";

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync(url, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OSRM {StatusCode} ({Lat1},{Lon1})→({Lat2},{Lon2})",
                    response.StatusCode, lat1, lon1, lat2, lon2);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.GetProperty("code").GetString() != "Ok")
            {
                _logger.LogWarning("OSRM code != Ok: ({Lat1},{Lon1})→({Lat2},{Lon2})", lat1, lon1, lat2, lon2);
                return null;
            }

            var routes = root.GetProperty("routes");
            if (routes.GetArrayLength() == 0)
                return null;

            return routes[0].GetProperty("distance").GetDouble();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OSRM xatolik ({Lat1},{Lon1})→({Lat2},{Lon2}), Haversine fallback ishlatiladi",
                lat1, lon1, lat2, lon2);
            return null;
        }
    }
}
