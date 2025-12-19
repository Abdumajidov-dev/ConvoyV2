using Convoy.Service.Interfaces;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services.SmsProviders;

/// <summary>
/// Ikki SMS provayderdan foydalanuvchi composite service
/// Birinchi provider ishlamasa, ikkinchisiga o'tadi
/// </summary>
public class CompositeSmsService : ISmsService
{
    private readonly SmsFlySender _smsFlySender;
    private readonly SayqalSender _sayqalSender;
    private readonly ILogger<CompositeSmsService> _logger;

    public CompositeSmsService(
        SmsFlySender smsFlySender,
        SayqalSender sayqalSender,
        ILogger<CompositeSmsService> logger)
    {
        _smsFlySender = smsFlySender;
        _sayqalSender = sayqalSender;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string phone, string message)
    {
        // Birinchi SmsFly orqali harakat qilamiz
        _logger.LogInformation("Attempting to send SMS via SmsFly to {Phone}", phone);
        var result = await _smsFlySender.SendAsync(phone, message);

        if (result)
        {
            return true;
        }

        // SmsFly ishlamasa, Sayqal orqali harakat qilamiz
        _logger.LogWarning("SmsFly failed, attempting via Sayqal to {Phone}", phone);
        result = await _sayqalSender.SendAsync(phone, message);

        if (result)
        {
            return true;
        }

        // Ikkala provider ham ishlamadi
        _logger.LogError("Both SMS providers failed for {Phone}", phone);
        return false;
    }
}
