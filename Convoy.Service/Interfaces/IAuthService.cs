using Convoy.Service.DTOs;

namespace Convoy.Service.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// 1-qadam: Telefon raqamni tekshiradi va position idni validatsiya qiladi
    /// </summary>
    /// <param name="phoneNumber">Telefon raqam</param>
    /// <returns>AuthResponse with PhpWorkerDto yoki error</returns>
    Task<AuthResponseDto<PhpWorkerDto>> VerifyNumberAsync(string phoneNumber);

    /// <summary>
    /// 2-qadam: OTP kod generatsiya qilib SMS yuboradi
    /// </summary>
    /// <param name="phoneNumber">Telefon raqam</param>
    /// <returns>AuthResponse with success message yoki error</returns>
    Task<AuthResponseDto<object>> SendOtpAsync(string phoneNumber);

    /// <summary>
    /// 3-qadam: OTP kodni tekshiradi va JWT token qaytaradi
    /// </summary>
    /// <param name="phoneNumber">Telefon raqam</param>
    /// <param name="otpCode">OTP kod</param>
    /// <returns>AuthResponse with token va worker data yoki error</returns>
    Task<AuthResponseDto<VerifyOtpResponseDto>> VerifyOtpAsync(string phoneNumber, string otpCode);

    /// <summary>
    /// 4-qadam: Token orqali user ma'lumotlarini qaytaradi (GetMe)
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>AuthResponse with PhpWorkerDto yoki error</returns>
    Task<AuthResponseDto<PhpWorkerDto>> GetMeAsync(string token);
}