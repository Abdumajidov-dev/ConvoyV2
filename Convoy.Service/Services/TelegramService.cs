using Convoy.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Convoy.Service.Services;

/// <summary>
/// Telegram bot service implementation
/// Telegram kanalga xabar yuborish
/// </summary>
public class TelegramService : ITelegramService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramService> _logger;
    private readonly string _botToken;
    private readonly string _channelId;
    private readonly bool _isEnabled;

    public TelegramService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TelegramService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Configuration'dan bot settings'ni o'qish
        _botToken = configuration["BotSettings:Telegram:BotToken"] ?? "";
        _channelId = configuration["BotSettings:Telegram:ChannelId"] ?? "";
        _isEnabled = !string.IsNullOrEmpty(_botToken) && !string.IsNullOrEmpty(_channelId);

        if (!_isEnabled)
        {
            _logger.LogWarning("⚠️ Telegram bot settings not configured. Service disabled.");
        }
        else
        {
            _logger.LogInformation("✅ Telegram bot service initialized. Channel: {ChannelId}", _channelId);
        }
    }

    public async Task<bool> SendMessageAsync(string message)
    {
        if (!_isEnabled)
        {
            _logger.LogWarning("Telegram service is disabled. Skipping message send.");
            return false;
        }

        try
        {
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

            var payload = new
            {
                chat_id = _channelId,
                text = message,
                parse_mode = "HTML"
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Telegram message sent successfully");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ Telegram API error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending Telegram message");
            return false;
        }
    }

    public async Task<bool> SendFormattedMessageAsync(string message, string parseMode = "HTML")
    {
        if (!_isEnabled)
        {
            _logger.LogWarning("Telegram service is disabled. Skipping message send.");
            return false;
        }

        try
        {
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

            var payload = new
            {
                chat_id = _channelId,
                text = message,
                parse_mode = parseMode
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Telegram formatted message sent successfully");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ Telegram API error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending Telegram formatted message");
            return false;
        }
    }

    public async Task<bool> SendLocationDataAsync(
        int userId,
        string userName,
        double latitude,
        double longitude,
        DateTime recordedAt)
    {
        var message = $@"📍 <b>Yangi Lokatsiya</b>

👤 <b>User:</b> {userName} (ID: {userId})
🌍 <b>Koordinatalar:</b>
   • Latitude: {latitude:F6}
   • Longitude: {longitude:F6}
⏰ <b>Vaqt:</b> {recordedAt:dd.MM.yyyy HH:mm:ss}

🗺 <a href='https://www.google.com/maps?q={latitude},{longitude}'>Google Maps'da ko'rish</a>";

        return await SendFormattedMessageAsync(message);
    }

    public async Task<bool> SendBulkLocationDataAsync(
        int userId,
        string userName,
        int locationCount,
        DateTime firstLocation,
        DateTime lastLocation)
    {
        var duration = lastLocation - firstLocation;
        var durationText = duration.TotalHours >= 1
            ? $"{duration.TotalHours:F1} soat"
            : $"{duration.TotalMinutes:F0} daqiqa";

        var message = $@"📊 <b>Bulk Lokatsiya Ma'lumoti</b>

👤 <b>User:</b> {userName} (ID: {userId})
📍 <b>Lokatsiyalar soni:</b> {locationCount}
⏰ <b>Davomiyligi:</b> {durationText}
🕐 <b>Birinchi:</b> {firstLocation:dd.MM.yyyy HH:mm:ss}
🕑 <b>Oxirgi:</b> {lastLocation:dd.MM.yyyy HH:mm:ss}

✅ <b>Ma'lumotlar muvaffaqiyatli saqlandi</b>";

        return await SendFormattedMessageAsync(message);
    }

    public async Task<bool> SendDataAsync(Dictionary<string, string> data, string title = "📊 Data Report")
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<b>{title}</b>");
        sb.AppendLine();

        foreach (var kvp in data)
        {
            sb.AppendLine($"• <b>{kvp.Key}:</b> {kvp.Value}");
        }

        sb.AppendLine();
        sb.AppendLine($"🕐 <i>{DateTime.Now:dd.MM.yyyy HH:mm:ss}</i>");

        return await SendFormattedMessageAsync(sb.ToString());
    }

    public async Task<bool> SendAlertAsync(string alertMessage, string level = "INFO")
    {
        var emoji = level.ToUpper() switch
        {
            "ERROR" => "🔴",
            "WARNING" => "⚠️",
            "SUCCESS" => "✅",
            "INFO" => "ℹ️",
            _ => "📢"
        };

        var message = $@"{emoji} <b>{level}</b>

{alertMessage}

🕐 {DateTime.Now:dd.MM.yyyy HH:mm:ss}";

        return await SendFormattedMessageAsync(message);
    }
}
