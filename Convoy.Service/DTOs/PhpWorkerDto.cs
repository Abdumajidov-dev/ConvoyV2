using System.Text.Json.Serialization;

namespace Convoy.Service.DTOs;

/// <summary>
/// PHP API /auth/verify/phone response DTO
/// </summary>
public class PhpWorkerDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("is_active")]
    public int IsActive { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("responsible_worker")]
    public string ResponsibleWorker { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;
}

/// <summary>
/// PHP API /auth/verify-otp response DTO (JWT token bilan)
/// </summary>
public class PhpAuthTokenDto
{
    [JsonPropertyName("allow")]
    public bool Allow { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public int Id { get; set; }
}

/// <summary>
/// PHP API /auth/me response DTO (to'liq user ma'lumotlari)
/// </summary>
public class PhpUserDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("is_active")]
    public int IsActive { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("branch_guid")]
    public string? BranchGuid { get; set; }

    [JsonPropertyName("worker_guid")]
    public string? WorkerGuid { get; set; }

    [JsonPropertyName("position_id")]
    public int? PositionId { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}

/// <summary>
/// PHP API generic response wrapper
/// Success: { "status": true, "result": {...}, "message": "..." (optional) }
/// Error: { "status": false, "error": { "message": "..." } }
/// </summary>
public class PhpApiResponse<T>
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("result")]
    public T? Result { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("error")]
    public PhpApiError? Error { get; set; }

    /// <summary>
    /// Error yoki message'dan message olish (priority: error.message > message)
    /// </summary>
    public string GetMessage()
    {
        if (Error != null && !string.IsNullOrEmpty(Error.Message))
            return Error.Message;

        return Message ?? string.Empty;
    }
}

/// <summary>
/// PHP API error object
/// </summary>
public class PhpApiError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}