namespace Convoy.Service.Interfaces;

/// <summary>
/// Telegram bot service interface
/// Telegram kanalga xabar yuborish uchun
/// </summary>
public interface ITelegramService
{
    /// <summary>
    /// Telegram kanalga oddiy text xabar yuborish
    /// </summary>
    Task<bool> SendMessageAsync(string message);

    /// <summary>
    /// Telegram kanalga formatted xabar yuborish (HTML/Markdown)
    /// </summary>
    Task<bool> SendFormattedMessageAsync(string message, string parseMode = "HTML");

    /// <summary>
    /// Location ma'lumotlarini Telegram kanalga yuborish
    /// </summary>
    Task<bool> SendLocationDataAsync(int userId, string userName, double latitude, double longitude, DateTime recordedAt);

    /// <summary>
    /// Bulk location ma'lumotlarini Telegram kanalga yuborish
    /// </summary>
    Task<bool> SendBulkLocationDataAsync(int userId, string userName, int locationCount, DateTime firstLocation, DateTime lastLocation);

    /// <summary>
    /// Custom formatted xabar yuborish (key-value pairs)
    /// </summary>
    Task<bool> SendDataAsync(Dictionary<string, string> data, string title = "📊 Data Report");

    /// <summary>
    /// Alert/Warning xabar yuborish
    /// </summary>
    Task<bool> SendAlertAsync(string alertMessage, string level = "INFO");
}
