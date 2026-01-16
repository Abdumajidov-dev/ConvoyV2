using System.Text.Json.Serialization;

namespace Convoy.Service.DTOs;

/// <summary>
/// PHP API dan keluvchi filial ma'lumotlari
/// </summary>
public class BranchDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("state_id")]
    public int? StateId { get; set; }

    [JsonPropertyName("state_name")]
    public string? StateName { get; set; }

    [JsonPropertyName("region_id")]
    public int? RegionId { get; set; }

    [JsonPropertyName("region_name")]
    public string? RegionName { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("target")]
    public string? Target { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("responsible_worker")]
    public string? ResponsibleWorker { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }
    [JsonPropertyName("branch_guid")]
    public string BranchGuid { get; set; }
}

/// <summary>
/// PHP API response wrapper
/// </summary>
public class PhpBranchResponse
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("data")]
    public List<BranchDto> Data { get; set; } = new List<BranchDto>();
}

/// <summary>
/// Branch search request
/// </summary>
public class BranchSearchRequest
{
    [JsonPropertyName("search")]
    public string? Search { get; set; }
}
