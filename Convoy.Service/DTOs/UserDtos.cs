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

    [JsonPropertyName("is_active")]
    public bool? IsActive { get; set; }

    [JsonPropertyName("branch_guid")]
    public string? BranchGuid { get; set; }

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

    // User'ning locationlari (array)
    [JsonPropertyName("locations")]
    public List<LocationResponseDto> Locations { get; set; } = new();
}
