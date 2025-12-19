using Convoy.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Convoy.Service.Services.SmsProviders;

/// <summary>
/// Sayqal Routee orqali SMS yuborish
/// </summary>
public class SayqalSender : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SayqalSender> _logger;
    private readonly string _userName;
    private readonly string _secretKey;
    private readonly string _apiUrl;

    public SayqalSender(HttpClient httpClient, IConfiguration configuration, ILogger<SayqalSender> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _userName = configuration["SmsProviders:Sayqal:UserName"] ?? "ismoilovdb";
        _secretKey = configuration["SmsProviders:Sayqal:SecretKey"] ?? "298174a623207364db70a02ebb57124e";
        _apiUrl = configuration["SmsProviders:Sayqal:ApiUrl"] ?? "https://routee.sayqal.uz/sms/TransmitSMS";
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

            _logger.LogInformation("Sayqal: Sending SMS to formatted phone: {Phone}", cleanPhone);

            var utime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var payload = new
            {
                utime = utime,
                username = _userName,
                service = new { service = 1 },
                message = new
                {
                    smsid = 1,
                    phone = cleanPhone,
                    text = $"GARANT Tasdiqlash uchun maxsus kod: {message}"
                }
            };

            // Token generatsiya qilish
            var token = GenerateToken(utime);

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
            {
                Content = content
            };
            request.Headers.Add("X-Access-Token", token);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SMS successfully sent via Sayqal to {Phone}", phone);
                return true;
            }

            _logger.LogWarning("Sayqal returned status code {StatusCode} for {Phone}",
                response.StatusCode, phone);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS via Sayqal to {Phone}", phone);
            return false;
        }
    }

    private string GenerateToken(long utime)
    {
        var tokenString = $"TransmitSMS {_userName} {_secretKey} {utime}";
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(tokenString));
        return Convert.ToHexString(hashBytes).ToLower();
    }
}
