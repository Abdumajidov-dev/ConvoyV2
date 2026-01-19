using System.Text.Json.Serialization;

namespace Convoy.Service.DTOs;

/// <summary>
/// /api/auth/me endpoint uchun response DTO
/// Flutter uchun qulay formatda permissions grouped by resource
/// </summary>
public class UserPermissionsDto
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("role_id")]
    public List<long> RoleId { get; set; } = new();

    [JsonPropertyName("permissions")]
    public Dictionary<string, List<string>> Permissions { get; set; } = new();
}
