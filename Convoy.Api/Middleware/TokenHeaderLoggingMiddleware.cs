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
        // "token" headerini tekshirish
        Console.WriteLine(context.Request.Headers);

        if (context.Request.Headers.TryGetValue("token", out var tokenValue))
        {
            _logger.LogWarning("========================================");
            _logger.LogWarning("🔍 FLUTTER TOKEN HEADER DETECTED!");
            _logger.LogWarning("Path: {Path}", context.Request.Path);
            _logger.LogWarning("Method: {Method}", context.Request.Method);
            _logger.LogWarning("Token Value: {TokenValue}", tokenValue.ToString());
            _logger.LogWarning("Content-Type: {ContentType}", context.Request.ContentType);
            _logger.LogWarning("User-Agent: {UserAgent}", context.Request.Headers["User-Agent"].ToString());
            _logger.LogWarning("========================================");

            // Agar boshqa headerlar ham kerak bo'lsa
            _logger.LogInformation("All Headers:");
            foreach (var header in context.Request.Headers)
            {
                _logger.LogInformation("  {Key}: {Value}", header.Key, header.Value);
            }
        }

        // Keyingi middleware'ga o'tkazish
        await _next(context);
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
