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
/// PHP API /auth/verify-otp response DTO (JWT token + user data bilan)
/// </summary>
public class PhpAuthTokenDto
{
    [JsonPropertyName("allow")]
    public bool Allow { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// PHP API'dan kelgan user ma'lumotlari (optional)
    /// Agar verify_otp dan user data qaytarsa, buni sync qilamiz
    /// </summary>
    [JsonPropertyName("user")]
    public PhpUserDto? User { get; set; }
}

/// <summary>
/// PHP API /auth/me response DTO va JWT token payload DTO
/// Token ichidagi ma'lumotlar bilan to'liq mos keladi
/// </summary>
public class PhpUserDto
{
    /// <summary>
    /// PHP API user ID (auth table'dagi ID)
    /// </summary>
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    /// <summary>
    /// Worker ID (workers table'dagi ID) - BU ASOSIY ID!
    /// Local DB'da user_id sifatida saqlanadi
    /// </summary>
    [JsonPropertyName("worker_id")]
    public int WorkerId { get; set; }

    [JsonPropertyName("worker_guid")]
    public string? WorkerGuid { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("filial_guid")]
    public string? FilialGuid { get; set; }

    [JsonPropertyName("filial_name")]
    public string? FilialName { get; set; }

    [JsonPropertyName("photo")]
    public string? Photo { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("login_date")]
    public string? LoginDate { get; set; }

    [JsonPropertyName("position_id")]
    public int? PositionId { get; set; }

    /// <summary>
    /// PHP API /auth/unduruv/me endpoint'dan kelgan app ma'lumotlari
    /// </summary>
    [JsonPropertyName("app")]
    public PhpAppDto? App { get; set; }

    // Backward compatibility (eski API endpoint'lar uchun)
    [JsonPropertyName("id")]
    public int Id
    {
        get => WorkerId;
        set => WorkerId = value;
    }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("is_active")]
    public int IsActive { get; set; } = 1;

    [JsonPropertyName("image")]
    public string? Image
    {
        get => Photo;
        set => Photo = value;
    }

    [JsonPropertyName("branch_guid")]
    public string? BranchGuid
    {
        get => FilialGuid;
        set => FilialGuid = value;
    }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}

/// <summary>
/// PHP API /auth/unduruv/me response'dagi app obyekti
/// </summary>
public class PhpAppDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("base_url")]
    public string BaseUrl { get; set; } = string.Empty;

    [JsonPropertyName("which_project")]
    public string WhichProject { get; set; } = string.Empty;

    [JsonPropertyName("allowed")]
    public PhpAppAllowedDto? Allowed { get; set; }
}

/// <summary>
/// PHP API /auth/unduruv/me response'dagi app.allowed obyekti
/// </summary>
public class PhpAppAllowedDto
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("which_project")]
    public string WhichProject { get; set; } = string.Empty;

    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = new();
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