namespace Convoy.Service.DTOs;

/// <summary>
/// User yaratish uchun DTO
/// </summary>
public class CreateUserDto
{
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// User yangilash uchun DTO
/// </summary>
public class UpdateUserDto
{
    public string? Name { get; set; }
    public string? Username { get; set; }
    public string? Phone { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// User response DTO
/// </summary>
public class UserResponseDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    //public string Username { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// User list query parametrlari
/// </summary>
public class UserQueryDto
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Pagination response
/// </summary>
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
