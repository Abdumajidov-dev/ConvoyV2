namespace Convoy.Api.Middleware;

/// <summary>
/// Device token middleware extension - Program.cs'da middleware'ni qo'shish uchun
/// </summary>
public static class DeviceTokenMiddlewareExtensions
{
    /// <summary>
    /// Device token middleware'ni qo'shish
    /// </summary>
    public static IApplicationBuilder UseDeviceToken(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DeviceTokenMiddleware>();
    }
}
