using System.Text.Json.Serialization;

namespace Convoy.Service.DTOs;

public class PhpWorkerDto
{
    [JsonPropertyName("worker_id")]
    public int WorkerId { get; set; }

    [JsonPropertyName("worker_name")]
    public string WorkerName { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("worker_guid")]
    public string WorkerGuid { get; set; } = string.Empty;

    [JsonPropertyName("branch_guid")]
    public string BranchGuid { get; set; } = string.Empty;

    [JsonPropertyName("branch_name")]
    public string BranchName { get; set; } = string.Empty;

    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("position_id")]
    public int PositionId { get; set; }
}
