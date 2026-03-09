using Newtonsoft.Json;

namespace Convoy.Service.DTOs;

/// <summary>
/// Kunlik masofa hisoboti response DTO
/// </summary>
public class DailyDistanceReportDto
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("user_id")]
    public int? UserId { get; set; }  // PHP API worker_id

    [JsonProperty("user_name")]
    public string? UserName { get; set; }

    [JsonProperty("phone")]
    public string? Phone { get; set; }

    [JsonProperty("user_image")]
    public string? UserImage { get; set; }

    [JsonProperty("branch_guid")]
    public string? BranchGuid { get; set; }

    [JsonProperty("branch_name")]
    public string? BranchName { get; set; }

    [JsonProperty("report_date")]
    public DateTime ReportDate { get; set; }

    [JsonProperty("total_distance_meters")]
    public decimal TotalDistanceMeters { get; set; }

    [JsonProperty("total_distance_km")]
    public decimal TotalDistanceKm { get; set; }

    [JsonProperty("location_count")]
    public int LocationCount { get; set; }

    [JsonProperty("first_location_time")]
    public DateTime? FirstLocationTime { get; set; }

    [JsonProperty("last_location_time")]
    public DateTime? LastLocationTime { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Kunlik hisobot yaratish uchun request DTO
/// </summary>
public class GenerateDailyReportRequestDto
{
    [JsonProperty("user_id")]
    public int? UserId { get; set; }  // Null bo'lsa barcha userlar uchun

    [JsonProperty("report_date")]
    public DateTime ReportDate { get; set; }
}

/// <summary>
/// Hisobotlarni filter qilish uchun request DTO
/// </summary>
public class DailyDistanceReportFilterDto
{
    /// <summary>
    /// Filial GUID (optional)
    /// </summary>
    [JsonProperty("branch_guid")]
    public string? BranchGuid { get; set; }

    /// <summary>
    /// User ID'lar ro'yxati (optional) - PHP API worker_id
    /// </summary>
    [JsonProperty("user_ids")]
    public List<int>? UserIds { get; set; }

    /// <summary>
    /// Boshlanish sanasi
    /// </summary>
    [JsonProperty("start_date")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Tugash sanasi
    /// </summary>
    [JsonProperty("end_date")]
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Sana oralig'i uchun request DTO
/// </summary>
public class DateRangeRequestDto
{
    [JsonProperty("user_id")]
    public int? UserId { get; set; }  // Null bo'lsa barcha userlar uchun

    [JsonProperty("start_date")]
    public DateTime StartDate { get; set; }

    [JsonProperty("end_date")]
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Kunlik hisobot statistikasi
/// </summary>
public class DailyDistanceStatisticsDto
{
    [JsonProperty("date")]
    public DateTime Date { get; set; }

    [JsonProperty("total_users")]
    public int TotalUsers { get; set; }

    [JsonProperty("total_distance_km")]
    public decimal TotalDistanceKm { get; set; }

    [JsonProperty("average_distance_km")]
    public decimal AverageDistanceKm { get; set; }

    [JsonProperty("max_distance_km")]
    public decimal MaxDistanceKm { get; set; }

    [JsonProperty("min_distance_km")]
    public decimal MinDistanceKm { get; set; }

    [JsonProperty("top_user")]
    public string? TopUserName { get; set; }

    [JsonProperty("top_user_distance_km")]
    public decimal TopUserDistanceKm { get; set; }
}

/// <summary>
/// Foydalanuvchi umumiy statistikasi
/// </summary>
public class UserDistanceSummaryDto
{
    [JsonProperty("user_id")]
    public int? UserId { get; set; }

    [JsonProperty("user_name")]
    public string UserName { get; set; } = string.Empty;

    [JsonProperty("phone")]
    public string? Phone { get; set; }

    [JsonProperty("total_days")]
    public int TotalDays { get; set; }

    [JsonProperty("total_distance_km")]
    public decimal TotalDistanceKm { get; set; }

    [JsonProperty("average_distance_per_day_km")]
    public decimal AverageDistancePerDayKm { get; set; }

    [JsonProperty("max_distance_day")]
    public DateTime? MaxDistanceDay { get; set; }

    [JsonProperty("max_distance_km")]
    public decimal MaxDistanceKm { get; set; }
}
