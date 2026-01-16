namespace Convoy.Api.Models;

/// <summary>
/// Standart API response wrapper
/// Barcha endpoint'lar uchun bir xil format
/// </summary>
public class ApiResponse<T>
{
    /// <summary>
    /// Request muvaffaqiyatli yoki yo'q
    /// </summary>
    public bool Status { get; set; }

    /// <summary>
    /// Foydalanuvchi uchun xabar
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Response data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Success response yaratish
    /// </summary>
    public static ApiResponse<T> Success(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Status = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// Error response yaratish
    /// </summary>
    public static ApiResponse<T> Error(string message, T? data = default)
    {
        return new ApiResponse<T>
        {
            Status = false,
            Message = message,
            Data = data
        };
    }
}

/// <summary>
/// Data'siz ApiResponse (faqat status va message)
/// </summary>
public class ApiResponse
{
    public bool Status { get; set; }
    public string Message { get; set; } = string.Empty;

    public static ApiResponse Success(string message = "Success")
    {
        return new ApiResponse
        {
            Status = true,
            Message = message
        };
    }

    public static ApiResponse Error(string message)
    {
        return new ApiResponse
        {
            Status = false,
            Message = message
        };
    }
}
