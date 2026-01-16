namespace Convoy.Service.Common;

/// <summary>
/// Service layer uchun standart result wrapper
/// Controller'da HTTP status code'ga convert qilinadi
/// </summary>
public class ServiceResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public int StatusCode { get; set; } = 200;

    /// <summary>
    /// Success result yaratish
    /// </summary>
    public static ServiceResult<T> Ok(T data, string message = "Success", int statusCode = 200)
    {
        return new ServiceResult<T>
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Created result (200 OK - same as Ok for consistency)
    /// </summary>
    public static ServiceResult<T> Created(T data, string message = "Created")
    {
        return new ServiceResult<T>
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = 200
        };
    }

    /// <summary>
    /// Error result yaratish
    /// </summary>
    public static ServiceResult<T> Fail(string message, int statusCode = 400, T? data = default)
    {
        return new ServiceResult<T>
        {
            Success = false,
            Message = message,
            Data = data,
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Not Found (404)
    /// </summary>
    public static ServiceResult<T> NotFound(string message = "Not found")
    {
        return new ServiceResult<T>
        {
            Success = false,
            Message = message,
            Data = default,
            StatusCode = 404
        };
    }

    /// <summary>
    /// Bad Request (400)
    /// </summary>
    public static ServiceResult<T> BadRequest(string message)
    {
        return new ServiceResult<T>
        {
            Success = false,
            Message = message,
            Data = default,
            StatusCode = 400
        };
    }

    /// <summary>
    /// Unauthorized (401)
    /// </summary>
    public static ServiceResult<T> Unauthorized(string message = "Unauthorized")
    {
        return new ServiceResult<T>
        {
            Success = false,
            Message = message,
            Data = default,
            StatusCode = 401
        };
    }

    /// <summary>
    /// Internal Server Error (500)
    /// </summary>
    public static ServiceResult<T> ServerError(string message = "Internal server error")
    {
        return new ServiceResult<T>
        {
            Success = false,
            Message = message,
            Data = default,
            StatusCode = 500
        };
    }
}

/// <summary>
/// Data'siz ServiceResult
/// </summary>
public class ServiceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 200;

    public static ServiceResult Ok(string message = "Success")
    {
        return new ServiceResult
        {
            Success = true,
            Message = message,
            StatusCode = 200
        };
    }

    public static ServiceResult Fail(string message, int statusCode = 400)
    {
        return new ServiceResult
        {
            Success = false,
            Message = message,
            StatusCode = statusCode
        };
    }
}
