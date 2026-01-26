using Convoy.Service.DTOs;

namespace Convoy.Service.Interfaces;

/// <summary>
/// PHP API bilan ishlash uchun interfeys - barcha auth requestlarni PHP'ga proxy qiladi
/// </summary>
public interface IPhpApiService
{
    /// <summary>
    /// Telefon raqamni PHP API orqali verify qiladi (proxy to PHP /auth/verify/phone)
    /// </summary>
    Task<PhpApiResponse<PhpWorkerDto>> VerifyNumberAsync(string phoneNumber);

    /// <summary>
    /// OTP kod yuborishni PHP API orqali amalga oshiradi (proxy to PHP /auth/send-otp)
    /// </summary>
    Task<PhpApiResponse<object>> SendOtpAsync(string phoneNumber);

    /// <summary>
    /// OTP kodni PHP API orqali verify qiladi va JWT token oladi (proxy to PHP /auth/verify-otp)
    /// </summary>
    Task<PhpApiResponse<PhpAuthTokenDto>> VerifyOtpAsync(string phoneNumber, string otpCode);

    /// <summary>
    /// JWT token orqali user ma'lumotlarini PHP API'dan oladi (proxy to PHP /auth/me)
    /// </summary>
    Task<PhpApiResponse<PhpUserDto>> GetMeAsync(string token);

    /// <summary>
    /// PHP API dan filiallar ro'yxatini oladi (search bilan yoki search'siz)
    /// </summary>
    /// <param name="searchTerm">Qidiruv matni (optional)</param>
    /// <returns>Filiallar ro'yxati</returns>
    Task<List<BranchDto>> GetBranchesAsync(string? searchTerm = null);
}
