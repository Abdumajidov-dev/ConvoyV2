using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace Convoy.Service.Services;

/// <summary>
/// PHP JWT token'larni decode qilish uchun service
/// IMPORTANT: Bu service signature validate qilmaydi, faqat decode qiladi
/// Token validation PHP API'da qilinadi (GetMeAsync orqali)
/// </summary>
public class PhpTokenService : IPhpTokenService
{
    private readonly ILogger<PhpTokenService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public PhpTokenService(ILogger<PhpTokenService> logger)
    {
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    /// <summary>
    /// JWT token'ni decode qilib, ichidagi user ma'lumotlarini oladi
    /// </summary>
    public PhpUserDto? DecodeToken(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Token is null or empty");
                return null;
            }

            // Bearer prefix'ni olib tashlash (agar mavjud bo'lsa)
            token = token.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();

            // Token'ni o'qib bo'lishini tekshirish
            if (!_tokenHandler.CanReadToken(token))
            {
                _logger.LogWarning("Cannot read token: {Token}", token.Substring(0, Math.Min(20, token.Length)) + "...");
                return null;
            }

            // Token'ni decode qilish (signature validate qilmasdan)
            var jwtToken = _tokenHandler.ReadJwtToken(token);

            // Claims'dan user ma'lumotlarini olish
            var userDto = new PhpUserDto
            {
                UserId = GetClaimValueInt(jwtToken, "user_id") ?? 0,
                WorkerId = GetClaimValueInt(jwtToken, "worker_id") ?? 0,
                WorkerGuid = GetClaimValue(jwtToken, "worker_guid"),
                Name = GetClaimValue(jwtToken, "name") ?? string.Empty,
                Phone = GetClaimValue(jwtToken, "phone") ?? string.Empty,
                FilialGuid = GetClaimValue(jwtToken, "filial_guid"),
                FilialName = GetClaimValue(jwtToken, "filial_name"),
                Photo = GetClaimValue(jwtToken, "photo"),
                Type = GetClaimValue(jwtToken, "type"),
                Role = GetClaimValue(jwtToken, "role"),
                LoginDate = GetClaimValue(jwtToken, "login_date"),
            };

            _logger.LogInformation("Token decoded successfully: worker_id={WorkerId}, name={Name}",
                userDto.WorkerId, userDto.Name);

            return userDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decoding token");
            return null;
        }
    }

    /// <summary>
    /// Token'ning amal qilish muddatini tekshiradi
    /// </summary>
    public bool IsTokenValid(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            token = token.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();

            if (!_tokenHandler.CanReadToken(token))
                return false;

            var jwtToken = _tokenHandler.ReadJwtToken(token);

            // Expiration time'ni tekshirish
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                _logger.LogWarning("Token expired at {ExpireTime}, current time: {CurrentTime}",
                    jwtToken.ValidTo, DateTime.UtcNow);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token expiration");
            return false;
        }
    }

    /// <summary>
    /// Token'dan worker_id ni oladi
    /// </summary>
    public int? GetWorkerIdFromToken(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            token = token.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();

            if (!_tokenHandler.CanReadToken(token))
                return null;

            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return GetClaimValueInt(jwtToken, "worker_id");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting worker_id from token");
            return null;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Claim'dan string qiymatni oladi
    /// </summary>
    private string? GetClaimValue(JwtSecurityToken token, string claimType)
    {
        var claim = token.Claims.FirstOrDefault(c => c.Type == claimType);
        return claim?.Value;
    }

    /// <summary>
    /// Claim'dan int qiymatni oladi
    /// </summary>
    private int? GetClaimValueInt(JwtSecurityToken token, string claimType)
    {
        var value = GetClaimValue(token, claimType);
        if (string.IsNullOrEmpty(value))
            return null;

        if (int.TryParse(value, out var result))
            return result;

        return null;
    }

    #endregion
}
