namespace Convoy.Service.Interfaces;

/// <summary>
/// Push notification yuborish uchun service interface
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Bir admin'ga notification yuborish
    /// </summary>
    Task<bool> SendNotificationToAdminAsync(int adminUserId, string title, string message, Dictionary<string, string>? data = null);

    /// <summary>
    /// Barcha admin'larga notification yuborish
    /// </summary>
    Task SendNotificationToAllAdminsAsync(string title, string message, Dictionary<string, string>? data = null);

    /// <summary>
    /// User offline ekanligini admin'larga bildirish
    /// </summary>
    Task SendUserOfflineNotificationAsync(int userId, string userName, int offlineDurationMinutes);

    /// <summary>
    /// User haqida admin'larga xabar yuborish
    /// </summary>
    Task SendUserAlertToAdminsAsync(int userId, string userName, string alertMessage, int offlineDurationMinutes);
}
