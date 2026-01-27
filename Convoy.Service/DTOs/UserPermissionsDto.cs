using System.Text.Json.Serialization;

namespace Convoy.Service.DTOs;

/// <summary>
/// /api/auth/me endpoint uchun response DTO
/// PHP API'dan kelgan user ma'lumotlari
/// Permissions PHP API'da boshqariladi (biz faqat proxy qilamiz)
/// </summary>
public class UserPermissionsDto
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;
    [JsonPropertyName("branch_guid")]
    public string BranchGuid { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("role_id")]
    public List<long> RoleId { get; set; } = new();

    [JsonPropertyName("permissions")]
    public Dictionary<string, List<string>> Permissions { get; set; } = new();
}
