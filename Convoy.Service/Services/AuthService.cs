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
    /// User ma'lumotlarini olish - Token'dan user_id ni oladi va local database'dan ma'lumotlarni qaytaradi
    /// Local database'da user mavjud bo'lishi kerak (sync qilingan bo'lishi kerak)
    /// </summary>
    public async Task<AuthResponseDto<UserPermissionsDto>> GetMeAsync(string token)
    {
        try
        {
            _logger.LogInformation("Getting user info from token and local database");

            // Token'dan user_id ni olish
            var phpUser = _phpTokenService.DecodeToken(token);

            if (phpUser == null || phpUser.WorkerId <= 0)
            {
                _logger.LogWarning("Invalid token or user_id not found in token");
                return AuthResponseDto<UserPermissionsDto>.Failure("Token noto'g'ri yoki muddati tugagan");
            }

            var userId = phpUser.WorkerId;
            _logger.LogInformation("Token decoded successfully: UserId={UserId}", userId);

            // Local database'dan user ma'lumotlarini olish
            var localUser = await _userService.GetByUserIdAsync(userId);

            if (localUser == null)
            {
                _logger.LogWarning("User not found in local database: UserId={UserId}", userId);

                // User local DB'da yo'q - PHP API'dan sync qilish
                _logger.LogInformation("Fetching user from PHP API and syncing to local DB");
                var phpResponse = await _phpApiService.GetMeAsync(token);

                if (!phpResponse.Status || phpResponse.Result == null)
                {
                    return AuthResponseDto<UserPermissionsDto>.Failure("User topilmadi");
                }

                await SyncUserFromTokenAsync(phpResponse.Result);
                localUser = await _userService.GetByUserIdAsync(userId);

                if (localUser == null)
                {
                    return AuthResponseDto<UserPermissionsDto>.Failure("User yaratishda xatolik yuz berdi");
                }
            }

            // Local database'dan olingan ma'lumotlarni response DTO ga map qilish
            var response = new UserPermissionsDto
            {
                UserId = localUser.UserId ?? 0,
                Name = localUser.Name,
                Username = localUser.Username ?? "",
                Image = localUser.Image,
                Phone = localUser.Phone ?? "",
                BranchGuid = localUser.BranchGuid ?? "",
                Role = localUser.Role,
                IsActive = localUser.IsActive ? "true" : "false", // bool -> string conversion
                RoleId = new List<long>(), // Bo'sh array
                Permissions = new Dictionary<string, List<string>>() // Bo'sh object
            };

            _logger.LogInformation("Successfully retrieved user from local DB: UserId={UserId}, IsActive={IsActive}",
                localUser.UserId, localUser.IsActive);

            return AuthResponseDto<UserPermissionsDto>.Success(
                response,
                "Foydalanuvchi ma'lumotlari"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info");
            return AuthResponseDto<UserPermissionsDto>.Failure("Xatolik yuz berdi");
        }
    }

    /// <summary>
    /// PHP API rasm manzilini tozalash.
    /// PHP tomon ba'zan base URL'ni ikki marta qo'shib yuboradi:
    ///   https://garant-hr.uz/api/public/https://garant-hr.uz/api/public/images/...
    /// Bunday holatda oxirgi to'liq URL qoldiriladi.
    /// </summary>
    private static string? NormalizeImageUrl(string? photo)
    {
        if (string.IsNullOrWhiteSpace(photo))
            return photo;

        var trimmed = photo.Trim();

        // Birinchi belgidan keyin yana http(s):// uchrasa - takrorlangan prefiks
        var lastHttps = trimmed.LastIndexOf("https://", StringComparison.OrdinalIgnoreCase);
        var lastHttp = trimmed.LastIndexOf("http://", StringComparison.OrdinalIgnoreCase);
        var lastScheme = Math.Max(lastHttps, lastHttp);

        return lastScheme > 0 ? trimmed.Substring(lastScheme) : trimmed;
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
                existingUser.Image = NormalizeImageUrl(phpUser.Photo);
                existingUser.UserType = phpUser.Type;
                existingUser.Role = phpUser.Role;
                // Role: App.Allowed.Role dan olish (agar null bo'lsa phpUser.Role ishlatiladi)
               // existingUser.Role = phpUser.App?.Allowed?.Role ?? phpUser.Role;
                //existingUser.IsActive = true; // Token active bo'lsa user ham active

                await _userService.UpdateAsync(existingUser.Id, existingUser);

                _logger.LogInformation("User updated successfully: WorkerId={WorkerId}, Name={Name}, Role={Role}",
                    phpUser.WorkerId, phpUser.Name, existingUser.Role);
            }
            else
            {
                // User mavjud emas - yangi user yaratish
                _logger.LogInformation("User not found with worker_id={WorkerId}, creating new user", phpUser.WorkerId);

                // Role: App.Allowed.Role dan olish (agar null bo'lsa phpUser.Role ishlatiladi)
                var userRole = phpUser.App?.Allowed?.Role ?? phpUser.Role;

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
                    Image = NormalizeImageUrl(phpUser.Photo),
                    UserType = phpUser.Type,
                    Role = userRole,
                    IsActive = true
                };

                await _userService.CreateAsync(newUser);

                _logger.LogInformation("User created successfully: WorkerId={WorkerId}, Name={Name}, Role={Role}",
                    phpUser.WorkerId, phpUser.Name, userRole);
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
