namespace Convoy.Api.Middleware;

/// <summary>
/// Flutter'dan kelayotgan "token" headerini ushlash va log qilish uchun middleware
/// </summary>
public class TokenHeaderLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenHeaderLoggingMiddleware> _logger;

    public TokenHeaderLoggingMiddleware(RequestDelegate next, ILogger<TokenHeaderLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // "token" headerini tekshirish.
        // DIQQAT: token qiymati HECH QACHON to'liq log qilinmasin - loglar orqali sizib chiqadi.
        if (context.Request.Headers.TryGetValue("token", out var tokenValue))
        {
            _logger.LogDebug(
                "Flutter 'token' header keldi. Path={Path}, Method={Method}, Token={MaskedToken}, UserAgent={UserAgent}",
                context.Request.Path,
                context.Request.Method,
                Mask(tokenValue.ToString()),
                context.Request.Headers["User-Agent"].ToString());
        }

        // Keyingi middleware'ga o'tkazish
        await _next(context);
    }

    /// <summary>
    /// Token qiymatini log uchun xavfsiz holga keltiradi: faqat oxirgi 4 belgi ko'rinadi
    /// </summary>
    private static string Mask(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "(bo'sh)";

        return value.Length <= 4
            ? "***"
            : $"***{value[^4..]} (uzunlik: {value.Length})";
    }
}

/// <summary>
/// Extension method for easy middleware registration
/// </summary>
public static class TokenHeaderLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenHeaderLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TokenHeaderLoggingMiddleware>();
    }
}
