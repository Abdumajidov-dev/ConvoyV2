using Convoy.Service.DTOs;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services;

/// <summary>
/// Location Clustering Service
/// 10 metr oralig'dagi locationlarni groupalaydi va stopped_time hisoblab beradi
/// </summary>
public class LocationClusteringService
{
    private readonly ILogger<LocationClusteringService> _logger;
    private const double ClusterRadiusMeters = 10.0; // 10 metr

    public LocationClusteringService(ILogger<LocationClusteringService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Locationlarni 10 metr oralig'ida clustering qiladi
    /// </summary>
    public List<ClusteredLocationDto> ClusterLocations(List<LocationResponseDto> locations, int userId)
    {
        if (locations == null || !locations.Any())
        {
            _logger.LogWarning("ClusterLocations: locations list bo'sh yoki null");
            return new List<ClusteredLocationDto>();
        }

        // Vaqt bo'yicha tartiblash (eskidan yangiga)
        var sortedLocations = locations.OrderBy(l => l.RecordedAt).ToList();

        var clusters = new List<ClusteredLocationDto>();
        var currentCluster = new List<LocationResponseDto>();
        int clusterId = 1;

        for (int i = 0; i < sortedLocations.Count; i++)
        {
            var currentLocation = sortedLocations[i];

            if (currentCluster.Count == 0)
            {
                // Yangi cluster boshlanadi
                currentCluster.Add(currentLocation);
            }
            else
            {
                // Hozirgi cluster markazidan masofani hisoblash
                var clusterCenter = GetClusterCenter(currentCluster);
                var distance = CalculateDistance(
                    (double)clusterCenter.Latitude,
                    (double)clusterCenter.Longitude,
                    (double)currentLocation.Latitude,
                    (double)currentLocation.Longitude
                );

                if (distance <= ClusterRadiusMeters)
                {
                    // O'sha cluster ichida qoladi
                    currentCluster.Add(currentLocation);
                }
                else
                {
                    // Yangi cluster boshlanadi - avvalgi clusterni saqlash
                    if (currentCluster.Any())
                    {
                        clusters.Add(CreateClusterDto(currentCluster, clusterId, userId));
                        clusterId++;
                    }

                    // Yangi cluster
                    currentCluster = new List<LocationResponseDto> { currentLocation };
                }
            }
        }

        // Oxirgi clusterni qo'shish
        if (currentCluster.Any())
        {
            clusters.Add(CreateClusterDto(currentCluster, clusterId, userId));
        }

        _logger.LogInformation($"Clustering completed: {locations.Count} locations -> {clusters.Count} clusters");
        return clusters;
    }

    /// <summary>
    /// Locationlar uchun stopped_time hisoblab beradi
    /// Har bir location oldingisi bilan bir xil joyda bo'lgan vaqtni hisoblaydi
    /// DEPRECATED: GetFilteredLocationsWithStoppedTime ishlatiladi
    /// </summary>
    public List<LocationResponseDto> CalculateStoppedTime(List<LocationResponseDto> locations)
    {
        if (locations == null || locations.Count <= 1)
            return locations ?? new List<LocationResponseDto>();

        // Vaqt bo'yicha tartiblash
        var sortedLocations = locations.OrderBy(l => l.RecordedAt).ToList();

        for (int i = 0; i < sortedLocations.Count; i++)
        {
            if (i == 0)
            {
                sortedLocations[i].StoppedTime = "00:00";
                continue;
            }

            var current = sortedLocations[i];
            var previous = sortedLocations[i - 1];

            // Masofani hisoblash
            var distance = CalculateDistance(
                (double)previous.Latitude,
                (double)previous.Longitude,
                (double)current.Latitude,
                (double)current.Longitude
            );

            // Agar 10 metr ichida bo'lsa - stopped time hisoblanadi
            if (distance <= ClusterRadiusMeters)
            {
                // Previous stopped time'ni parse qilish
                var previousMinutes = 0;
                if (!string.IsNullOrEmpty(previous.StoppedTime))
                {
                    var parts = previous.StoppedTime.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var mins))
                    {
                        previousMinutes = hours * 60 + mins;
                    }
                }

                var stoppedMinutes = (int)(current.RecordedAt - previous.RecordedAt).TotalMinutes;
                var totalMinutes = previousMinutes + stoppedMinutes;
                var h = totalMinutes / 60;
                var m = totalMinutes % 60;
                current.StoppedTime = $"{h:D2}:{m:D2}";
            }
            else
            {
                // Yangi joyga o'tdi - stopped time 0 dan boshlanadi
                current.StoppedTime = "00:00";
            }
        }

