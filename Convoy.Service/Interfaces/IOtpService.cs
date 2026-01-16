using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convoy.Service.Interfaces;

public interface IOtpService
{
    /// <summary>
    /// Yangi OTP kod generatsiya qiladi va saqlaydi
    /// </summary>
    /// <param name="phoneNumber">Telefon raqam</param>
    /// <returns>Generatsiya qilingan kod</returns>
    Task<string> GenerateOtpAsync(string phoneNumber);

    /// <summary>
    /// OTP kodni tekshiradi
    /// </summary>
    /// <param name="phoneNumber">Telefon raqam</param>
    /// <param name="code">OTP kod</param>
    /// <returns>To'g'ri yoki noto'g'ri</returns>
    Task<bool> ValidateOtpAsync(string phoneNumber, string code);

    /// <summary>
    /// Eski va ishlatilmagan OTP kodlarni o'chiradi
    /// </summary>
    Task CleanupExpiredOtpsAsync();
}
