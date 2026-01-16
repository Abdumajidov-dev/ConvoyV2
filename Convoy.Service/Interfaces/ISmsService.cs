namespace Convoy.Service.Interfaces;

/// <summary>
/// SMS yuborish uchun umumiy interfeys
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// SMS yuboradi
    /// </summary>
    /// <param name="phone">Telefon raqam</param>
    /// <param name="message">SMS matni</param>
    /// <returns>Yuborildi yoki yo'q</returns>
    Task<bool> SendAsync(string phone, string message);
}
