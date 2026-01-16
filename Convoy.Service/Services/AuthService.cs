using Convoy.Service.DTOs;
using Convoy.Service.Extensions;
using Convoy.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services;

public class AuthService : IAuthService
{
    private readonly IPhpApiService _phpApiService;
    private readonly IOtpService _otpService;
    private readonly ISmsService _smsService;
    private readonly ITokenService _tokenService;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<AuthService> _logger;
    private readonly int[] _allowedPositionIds;

    // Worker ma'lumotlarini vaqtinchalik saqlash uchun (OTP verify paytida kerak bo'ladi)
    private readonly Dictionary<string, PhpWorkerDto> _workerCache = new();

    public AuthService(
        IPhpApiService phpApiService,
        IOtpService otpService,
        ISmsService smsService,
        ITokenService tokenService,
        IPermissionService permissionService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _phpApiService = phpApiService;
        _otpService = otpService;
        _smsService = smsService;
        _tokenService = tokenService;
        _permissionService = permissionService;
        _logger = logger;

        // Allowed position IDs configuration dan o'qish
        var positionIdsConfig = configuration["Auth:AllowedPositionIds"];
        if (!string.IsNullOrEmpty(positionIdsConfig))
        {
            _allowedPositionIds = positionIdsConfig
                .Split(',')
                .Select(id => int.Parse(id.Trim()))
                .ToArray();
        }
        else
        {
            _allowedPositionIds = Array.Empty<int>();
            _logger.LogWarning("No allowed position IDs configured. All positions will be accepted.");
        }
    }

    public async Task<AuthResponseDto<PhpWorkerDto>> VerifyNumberAsync(string phoneNumber)
    {
        try
        {
            // PHP API dan user ma'lumotlarini olish
            var worker = await _phpApiService.VerifyUserAsync(phoneNumber);

            if (worker == null)
            {
                _logger.LogWarning("User not found in PHP API for phone {Phone}", phoneNumber);
                return AuthResponseDto<PhpWorkerDto>.Failure("Foydalanuvchi topilmadi");
            }

            // Position ID tekshirish (agar configured bo'lsa)
            if (_allowedPositionIds.Length > 0 && !_allowedPositionIds.Contains(worker.PositionId))
            {
                _logger.LogWarning("User {WorkerId} has invalid position ID {PositionId}",
                    worker.WorkerId, worker.PositionId);
                return AuthResponseDto<PhpWorkerDto>.Failure("Sizning lavozimingiz tizimga kirishga ruxsat bermaydi");
            }

            // Worker ma'lumotlarini cache ga saqlash (keyingi qadamlar uchun)
            _workerCache[phoneNumber] = worker;

            _logger.LogInformation("Successfully verified phone {Phone} for worker {WorkerId}",
                phoneNumber, worker.WorkerId);

            return AuthResponseDto<PhpWorkerDto>.Success(worker, "Telefon raqam tasdiqlandi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying phone number {Phone}", phoneNumber);
            return AuthResponseDto<PhpWorkerDto>.Failure("Xatolik yuz berdi");
        }
    }

