using System.Text.Json.Serialization;

namespace Convoy.Service.DTOs;

public class VerifyOtpResponseDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("expires_in_seconds")]
    public long ExpiresInSeconds { get; set; }
}