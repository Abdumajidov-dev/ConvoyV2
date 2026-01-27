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
    private readonly IPhpTokenService _phpTokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IPhpApiService phpApiService,
        IUserService userService,
        IPhpTokenService phpTokenService,
        ILogger<AuthService> logger)
    {
        _phpApiService = phpApiService;
        _userService = userService;
        _phpTokenService = phpTokenService;
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
    /// Token'ni decode qilib user ma'lumotlarini local DB'ga sync qiladi
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

            var token = phpResponse.Result.Token;

            // Token'ni decode qilib user ma'lumotlarini olish
            _logger.LogInformation("Decoding JWT token to extract user data");
            var phpUser = _phpTokenService.DecodeToken(token);

            if (phpUser != null && phpUser.WorkerId > 0)
            {
                _logger.LogInformation("Token decoded successfully, syncing user worker_id={WorkerId}", phpUser.WorkerId);
                await SyncUserFromTokenAsync(phpUser);
            }
            else
            {
                _logger.LogWarning("Failed to decode token or worker_id is invalid");
            }

            // PHP API'dan kelgan tokenni qaytaramiz
            var response = new VerifyOtpResponseDto
            {
                Token = token,
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
    /// User ma'lumotlarini olish - Token'ni decode qilib local DB'dan oladi
    /// PHP API'ga murojaat qilmasdan token ichidagi ma'lumotlardan foydalanadi
    /// </summary>
    public async Task<AuthResponseDto<UserPermissionsDto>> GetMeAsync(string token)
    {
        try
        {
            _logger.LogInformation("Decoding JWT token to get user data");

            // Token'ni decode qilish
            var phpUser = _phpTokenService.DecodeToken(token);

            if (phpUser == null || phpUser.WorkerId <= 0)
            {
                _logger.LogWarning("Failed to decode token or invalid worker_id");
                return AuthResponseDto<UserPermissionsDto>.Failure("Token noto'g'ri yoki muddati tugagan");
            }

            // Token muddatini tekshirish
            if (!_phpTokenService.IsTokenValid(token))
            {
                _logger.LogWarning("Token expired for worker_id={WorkerId}", phpUser.WorkerId);
                return AuthResponseDto<UserPermissionsDto>.Failure("Token muddati tugagan");
            }

            // User'ni local database'ga sync qilish (create yoki update)
            await SyncUserFromTokenAsync(phpUser);

            // Response DTO yaratish (token'dan olingan ma'lumotlar)
            var response = new UserPermissionsDto
            {
                UserId = phpUser.WorkerId,
                BranchGuid = phpUser.BranchGuid ?? "",
                Name = phpUser.Name,
                Phone = phpUser.Phone,
                Username = phpUser.Username,
                Image = phpUser.Photo,
                Role = phpUser.Role,
                RoleId = new List<long>(), // PHP API'da boshqariladi
                Permissions = new Dictionary<string, List<string>>() // PHP API'da boshqariladi
            };

            _logger.LogInformation("Successfully decoded token and synced user worker_id={WorkerId}", phpUser.WorkerId);

            return AuthResponseDto<UserPermissionsDto>.Success(
                response,
                "Foydalanuvchi ma'lumotlari"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user from token");
            return AuthResponseDto<UserPermissionsDto>.Failure("Xatolik yuz berdi");
        }
    }

    /// <summary>
    /// Token'dan olingan user ma'lumotlarini local database'ga sync qilish
    /// worker_id bo'yicha user'ni qidiradi (agar user mavjud bo'lsa - update, bo'lmasa - create)
    /// </summary>
    private async Task SyncUserFromTokenAsync(PhpUserDto phpUser)
    {
        try
        {
            _logger.LogInformation("Syncing user from token: WorkerId={WorkerId}, Name={Name}",
                phpUser.WorkerId, phpUser.Name);

            // User'ni worker_id bo'yicha qidirish
            var existingUser = await _userService.GetByUserIdAsync(phpUser.WorkerId);

            if (existingUser != null)
            {
                // User mavjud - ma'lumotlarni yangilash
                _logger.LogInformation("User exists with worker_id={WorkerId}, updating data", phpUser.WorkerId);

                existingUser.Name = phpUser.Name;
                existingUser.Username = phpUser.Username;
                existingUser.Phone = phpUser.Phone;
                existingUser.WorkerGuid = phpUser.WorkerGuid;
                existingUser.BranchGuid = phpUser.FilialGuid;
                existingUser.BranchName = phpUser.FilialName;
                existingUser.PositionId = phpUser.PositionId;
                existingUser.Image = phpUser.Photo;
                existingUser.UserType = phpUser.Type;
                existingUser.Role = phpUser.Role;
                existingUser.IsActive = true; // Token active bo'lsa user ham active

                await _userService.UpdateAsync(existingUser.Id, existingUser);

                _logger.LogInformation("User updated successfully: WorkerId={WorkerId}, Name={Name}",
                    phpUser.WorkerId, phpUser.Name);
            }
            else
            {
                // User mavjud emas - yangi user yaratish
                _logger.LogInformation("User not found with worker_id={WorkerId}, creating new user", phpUser.WorkerId);

                var newUser = new Domain.Entities.User
                {
                    UserId = phpUser.WorkerId, // worker_id ni user_id sifatida saqlash
                    Name = phpUser.Name,
                    Username = phpUser.Username,
                    Phone = phpUser.Phone,
                    WorkerGuid = phpUser.WorkerGuid,
                    BranchGuid = phpUser.FilialGuid,
                    BranchName = phpUser.FilialName,
                    PositionId = phpUser.PositionId,
                    Image = phpUser.Photo,
                    UserType = phpUser.Type,
                    Role = phpUser.Role,
                    IsActive = true
                };

                await _userService.CreateAsync(newUser);

                _logger.LogInformation("User created successfully: WorkerId={WorkerId}, Name={Name}",
                    phpUser.WorkerId, phpUser.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user from token: WorkerId={WorkerId}",
                phpUser.WorkerId);
            // Don't throw - authentication should continue even if user sync fails
        }
    }
}