        return sortedLocations;
    }

    /// <summary>
    /// Cluster DTO yaratish
    /// </summary>
    private ClusteredLocationDto CreateClusterDto(List<LocationResponseDto> clusterLocations, int clusterId, int userId)
    {
        var center = GetClusterCenter(clusterLocations);
        var startTime = clusterLocations.Min(l => l.RecordedAt);
        var endTime = clusterLocations.Max(l => l.RecordedAt);
        var stoppedMinutes = (int)(endTime - startTime).TotalMinutes;

        return new ClusteredLocationDto
        {
            ClusterId = clusterId,
            UserId = userId,
            Latitude = center.Latitude,
            Longitude = center.Longitude,
            StartTime = startTime,
            EndTime = endTime,
            StoppedTime = stoppedMinutes,
            LocationCount = clusterLocations.Count,
            Locations = clusterLocations // Agar kerak bo'lsa locations ham qaytariladi
        };
    }

    /// <summary>
    /// Cluster markazini hisoblash (o'rtacha koordinatalar)
    /// </summary>
    private (decimal Latitude, decimal Longitude) GetClusterCenter(List<LocationResponseDto> locations)
    {
        if (!locations.Any())
            return (0, 0);

        var avgLat = locations.Average(l => l.Latitude);
        var avgLon = locations.Average(l => l.Longitude);

        return (avgLat, avgLon);
    }

    /// <summary>
    /// Haversine formula - ikki koordinata orasidagi masofani hisoblash (metrda)
    /// </summary>
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        var distanceKm = earthRadiusKm * c;
        return distanceKm * 1000; // metrga o'girish
    }

    private double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    /// <summary>
    /// Locationlarni clustering qilib, har bir cluster uchun FAQAT 1 ta location qaytaradi
    /// (cluster markazidagi eng yaqin location + stopped_time)
    /// </summary>
    public List<LocationResponseDto> GetFilteredLocationsWithStoppedTime(List<LocationResponseDto> locations, int userId)
    {
        if (locations == null || !locations.Any())
        {
            return new List<LocationResponseDto>();
        }

        // Clustering qilish
        var clusters = ClusterLocations(locations, userId);

        var filteredLocations = new List<LocationResponseDto>();

        foreach (var cluster in clusters)
        {
            // Cluster markaziga eng yaqin locationni topish
            var clusterCenter = (cluster.Latitude, cluster.Longitude);

            LocationResponseDto? closestLocation = null;
            double minDistance = double.MaxValue;

            foreach (var location in locations.Where(l => l.RecordedAt >= cluster.StartTime && l.RecordedAt <= cluster.EndTime))
            {
                var distance = CalculateDistance(
                    (double)clusterCenter.Latitude,
                    (double)clusterCenter.Longitude,
                    (double)location.Latitude,
                    (double)location.Longitude
                );

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestLocation = location;
                }
            }

            if (closestLocation != null)
            {
                // Stopped time qo'shish (daqiqalarni HH:mm formatga o'tkazish)
                var hours = cluster.StoppedTime / 60;
                var minutes = cluster.StoppedTime % 60;
                closestLocation.StoppedTime = $"{hours:D2}:{minutes:D2}";
                filteredLocations.Add(closestLocation);
            }
        }

        _logger.LogInformation($"Filtered {locations.Count} locations to {filteredLocations.Count} cluster representatives");

        return filteredLocations;
    }
}
