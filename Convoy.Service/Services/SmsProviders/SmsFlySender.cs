using Convoy.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Convoy.Service.Services.SmsProviders;


/// <summary>
/// SmsFly.uz orqali SMS yuborish
/// </summary>
public class SmsFlySender : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SmsFlySender> _logger;
    private readonly string _apiKey;
    private readonly string _apiUrl;

    public SmsFlySender(HttpClient httpClient, IConfiguration configuration, ILogger<SmsFlySender> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["SmsProviders:SmsFly:ApiKey"] ?? "9b9ea1f9-6699-11ed-b8e4-0242ac120003";
        _apiUrl = configuration["SmsProviders:SmsFly:ApiUrl"] ?? "https://api.smsfly.uz/send";
    }

    public async Task<bool> SendAsync(string phone, string message)
    {
        try
        {
            // Telefon raqamni formatlash: 941033001 -> 998941033001
            var cleanPhone = phone.TrimStart('+').Trim();

            // Agar 9 raqam bo'lsa (masalan: 941033001), 998 qo'shish
            if (cleanPhone.Length == 9 && cleanPhone.StartsWith("9"))
            {
                cleanPhone = "998" + cleanPhone;
            }
            // Agar 12 raqam va 998 bilan boshlanmasa, 998 qo'shish
            else if (!cleanPhone.StartsWith("998"))
            {
                cleanPhone = "998" + cleanPhone.TrimStart('0');
            }

            _logger.LogInformation("SmsFly: Sending SMS to formatted phone: {Phone}", cleanPhone);

            var payload = new
            {
                key = _apiKey,
                phone = cleanPhone,
                message = $"GARANT: Tasdiqlash uchun maxsus kod: {message}"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(_apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SMS successfully sent via SmsFly to {Phone}", phone);
                return true;
            }

            _logger.LogWarning("SmsFly returned status code {StatusCode} for {Phone}",
                response.StatusCode, phone);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS via SmsFly to {Phone}", phone);
            return false;
        }
    }
}