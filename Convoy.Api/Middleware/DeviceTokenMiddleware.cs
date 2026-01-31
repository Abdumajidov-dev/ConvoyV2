using Newtonsoft.Json;
using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;

namespace Convoy.Api.Middleware;

/// <summary>
/// Device token middleware - har bir request'da device-info header'dan device token'ni avtomatik saqlaydi
/// </summary>
public class DeviceTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DeviceTokenMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public DeviceTokenMiddleware(RequestDelegate next, ILogger<DeviceTokenMiddleware> logger, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context, IPhpTokenService phpTokenService)
    {
        try
        {
            // DEBUG: Barcha headerlarni log qilish
            _logger.LogInformation("=== DeviceTokenMiddleware START ===");
            _logger.LogInformation("Path: {Path}", context.Request.Path);
            _logger.LogInformation("Headers count: {Count}", context.Request.Headers.Count);

            foreach (var header in context.Request.Headers)
            {
                // Password'larni log qilmaslik uchun
                var value = header.Key.ToLower().Contains("auth") || header.Key.ToLower().Contains("token")
                    ? "[REDACTED]"
                    : header.Value.ToString();
                _logger.LogInformation("Header: {Key} = {Value}", header.Key, value);
            }

            // Authorization header'dan token olish
            var authHeader = context.Request.Headers["Authorization"].ToString();
            _logger.LogInformation("Authorization header mavjud: {HasAuth}", !string.IsNullOrEmpty(authHeader));

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();

                // PHP token'dan worker_id (user_id) ni olish
                var userId = phpTokenService.GetWorkerIdFromToken(token);
                _logger.LogInformation("UserId from token: {UserId}", userId);

                if (userId.HasValue)
                {
                    // Header'dan device-info olish
                    var hasDeviceInfo = context.Request.Headers.TryGetValue("device-info", out var deviceInfoHeader);
                    _logger.LogInformation("device-info header mavjud: {HasDeviceInfo}", hasDeviceInfo);

                    if (hasDeviceInfo)
                    {
                        var deviceInfoJson = deviceInfoHeader.ToString();
                        _logger.LogInformation("device-info value length: {Length}", deviceInfoJson?.Length ?? 0);
                        _logger.LogInformation("device-info value: {Value}", deviceInfoJson);

                        if (!string.IsNullOrEmpty(deviceInfoJson))
                        {
                            _logger.LogInformation("Starting background task for device token save...");

                            // Background task'da process qilish (main request'ni bloklamaslik uchun)
                            // Yangi scope yaratish DbContext thread-safety uchun
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    using var scope = _scopeFactory.CreateScope();
                                    var deviceTokenService = scope.ServiceProvider.GetRequiredService<IDeviceTokenService>();
                                    await ProcessDeviceInfoAsync(deviceInfoJson, userId.Value, deviceTokenService);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Background device token saqlashda xatolik. UserId: {UserId}", userId.Value);
                                }
                            });
                        }
                        else
                        {
                            _logger.LogWarning("device-info header bo'sh!");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("device-info header topilmadi!");
                    }
                }
                else
                {
                    _logger.LogWarning("Token'dan UserId olinmadi!");
                }
            }
            else
            {
                _logger.LogWarning("Authorization header topilmadi yoki noto'g'ri format!");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeviceTokenMiddleware'da xatolik");
        }

        await _next(context);
    }

    private async Task ProcessDeviceInfoAsync(string deviceInfoJson, int userId, IDeviceTokenService deviceTokenService)
    {
        try
        {
            _logger.LogInformation("Device info processlanmoqda. UserId: {UserId}, JSON: {Json}", userId, deviceInfoJson);

            DeviceInfoDto? deviceInfo = null;

            // Flutter'dan kelayotgan format object ko'rinishida: {"device_system":"android",...}
            try
            {
                // Avval direct object sifatida parse qilishga harakat qilamiz
                deviceInfo = JsonConvert.DeserializeObject<DeviceInfoDto>(deviceInfoJson);
            }
            catch (JsonException)
            {
                // Agar object sifatida parse qilinmasa, array formatni sinab ko'ramiz
                try
                {
                    var deviceInfoArray = JsonConvert.DeserializeObject<List<DeviceInfoDto>>(deviceInfoJson);
                    if (deviceInfoArray != null && deviceInfoArray.Any())
                    {
                        deviceInfo = deviceInfoArray.First();
                    }
                }
                catch (JsonException arrayEx)
                {
                    _logger.LogError(arrayEx, "Array format ham parse qilinmadi. JSON: {Json}", deviceInfoJson);
                    return;
                }
            }

            if (deviceInfo != null)
            {
                _logger.LogInformation("Device info parse qilindi. UserId: {UserId}, DeviceSystem: {DeviceSystem}, Model: {Model}",
                    userId, deviceInfo.DeviceSystem, deviceInfo.Model);

                // Device token mavjud bo'lsa saqlash
                if (!string.IsNullOrEmpty(deviceInfo.DeviceToken))
                {
                    var success = await deviceTokenService.SaveOrUpdateDeviceTokenAsync(userId, deviceInfo);

                    if (success)
                    {
                        _logger.LogInformation("Device token muvaffaqiyatli saqlandi. UserId: {UserId}, DeviceSystem: {DeviceSystem}",
                            userId, deviceInfo.DeviceSystem);
                    }
                    else
                    {
                        _logger.LogWarning("Device token saqlanmadi. UserId: {UserId}", userId);
                    }
                }
                else
                {
                    _logger.LogWarning("Device token bo'sh. UserId: {UserId}", userId);
                }
            }
            else
            {
                _logger.LogWarning("Device info parse qilinmadi. UserId: {UserId}, JSON: {Json}", userId, deviceInfoJson);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Device info processda umumiy xatolik. UserId: {UserId}, JSON: {Json}", userId, deviceInfoJson);
        }
    }
}
