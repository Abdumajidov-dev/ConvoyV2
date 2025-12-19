using Convoy.Data.DbContexts;
using Convoy.Domain.Entities;
using Convoy.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services;

public class OtpService : IOtpService
{
    private readonly AppDbConText _context;
    private readonly ILogger<OtpService> _logger;
    private readonly int _otpExpirationMinutes;
    private readonly int _otpLength;

    public OtpService(AppDbConText context, IConfiguration configuration, ILogger<OtpService> logger)
    {
        _context = context;
        _logger = logger;
        _otpExpirationMinutes = int.TryParse(configuration["Auth:OtpExpirationMinutes"], out var expMin) ? expMin : 5;
        _otpLength = int.TryParse(configuration["Auth:OtpLength"], out var len) ? len : 6;
    }

    public async Task<string> GenerateOtpAsync(string phoneNumber)
    {
        // Eski OTP kodlarni bekor qilish (bir telefon uchun faqat bitta aktiv OTP)
        var existingOtps = await _context.OtpCodes
            .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed)
            .ToListAsync();

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
}
