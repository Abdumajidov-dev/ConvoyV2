using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services;

/// <summary>
/// AuthService - barcha authentication requestlarni PHP API'ga proxy qiladi
/// OTP va SMS funksiyalari olib tashlandi - PHP API'da boshqariladi
/// </summary>
public class AuthService : IAuthService
{
    private readonly IPhpApiService _phpApiService;
    private readonly IUserService _userService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IPhpApiService phpApiService,
        IUserService userService,
        ILogger<AuthService> logger)
    {
        _phpApiService = phpApiService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Telefon raqamni verify qilish - PHP API'ga proxy
    /// </summary>
    public async Task<AuthResponseDto<PhpWorkerDto>> VerifyNumberAsync(string phoneNumber)
    {
        try
        {
            _logger.LogInformation("Proxying verify_number request to PHP API for phone: {Phone}", phoneNumber);

            var phpResponse = await _phpApiService.VerifyNumberAsync(phoneNumber);

            if (!phpResponse.Status || phpResponse.Result == null)
            {
                var errorMessage = phpResponse.GetMessage();
                _logger.LogWarning("PHP API verify_number failed for phone {Phone}: {Message}",
                    phoneNumber, errorMessage);
                return AuthResponseDto<PhpWorkerDto>.Failure(
                    string.IsNullOrEmpty(errorMessage) ? "Foydalanuvchi topilmadi" : errorMessage
                );
            }

            _logger.LogInformation("PHP API verify_number success for phone {Phone}", phoneNumber);
            var successMessage = phpResponse.GetMessage();
            return AuthResponseDto<PhpWorkerDto>.Success(
                phpResponse.Result,
                string.IsNullOrEmpty(successMessage) ? "Telefon raqam tasdiqlandi" : successMessage
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying verify_number to PHP API for {Phone}", phoneNumber);
            return AuthResponseDto<PhpWorkerDto>.Failure("Xatolik yuz berdi");
        }
    }

    /// <summary>
    /// OTP kod yuborish - PHP API'ga proxy
    /// </summary>
    public async Task<AuthResponseDto<object>> SendOtpAsync(string phoneNumber)
    {
        try
        {
            _logger.LogInformation("Proxying send_otp request to PHP API for phone: {Phone}", phoneNumber);

            var phpResponse = await _phpApiService.SendOtpAsync(phoneNumber);

            if (!phpResponse.Status)
            {
                var errorMessage = phpResponse.GetMessage();
                _logger.LogWarning("PHP API send_otp failed for phone {Phone}: {Message}",
                    phoneNumber, errorMessage);
                return AuthResponseDto<object>.Failure(
                    string.IsNullOrEmpty(errorMessage) ? "OTP yuborishda xatolik" : errorMessage
                );
            }

            _logger.LogInformation("PHP API send_otp success for phone {Phone}", phoneNumber);
            var successMessage = phpResponse.GetMessage();
            return AuthResponseDto<object>.Success(
                phpResponse.Result ?? new { },
                string.IsNullOrEmpty(successMessage) ? "OTP kod yuborildi" : successMessage
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying send_otp to PHP API for {Phone}", phoneNumber);
            return AuthResponseDto<object>.Failure("Xatolik yuz berdi");
        }
    }

    /// <summary>
    /// OTP kodni verify qilish va JWT token olish - PHP API'ga proxy
    /// PHP API JWT token qaytaradi, uni biz client'ga o'tkazamiz
    /// </summary>
    public async Task<AuthResponseDto<VerifyOtpResponseDto>> VerifyOtpAsync(string phoneNumber, string otpCode)
    {
        try
        {
            _logger.LogInformation("Proxying verify_otp request to PHP API for phone: {Phone}", phoneNumber);

            var phpResponse = await _phpApiService.VerifyOtpAsync(phoneNumber, otpCode);

            if (!phpResponse.Status || phpResponse.Result == null)
            {
                var errorMessage = phpResponse.GetMessage();
                _logger.LogWarning("PHP API verify_otp failed for phone {Phone}: {Message}",
                    phoneNumber, errorMessage);
                return AuthResponseDto<VerifyOtpResponseDto>.Failure(
                    string.IsNullOrEmpty(errorMessage) ? "Noto'g'ri kod" : errorMessage
                );
            }

            // PHP API'dan kelgan tokenni qaytaramiz
            var response = new VerifyOtpResponseDto
            {
                Token = phpResponse.Result.Token,
            };

            _logger.LogInformation("PHP API verify_otp success for phone {Phone}", phoneNumber);
            var successMessage = phpResponse.GetMessage();
            return AuthResponseDto<VerifyOtpResponseDto>.Success(
                response,
                string.IsNullOrEmpty(successMessage) ? "Muvaffaqiyatli tizimga kirildi" : successMessage
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying verify_otp to PHP API for {Phone}", phoneNumber);
            return AuthResponseDto<VerifyOtpResponseDto>.Failure("Xatolik yuz berdi");
        }
    }

    /// <summary>
    /// User ma'lumotlarini olish - PHP API'ga proxy va local DB'ga sync
    /// PHP API'dan user ma'lumotlarini oladi va local database'ga saqlab/yangilab qo'yadi
    /// </summary>
    public async Task<AuthResponseDto<UserPermissionsDto>> GetMeAsync(string token)
    {
        try
        {
            _logger.LogInformation("Proxying get_me request to PHP API");

            // PHP API'dan user ma'lumotlarini olish
            var phpResponse = await _phpApiService.GetMeAsync(token);

            if (!phpResponse.Status || phpResponse.Result == null)
            {
                var errorMessage = phpResponse.GetMessage();
                _logger.LogWarning("PHP API get_me failed: {Message}", errorMessage);
                return AuthResponseDto<UserPermissionsDto>.Failure(
                    string.IsNullOrEmpty(errorMessage) ? "Foydalanuvchi topilmadi" : errorMessage
                );
            }

            var phpUser = phpResponse.Result;

            // User'ni local database'ga sync qilish (create yoki update)
            await SyncUserFromPhpApiAsync(phpUser);

            // Response DTO yaratish
            var response = new UserPermissionsDto
            {
                UserId = phpUser.Id,
                Name = phpUser.Name,
                Phone = phpUser.Phone,
                Username = phpUser.Username,
                Image = phpUser.Image,
                Role = phpUser.Role,
                RoleId = new List<long>(), // PHP API'da boshqariladi
                Permissions = new Dictionary<string, List<string>>() // PHP API'da boshqariladi
            };

            _logger.LogInformation("Successfully retrieved and synced user data for user {UserId}", phpUser.Id);

            var successMessage = phpResponse.GetMessage();
            return AuthResponseDto<UserPermissionsDto>.Success(
                response,
                string.IsNullOrEmpty(successMessage) ? "Foydalanuvchi ma'lumotlari" : successMessage
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying get_me to PHP API");
            return AuthResponseDto<UserPermissionsDto>.Failure("Xatolik yuz berdi");
        }
    }

    /// <summary>
    /// PHP API'dan kelgan user ma'lumotlarini local database'ga sync qilish
    /// Agar user mavjud bo'lsa - update, bo'lmasa - create
    /// </summary>
    private async Task SyncUserFromPhpApiAsync(PhpUserDto phpUser)
    {
        try
        {
            _logger.LogInformation("Syncing user from PHP API: UserId={UserId}, Phone={Phone}",
                phpUser.Id, phpUser.Phone);

            // User'ni user_id bo'yicha qidirish (PHP API user ID)
            var existingUser = await _userService.GetByUserIdAsync(phpUser.Id);

            if (existingUser != null)
            {
                // User mavjud - ma'lumotlarni yangilash
                _logger.LogInformation("User exists with user_id={UserId}, updating data", phpUser.Id);

                existingUser.Name = phpUser.Name;
                existingUser.Username = phpUser.Username;
                existingUser.Phone = phpUser.Phone;
                existingUser.WorkerGuid = phpUser.WorkerGuid;
                existingUser.BranchGuid = phpUser.BranchGuid;
                existingUser.PositionId = phpUser.PositionId;
                existingUser.Image = phpUser.Image;
                existingUser.IsActive = phpUser.IsActive == 1;

                await _userService.UpdateAsync(existingUser.Id, existingUser);

                _logger.LogInformation("User updated successfully: UserId={UserId}, Name={Name}",
                    phpUser.Id, phpUser.Name);
            }
            else
            {
                // User mavjud emas - yangi user yaratish
                _logger.LogInformation("User not found with user_id={UserId}, creating new user", phpUser.Id);

                var newUser = new Domain.Entities.User
                {
                    UserId = phpUser.Id,
                    Name = phpUser.Name,
                    Username = phpUser.Username,
                    Phone = phpUser.Phone,
                    WorkerGuid = phpUser.WorkerGuid,
                    BranchGuid = phpUser.BranchGuid,
                    PositionId = phpUser.PositionId,
                    Image = phpUser.Image,
                    IsActive = phpUser.IsActive == 1
                };

                await _userService.CreateAsync(newUser);

                _logger.LogInformation("User created successfully: UserId={UserId}, Name={Name}",
                    phpUser.Id, phpUser.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user from PHP API: UserId={UserId}",
                phpUser.Id);
            // Don't throw - authentication should continue even if user sync fails
        }
    }
}
