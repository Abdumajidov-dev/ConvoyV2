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
    private readonly IUserService _userService;
    private readonly ILogger<AuthService> _logger;
    private readonly int[] _allowedPositionIds;

    // Worker ma'lumotlarini vaqtinchalik saqlash uchun (OTP verify paytida kerak bo'ladi)
    private readonly Dictionary<string, PhpWorkerDto> _workerCache = new();

    public AuthService(
        IPhpApiService phpApiService,
        IOtpService otpService,
        ISmsService smsService,
        ITokenService tokenService,
        IUserService userService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _phpApiService = phpApiService;
        _otpService = otpService;
        _smsService = smsService;
        _tokenService = tokenService;
        _userService = userService;
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

            // User'ni local database'ga sync qilish (create yoki update)
            await SyncUserFromPhpApiAsync(worker);

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

            // Image claim'ni olish (agar mavjud bo'lsa)
            var imageClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "image");
            var image = imageClaim?.Value;

            // Response DTO yaratish (permissions'siz - PHP API'da boshqariladi)
            var response = new UserPermissionsDto
            {
                UserId = userId,
                Name = userName,
                Phone = phoneNumber,
                Image = image,
                Role = null, // PHP API'da boshqariladi
                RoleId = new List<long>(), // PHP API'da boshqariladi
                Permissions = new Dictionary<string, List<string>>() // PHP API'da boshqariladi
            };

            _logger.LogInformation("Successfully retrieved user data for user {UserId}", userId);

            return AuthResponseDto<UserPermissionsDto>.Success(response, "Foydalanuvchi ma'lumotlari");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user data from token");
            return AuthResponseDto<UserPermissionsDto>.Failure("Xatolik yuz berdi");
        }
    }

    /// <summary>
    /// PHP API'dan kelgan user ma'lumotlarini local database'ga sync qilish
    /// Agar user mavjud bo'lsa - update, bo'lmasa - create
    /// </summary>
    private async Task SyncUserFromPhpApiAsync(PhpWorkerDto worker)
    {
        try
        {
            _logger.LogInformation("Syncing user from PHP API: WorkerId={WorkerId}, Phone={Phone}",
                worker.WorkerId, worker.PhoneNumber);

            // User'ni user_id bo'yicha qidirish (PHP API worker_id)
            var existingUser = await _userService.GetByUserIdAsync(worker.WorkerId);

            if (existingUser != null)
            {
                // User mavjud - ma'lumotlarni yangilash
                _logger.LogInformation("User exists with user_id={UserId}, updating data", worker.WorkerId);

                existingUser.Name = worker.WorkerName;
                existingUser.Phone = worker.PhoneNumber;
                existingUser.WorkerGuid = worker.WorkerGuid;
                existingUser.BranchGuid = worker.BranchGuid;
                existingUser.PositionId = worker.PositionId;
                existingUser.Image = worker.Image; // PHP API'dan kelgan image URL
                existingUser.IsActive = true;

                await _userService.UpdateAsync(existingUser.Id, existingUser);

                _logger.LogInformation("User updated successfully: UserId={UserId}, Name={Name}",
                    worker.WorkerId, worker.WorkerName);
            }
            else
            {
                // User mavjud emas - yangi user yaratish
                _logger.LogInformation("User not found with user_id={UserId}, creating new user", worker.WorkerId);

                var newUser = new Domain.Entities.User
                {
                    UserId = worker.WorkerId,
                    Name = worker.WorkerName,
                    Phone = worker.PhoneNumber,
                    WorkerGuid = worker.WorkerGuid,
                    BranchGuid = worker.BranchGuid,
                    PositionId = worker.PositionId,
                    Image = worker.Image,
                    IsActive = true
                };

                await _userService.CreateAsync(newUser);

                _logger.LogInformation("User created successfully: UserId={UserId}, Name={Name}",
                    worker.WorkerId, worker.WorkerName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user from PHP API: WorkerId={WorkerId}",
                worker.WorkerId);
            // Don't throw - authentication should continue even if user sync fails
        }
    }
}
