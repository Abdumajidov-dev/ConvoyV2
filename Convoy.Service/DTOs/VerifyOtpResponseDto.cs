using System.Text.Json.Serialization;

namespace Convoy.Service.DTOs;

/// <summary>
/// OTP verify qilgandan keyin qaytariladigan response
/// PHP API'dan kelgan JWT token bilan
/// </summary>
public class VerifyOtpResponseDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public string? ExpiresAt { get; set; }
}