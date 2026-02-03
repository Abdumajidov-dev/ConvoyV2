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
                _logger.LogInformation("🔍 Attempting Firebase Admin SDK initialization...");

                // PRIORITY 1: Environment variable orqali Base64 encoded credentials
                var base64Credentials = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_BASE64");
                _logger.LogInformation("Checking FIREBASE_CREDENTIALS_BASE64: {Found}", !string.IsNullOrWhiteSpace(base64Credentials));

                if (!string.IsNullOrWhiteSpace(base64Credentials))
                {
                    _logger.LogInformation("Loading Firebase credentials from FIREBASE_CREDENTIALS_BASE64 environment variable");

                    // Base64'dan decode qilish
                    var jsonBytes = Convert.FromBase64String(base64Credentials);

                    // JSON string'dan credential yaratish
                    using var stream = new MemoryStream(jsonBytes);
                    var credential = GoogleCredential.FromStream(stream);

                    FirebaseApp.Create(new AppOptions { Credential = credential });
                    _logger.LogInformation("✅ Firebase Admin SDK initialized from Base64 environment variable");
                    return;
                }

                // PRIORITY 2: File path orqali (local development)
                var credentialsPath = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_PATH") ?? "firebase-adminsdk.json";
                var fileExists = File.Exists(credentialsPath);
                _logger.LogInformation("Checking file: {Path}, Exists: {Exists}", credentialsPath, fileExists);

                if (fileExists)
                {
                    _logger.LogInformation("Loading Firebase credentials from file: {Path}", credentialsPath);

                    // Stream orqali o'qish (FromFile o'rniga)
                    using var fileStream = File.OpenRead(credentialsPath);
                    var credential = GoogleCredential.FromStream(fileStream);

                    FirebaseApp.Create(new AppOptions { Credential = credential });
                    _logger.LogInformation("✅ Firebase Admin SDK initialized from file: {Path}", credentialsPath);
                    return;
                }

                // PRIORITY 3: Credentials topilmadi - notification disabled
                _logger.LogWarning("⚠️ Firebase credentials not found. Checked:");
                _logger.LogWarning("  1. FIREBASE_CREDENTIALS_BASE64 environment variable: {Found}", !string.IsNullOrWhiteSpace(base64Credentials) ? "SET (but empty/whitespace)" : "NOT SET");
                _logger.LogWarning("  2. File path: {Path} - {Status}", credentialsPath, fileExists ? "EXISTS (but failed to load)" : "NOT FOUND");
                _logger.LogWarning("Firebase notifications are DISABLED. API will continue without push notifications.");
                _logger.LogWarning("See FIREBASE_DEPLOYMENT.md for deployment instructions.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Firebase Admin SDK initialization failed");
                _logger.LogWarning("Firebase notifications are DISABLED due to initialization error.");
            }
        }
        else
        {
            _logger.LogInformation("Firebase Admin SDK already initialized");
        }
    }

    public async Task<bool> SendNotificationToAdminAsync(int adminUserId, string title, string message, Dictionary<string, string>? data = null)
    {
        try
        {
            // Firebase initialized emasligini tekshirish
            if (FirebaseApp.DefaultInstance == null)
            {
                _logger.LogWarning("⚠️ Firebase Admin SDK initialized emas. Notification yuborilmaydi.");
                _logger.LogWarning("To enable notifications, set FIREBASE_CREDENTIALS_BASE64 environment variable");
                _logger.LogWarning("See FIREBASE_DEPLOYMENT.md for complete instructions");
                return false;
            }

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
            //

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
