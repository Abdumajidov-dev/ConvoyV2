using System.Text.Json.Serialization;

namespace Convoy.Service.DTOs;

/// <summary>
/// User yaratish uchun DTO
/// </summary>
public class CreateUserDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// User yangilash uchun DTO
/// </summary>
public class UpdateUserDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("is_active")]
    public bool? IsActive { get; set; }
}

/// <summary>
/// User response DTO
/// </summary>
public class UserResponseDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    //[JsonPropertyName("username")]
    //public string Username { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("branch_guid")]
    public string? BranchGuid { get; set; }

    [JsonPropertyName("branch")]
    public BranchDto? Branch { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Oxirgi location (null bo'lishi mumkin agar user hali location yubormagan bo'lsa)
    [JsonPropertyName("latest_location")]
    public LocationResponseDto? LatestLocation { get; set; }
}

/// <summary>
/// User list query parametrlari
/// </summary>
public class UserQueryDto
{
    [JsonPropertyName("search_term")]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// User aktiv yoki emas: "true" = aktiv, "false" = noaktiv, null = barcha
    /// </summary>
    [JsonPropertyName("is_active")]
    public string? IsActive { get; set; }

    [JsonPropertyName("branch_guid")]
    public string? BranchGuid { get; set; }

    /// <summary>
    /// User to'xtab turgan yoki yo'q: "true" = to'xtaganlar, "false" = harakat qilayotganlar, null = barcha
    /// </summary>
    [JsonPropertyName("is_stopped")]
    public string? IsStopped { get; set; }

    /// <summary>
    /// Qaysi sana bo'yicha tekshirish (default: bugun)
    /// Format: "2026-02-11" yoki "2026-02-11 15:30:00"
    /// </summary>
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    /// <summary>
    /// Soat oralig'i boshlanishi (masalan: "09:00")
    /// Null bo'lsa soatga qaramaydi
    /// </summary>
    [JsonPropertyName("start_hour")]
    public string? StartHour { get; set; }

    /// <summary>
    /// Soat oralig'i tugashi (masalan: "18:00")
    /// Null bo'lsa soatga qaramaydi
    /// </summary>
    [JsonPropertyName("end_hour")]
    public string? EndHour { get; set; }

    /// <summary>
    /// Kamida necha daqiqa to'xtab turgan (default: 60 daqiqa = 1 soat)
    /// Null bo'lsa majburiy emas
    /// </summary>
    [JsonPropertyName("min_stopped_minutes")]
    public int? MinStoppedMinutes { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Pagination response
/// </summary>
public class PaginatedResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    [JsonPropertyName("has_next_page")]
    public bool HasNextPage => Page < TotalPages;

    [JsonPropertyName("has_previous_page")]
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// User ma'lumotlari va uning locationlari (multiple users endpoint uchun)
/// </summary>
public class UserWithLocationsDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("branch_guid")]
    public string? BranchGuid { get; set; }

    [JsonPropertyName("branch")]
    public BranchDto? Branch { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // User'ning locationlari (clustering qilingan, stopped_time bilan)
    [JsonPropertyName("locations")]
    public List<LocationResponseDto> Locations { get; set; } = new();
}

/// <summary>
/// User statistikasi response DTO
/// </summary>
public class UserStatisticsDto
{
    /// <summary>
    /// Jami userlar soni
    /// </summary>
    [JsonPropertyName("total_users")]
    public int TotalUsers { get; set; }

    /// <summary>
    /// Active (is_active = true) userlar soni
    /// </summary>
    [JsonPropertyName("active_users")]
    public int ActiveUsers { get; set; }

    /// <summary>
    /// Inactive (is_active = false) userlar soni
    /// </summary>
    [JsonPropertyName("inactive_users")]
    public int InactiveUsers { get; set; }
}
