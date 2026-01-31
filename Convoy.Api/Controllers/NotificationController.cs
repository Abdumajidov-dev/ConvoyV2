using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Convoy.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IDeviceTokenService _deviceTokenService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService,
        IDeviceTokenService deviceTokenService,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _deviceTokenService = deviceTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Bitta userga push notification yuborish
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
        try
        {
            if (request == null || request.UserId <= 0 || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "UserId va Message majburiy",
                    data = (object?)null
                });
            }

            _logger.LogInformation("Notification yuborilmoqda. UserId: {UserId}, Message: {Message}",
                request.UserId, request.Message);

            // SendNotificationToAdminAsync notification yuboradi
            var title = request.Title ?? "Yangi xabar";
            var success = await _notificationService.SendNotificationToAdminAsync(
                request.UserId,
                title,
                request.Message,
                request.Data
            );

            if (success)
            {
                _logger.LogInformation("Notification muvaffaqiyatli yuborildi. UserId: {UserId}", request.UserId);
                return Ok(new
                {
                    status = true,
                    message = "Notification muvaffaqiyatli yuborildi",
                    data = new
                    {
                        user_id = request.UserId,
                        title = title,
                        message = request.Message
                    }
                });
            }
            else
            {
                _logger.LogWarning("Notification yuborilmadi. UserId: {UserId}", request.UserId);
                return BadRequest(new
                {
                    status = false,
                    message = "Notification yuborilmadi. Device token topilmadi yoki invalid.",
                    data = new
                    {
                        user_id = request.UserId,
                        title = title,
                        message = request.Message
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendNotification xatolik. UserId: {UserId}", request?.UserId);
            return StatusCode(500, new
            {
                status = false,
                message = "Notification yuborishda xatolik yuz berdi",
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// Ko'p userlarga push notification yuborish
    /// </summary>
    [HttpPost("send-multiple")]
    public async Task<IActionResult> SendMultipleNotifications([FromBody] SendMultipleNotificationsRequest request)
    {
        try
        {
            if (request == null || request.UserIds == null || !request.UserIds.Any() || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "UserIds va Message majburiy",
                    data = (object?)null
                });
            }

            _logger.LogInformation("Multiple notification yuborilmoqda. Users: {Count}, Message: {Message}",
                request.UserIds.Count, request.Message);

            var results = new List<object>();
            int totalSuccess = 0;
            int totalFailed = 0;

            foreach (var userId in request.UserIds)
            {
                var tokens = await _deviceTokenService.GetActiveTokensBySupportIdAsync(userId);

                if (tokens == null || !tokens.Any())
                {
                    _logger.LogWarning("User device tokenlari topilmadi. UserId: {UserId}", userId);
                    results.Add(new
                    {
                        user_id = userId,
                        success = false,
                        sent_count = 0,
                        message = "Device token topilmadi"
                    });
                    continue;
                }

                int userSuccess = 0;
                foreach (var token in tokens)
                {
                    try
                    {
                        var title = request.Title ?? "Yangi xabar";
                        var success = await _notificationService.SendNotificationToAdminAsync(
                            userId,
                            title,
                            request.Message,
                            request.Data
                        );

                        if (success)
                        {
                            userSuccess++;
                            totalSuccess++;
                        }
                        else
                        {
                            totalFailed++;
                            await _deviceTokenService.DeactivateTokenAsync(token);
                        }
                    }
                    catch (Exception ex)
                    {
                        totalFailed++;
                        _logger.LogError(ex, "Notification yuborishda xatolik. UserId: {UserId}", userId);
                    }
                }

                results.Add(new
                {
                    user_id = userId,
                    success = userSuccess > 0,
                    sent_count = userSuccess,
                    tokens_count = tokens.Count
                });
            }

            return Ok(new
            {
                status = totalSuccess > 0,
                message = $"Notification {totalSuccess} ta device'ga yuborildi, {totalFailed} ta failed",
                data = new
                {
                    users_count = request.UserIds.Count,
                    total_sent = totalSuccess,
                    total_failed = totalFailed,
                    results = results
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendMultipleNotifications xatolik");
            return StatusCode(500, new
            {
                status = false,
                message = "Notification yuborishda xatolik yuz berdi",
                data = (object?)null
            });
        }
    }
}

/// <summary>
/// Bitta userga notification yuborish request
/// </summary>
public class SendNotificationRequest
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public Dictionary<string, string>? Data { get; set; }
}

/// <summary>
/// Ko'p userlarga notification yuborish request
/// </summary>
public class SendMultipleNotificationsRequest
{
    [JsonPropertyName("user_ids")]
    public List<int> UserIds { get; set; } = new();

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public Dictionary<string, string>? Data { get; set; }
}
