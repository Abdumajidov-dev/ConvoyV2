using Convoy.Data.DbContexts;
using Convoy.Domain.Entities;
using Convoy.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace Convoy.Service.Services;

/// <summary>
/// Firebase Cloud Messaging (FCM) orqali push notification yuborish
/// </summary>
public class NotificationService : INotificationService
{
    private readonly AppDbConText _context;
    private readonly ILogger<NotificationService> _logger;
    private readonly IDeviceTokenService _deviceTokenService;

    public NotificationService(
        AppDbConText context,
        ILogger<NotificationService> logger,
        IDeviceTokenService deviceTokenService)
    {
        _context = context;
        _logger = logger;
        _deviceTokenService = deviceTokenService;

        // Initialize Firebase Admin SDK (agar hali initialize qilinmagan bo'lsa)
        if (FirebaseApp.DefaultInstance == null)
        {
            try
            {
                var credential = GoogleCredential.FromFile("firebase-adminsdk.json");
                FirebaseApp.Create(new AppOptions
                {
                    Credential = credential
                });
                _logger.LogInformation("Firebase Admin SDK initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firebase Admin SDK initialization failed");
            }
        }
    }

    public async Task<bool> SendNotificationToAdminAsync(int adminUserId, string title, string message, Dictionary<string, string>? data = null)
    {
        try
        {
            // Admin'ning device token'larini olish
            var deviceTokens = await _deviceTokenService.GetActiveTokensBySupportIdAsync(adminUserId);

            if (deviceTokens == null || deviceTokens.Count == 0)
            {
                _logger.LogWarning("Admin {AdminUserId} uchun aktiv device token topilmadi", adminUserId);
                return false;
            }

            // FCM notification payload
            var notification = new Notification
            {
                Title = title,
                Body = message
            };

            var fcmMessage = new MulticastMessage
            {
                Tokens = deviceTokens,
                Notification = notification,
                Data = data ?? new Dictionary<string, string>(),
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        Sound = "default",
                        ChannelId = "user_offline_alerts"
                    }
                },
                Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        Sound = "default",
                        Badge = 1
                    }
                }
            };

            // FCM orqali yuborish
            var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(fcmMessage);

            _logger.LogInformation("Notification yuborildi: AdminId={AdminUserId}, Success={SuccessCount}, Failed={FailureCount}",
                adminUserId, response.SuccessCount, response.FailureCount);

            return response.SuccessCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin {AdminUserId} ga notification yuborishda xatolik", adminUserId);
            return false;
        }
    }

    public async Task SendNotificationToAllAdminsAsync(string title, string message, Dictionary<string, string>? data = null)
    {
        try
        {
            // Barcha admin_unduruv role'ga ega user'larni olish
            var adminUsers = await _context.Users
                .Where(u => u.Role == "admin_unduruv" && u.IsActive)
                .ToListAsync();

            if (adminUsers.Count == 0)
            {
                _logger.LogWarning("admin_unduruv role'ga ega user topilmadi");
                return;
            }

            _logger.LogInformation("Barcha admin'larga notification yuborilmoqda. Count: {Count}", adminUsers.Count);

            // Har bir admin'ga notification yuborish
            foreach (var admin in adminUsers)
            {
                if (admin.UserId.HasValue)
                {
                    await SendNotificationToAdminAsync(admin.UserId.Value, title, message, data);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Barcha admin'larga notification yuborishda xatolik");
        }
    }

    public async Task SendUserOfflineNotificationAsync(int userId, string userName, int offlineDurationMinutes)
    {
        try
        {
            string title = "⚠️ Hodim aloqaga chiqmayapti";
            string message = $"{userName} {offlineDurationMinutes} daqiqadan beri aloqaga chiqmayapti";

            var data = new Dictionary<string, string>
            {
                { "type", "user_offline" },
                { "user_id", userId.ToString() },
                { "user_name", userName },
                { "offline_duration", offlineDurationMinutes.ToString() },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            };

            await SendNotificationToAllAdminsAsync(title, message, data);

            // Notification log'ini database'ga saqlash
            await SaveNotificationLogAsync(userId, title, message, offlineDurationMinutes);

            _logger.LogInformation("User offline notification yuborildi: UserId={UserId}, Duration={Duration}min",
                userId, offlineDurationMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User offline notification yuborishda xatolik. UserId: {UserId}", userId);
        }
    }

    public async Task SendUserAlertToAdminsAsync(int userId, string userName, string alertMessage, int offlineDurationMinutes)
    {
        try
        {
            string title = "🚨 Hodim harakati tekshiruvi kerak";
            string message = $"{userName}: {alertMessage}";

            var data = new Dictionary<string, string>
            {
                { "type", "user_alert" },
                { "user_id", userId.ToString() },
                { "user_name", userName },
                { "alert_message", alertMessage },
                { "offline_duration", offlineDurationMinutes.ToString() },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            };

            await SendNotificationToAllAdminsAsync(title, message, data);

            _logger.LogInformation("User alert notification yuborildi: UserId={UserId}, Message={Message}",
                userId, alertMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User alert notification yuborishda xatolik. UserId: {UserId}", userId);
        }
    }

    /// <summary>
    /// Notification log'ini database'ga saqlash
    /// </summary>
    private async Task SaveNotificationLogAsync(int userId, string title, string message, int offlineDurationMinutes)
    {
        try
        {
            // Barcha admin'larga yuborilgan notification'larni saqlash
            var adminUsers = await _context.Users
                .Where(u => u.Role == "admin_unduruv" && u.IsActive && u.UserId.HasValue)
                .ToListAsync();

            foreach (var admin in adminUsers)
            {
                var notification = new AdminNotification
                {
                    UserId = userId,
                    AdminUserId = admin.UserId!.Value,
                    NotificationType = "user_offline",
                    Title = title,
                    Message = message,
                    OfflineDurationMinutes = offlineDurationMinutes,
                    IsSent = true,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.AdminNotifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification log saqlashda xatolik");
        }
    }
}
