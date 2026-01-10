using System.Text.Json.Serialization;

namespace Convoy.Service.DTOs;

/// <summary>
/// Barcha auth endpointlar uchun umumiy response struktura
/// </summary>
/// <typeparam name="T">Data turi</typeparam>
public class AuthResponseDto<T>
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    public static AuthResponseDto<T> Success(T data, string message = "Success")
    {
        return new AuthResponseDto<T>
        {
            Status = true,
            Message = message,
            Data = data
        };
    }

    public static AuthResponseDto<T> Failure(string message)
    {
        return new AuthResponseDto<T>
        {
            Status = false,
            Message = message,
            Data = default
        };
    }
}