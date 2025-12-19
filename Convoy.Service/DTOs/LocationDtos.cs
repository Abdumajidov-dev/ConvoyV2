namespace Convoy.Service.DTOs;

/// <summary>
/// Location yaratish uchun DTO
/// </summary>
public class CreateLocationDto
{
    public int UserId { get; set; }
    public DateTime RecordedAt { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? Accuracy { get; set; }
    public decimal? Speed { get; set; }
    public decimal? Heading { get; set; }
    public decimal? Altitude { get; set; }
    public string? ActivityType { get; set; }
    public int? ActivityConfidence { get; set; }
    public bool IsMoving { get; set; } = false;
    public int? BatteryLevel { get; set; }
    public bool? IsCharging { get; set; }
}

/// <summary>
/// Batch location yaratish uchun DTO
/// </summary>
public class CreateLocationBatchDto
{
    public List<CreateLocationDto> Locations { get; set; } = new();
}

/// <summary>
/// Location response DTO
/// </summary>
public class LocationResponseDto
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public DateTime RecordedAt { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? Accuracy { get; set; }
    public decimal? Speed { get; set; }
    public decimal? Heading { get; set; }
    public decimal? Altitude { get; set; }
    public string? ActivityType { get; set; }
    public int? ActivityConfidence { get; set; }
    public bool IsMoving { get; set; }
    public int? BatteryLevel { get; set; }
    public bool? IsCharging { get; set; }
    public decimal? DistanceFromPrevious { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Kunlik statistika DTO
/// </summary>
public class DailyStatisticsDto
{
    public DateTime Date { get; set; }
    public decimal TotalDistanceMeters { get; set; }
    public decimal TotalDistanceKilometers => Math.Round(TotalDistanceMeters / 1000, 2);
    public int LocationCount { get; set; }
}

/// <summary>
/// Location query uchun filter DTO
/// </summary>
public class LocationQueryDto
{
    public int UserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? Limit { get; set; } = 100;
}

/// <summary>
/// Daily summary query DTO
/// </summary>
public class DailySummaryQueryDto
{
    public int UserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