    public async Task<AuthResponseDto<object>> SendOtpAsync(string phoneNumber)
    {
        try
        {
            // OTP kod generatsiya qilish
            var otpCode = await _otpService.GenerateOtpAsync(phoneNumber);

            // SMS yuborish (ikki provayderdan biri ishlashi kerak)
            var smsSent = await _smsService.SendAsync(phoneNumber, otpCode);

            if (!smsSent)
            {
                _logger.LogError("Failed to send OTP to {Phone}", phoneNumber);
                return AuthResponseDto<object>.Failure("SMS yuborishda xatolik yuz berdi");
            }

            _logger.LogInformation("OTP sent successfully to {Phone}", phoneNumber);

            return AuthResponseDto<object>.Success(
                new { message = "SMS kod yuborildi" },
                "SMS kod yuborildi"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP to {Phone}", phoneNumber);
            return AuthResponseDto<object>.Failure("Xatolik yuz berdi");
        }
    }

    public async Task<AuthResponseDto<VerifyOtpResponseDto>> VerifyOtpAsync(string phoneNumber, string otpCode)
    {
        try
        {
            // OTP kodni tekshirish
            var isValid = await _otpService.ValidateOtpAsync(phoneNumber, otpCode);

            if (!isValid)
            {
                _logger.LogWarning("Invalid OTP code for {Phone}", phoneNumber);
                return AuthResponseDto<VerifyOtpResponseDto>.Failure("Noto'g'ri kod yoki kod muddati tugagan");
            }

            // Worker ma'lumotlarini cache dan olish
            if (!_workerCache.TryGetValue(phoneNumber, out var worker))
            {
                // Agar cache da bo'lmasa, PHP API dan qayta olish
                worker = await _phpApiService.VerifyUserAsync(phoneNumber);

                if (worker == null)
                {
                    return AuthResponseDto<VerifyOtpResponseDto>.Failure("Foydalanuvchi topilmadi");
                }
            }

            // JWT token generatsiya qilish
            var token = _tokenService.GenerateToken(worker);

            // Token expiration time olish
            var expiresAt = _tokenService.GetExpiryFromToken(token);
            var now = DateTimeExtensions.NowInApplicationTime();
            var expiresInSeconds = expiresAt.HasValue
                ? (long)(expiresAt.Value - now).TotalSeconds
                : 0;

            var response = new VerifyOtpResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt ?? now,
                ExpiresInSeconds = expiresInSeconds
            };

            // Cache dan o'chirish
            _workerCache.Remove(phoneNumber);

            _logger.LogInformation("Successfully authenticated worker {WorkerId}, token expires at {ExpiresAt}",
                worker.WorkerId, expiresAt);

            return AuthResponseDto<VerifyOtpResponseDto>.Success(response, "Muvaffaqiyatli tizimga kirildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP for {Phone}", phoneNumber);
            return AuthResponseDto<VerifyOtpResponseDto>.Failure("Xatolik yuz berdi");
        }
    }

    public async Task<AuthResponseDto<UserPermissionsDto>> GetMeAsync(string token)
    {
        try
        {
            // Tokenni validate qilish va worker ID olish
            var workerId = _tokenService.ValidateToken(token);

            if (workerId == null)
            {
                _logger.LogWarning("Invalid token provided");
                return AuthResponseDto<UserPermissionsDto>.Failure("Token noto'g'ri yoki muddati tugagan");
            }

            // Token ichidagi ma'lumotlarni parse qilish
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var userId = int.Parse(jwtToken.Claims.First(c => c.Type == "nameid").Value);
            var userName = jwtToken.Claims.First(c => c.Type == "unique_name").Value;
            var phoneNumber = jwtToken.Claims.First(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone").Value;

            // User'ning role'larini olish
            var rolesResult = await _permissionService.GetUserRolesAsync(userId);
            var roles = rolesResult.Success ? rolesResult.Data : new List<Domain.Entities.Role>();

            // User'ning permission'larini grouped format'da olish
            var groupedPermissions = await _permissionService.GetUserPermissionsGroupedAsync(userId);

            // Response DTO yaratish
            var response = new UserPermissionsDto
            {
                UserId = userId,
                Name = userName,
                Phone = phoneNumber,
                Image = null, // Token ichida image yo'q
                Role = roles?.FirstOrDefault()?.Name,
                RoleId = roles?.Select(r => r.Id).ToList() ?? new List<long>(),
                Permissions = groupedPermissions
            };

            _logger.LogInformation("Successfully retrieved user data with role '{RoleName}', {RoleIdCount} role IDs and {PermissionCount} permission groups for user {UserId}",
                response.Role ?? "no role", response.RoleId.Count, response.Permissions.Count, userId);

            return AuthResponseDto<UserPermissionsDto>.Success(response, "Foydalanuvchi ma'lumotlari");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user data from token");
            return AuthResponseDto<UserPermissionsDto>.Failure("Xatolik yuz berdi");
        }
    }
}
