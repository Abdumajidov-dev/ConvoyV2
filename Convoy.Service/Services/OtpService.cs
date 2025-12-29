using Convoy.Data.DbContexts;
using Convoy.Domain.Entities;
using Convoy.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;


namespace Convoy.Service.Services;

public class OtpService : IOtpService
{
    private readonly AppDbConText _context;
    private readonly ILogger<OtpService> _logger;
    private readonly int _otpExpirationMinutes;
    private readonly int _otpLength;
    private readonly int _otpRateLimitSeconds;
    private readonly Dictionary<string, string> _testPhoneNumbers;

    public OtpService(AppDbConText context, IConfiguration configuration, ILogger<OtpService> logger)
    {
        _context = context;
        _logger = logger;
        _otpExpirationMinutes = int.TryParse(configuration["Auth:OtpExpirationMinutes"], out var expMin) ? expMin : 5;
        _otpLength = int.TryParse(configuration["Auth:OtpLength"], out var len) ? len : 6;
        _otpRateLimitSeconds = int.TryParse(configuration["Auth:OtpRateLimitSeconds"], out var rateLimit) ? rateLimit : 60;
        
        // Test telefon raqamlarini yuklash
        _testPhoneNumbers = new Dictionary<string, string>();
        var testPhoneSection = configuration.GetSection("Auth:TestPhoneNumbers");
        if (testPhoneSection.Exists())
        {
            foreach (var item in testPhoneSection.GetChildren())
            {
                _testPhoneNumbers[item.Key] = item.Value ?? "1111";
            }
            _logger.LogInformation("Loaded {Count} test phone numbers for development", _testPhoneNumbers.Count);
        }
    }

    public async Task<string> GenerateOtpAsync(string phoneNumber)
    {
        // Test telefon raqamini tekshirish
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        if (_testPhoneNumbers.TryGetValue(normalizedPhone, out var fixedCode))
        {
            _logger.LogWarning("🧪 [TEST MODE] Using fixed OTP code for test phone: {Phone} → {Code}", 
                phoneNumber, fixedCode);
            
            // Test raqamlar uchun ham database'ga yozamiz (consistency uchun)
            var now = DateTime.UtcNow;
            var otpCode = new OtpCode
            {
                PhoneNumber = phoneNumber,
                Code = fixedCode,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(_otpExpirationMinutes),
                IsUsed = false
            };

            _context.OtpCodes.Add(otpCode);
            await _context.SaveChangesAsync();

            return fixedCode;
        }

        // Eski OTP kodlarni bekor qilish (bir telefon uchun faqat bitta aktiv OTP)
        var existingOtps = await _context.OtpCodes
            .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed)
            .ToListAsync();

        // RATE LIMITING: Agar 0 bo'lmasa va oxirgi OTP configured seconds ichida jo'natilgan bo'lsa, xatolik qaytarish
        if (_otpRateLimitSeconds > 0)
        {
            var lastOtp = existingOtps.OrderByDescending(o => o.CreatedAt).FirstOrDefault();
            if (lastOtp != null)
            {
                var timeSinceLastOtp = DateTime.UtcNow - lastOtp.CreatedAt;
                if (timeSinceLastOtp.TotalSeconds < _otpRateLimitSeconds)
                {
                    var waitSeconds = (int)(_otpRateLimitSeconds - timeSinceLastOtp.TotalSeconds);
                    _logger.LogWarning("OTP rate limit exceeded for {Phone}. Last OTP sent {Seconds} seconds ago. Wait {Wait} more seconds",
                        phoneNumber, (int)timeSinceLastOtp.TotalSeconds, waitSeconds);
                    throw new InvalidOperationException($"Iltimos {waitSeconds} soniya kuting va qayta urinib ko'ring");
                }
            }
        }
        else
        {
            _logger.LogInformation("OTP rate limiting is DISABLED (OtpRateLimitSeconds = 0)");
        }

        foreach (var otp in existingOtps)
        {
            otp.IsUsed = true;
        }

        // Yangi OTP kod generatsiya qilish
        var code = GenerateRandomCode(_otpLength);
        var now = DateTime.UtcNow;

        var otpCode = new OtpCode
        {
            PhoneNumber = phoneNumber,
            Code = code,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_otpExpirationMinutes),
            IsUsed = false
        };

        _context.OtpCodes.Add(otpCode);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated OTP code for phone {Phone}, expires at {ExpiresAt}",
            phoneNumber, otpCode.ExpiresAt);

        // DEVELOPMENT: OTP kodni console ga chiqarish
        _logger.LogWarning("🔐 [DEVELOPMENT] OTP CODE FOR {Phone}: {Code}", phoneNumber, code);

        return code;
    }

    public async Task<bool> ValidateOtpAsync(string phoneNumber, string code)
    {
        var otpCode = await _context.OtpCodes
            .Where(o => o.PhoneNumber == phoneNumber && o.Code == code && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otpCode == null)
        {
            _logger.LogWarning("OTP validation failed: No matching code found for {Phone}", phoneNumber);
            return false;
        }

        if (!otpCode.IsValid)
        {
            _logger.LogWarning("OTP validation failed: Code expired for {Phone}", phoneNumber);
            return false;
        }

        // Kodni ishlatilgan deb belgilash
        otpCode.IsUsed = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("OTP successfully validated for {Phone}", phoneNumber);
        return true;
    }

    public async Task CleanupExpiredOtpsAsync()
    {
        var expiredDate = DateTime.UtcNow.AddDays(-1); // 1 kundan eski OTPlar

        var expiredOtps = await _context.OtpCodes
            .Where(o => o.CreatedAt < expiredDate)
            .ToListAsync();

        if (expiredOtps.Any())
        {
            _context.OtpCodes.RemoveRange(expiredOtps);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired OTP codes", expiredOtps.Count);
        }
    }

    private string GenerateRandomCode(int length)
    {
        var random = new Random();
        var code = string.Empty;

        for (int i = 0; i < length; i++)
        {
            code += random.Next(0, 10).ToString();
        }

        return code;
    }

    /// <summary>
    /// Telefon raqamni normalize qilish (test raqamlarni tekshirish uchun)
    /// Masalan: +998941033001, 998941033001, 941033001 → 941033001
    /// </summary>
    private string NormalizePhoneNumber(string phoneNumber)
    {
        // Barcha non-digit belgilarni olib tashlash
        var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
        
        // Agar 998 bilan boshlansa, uni olib tashlash
        if (digitsOnly.StartsWith("998"))
        {
            digitsOnly = digitsOnly.Substring(3);
        }
        
        return digitsOnly;
    }
}
