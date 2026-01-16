using System;

namespace Convoy.Service.Extensions;

/// <summary>
/// DateTime va DateTimeOffset extension metodlari - Markazlashtirilgan timezone management
/// Barcha sana/vaqt konvertatsiyalarini bir joyda boshqarish uchun
/// </summary>
public static class DateTimeExtensions
{
    // Loyiha timezone'i - Toshkent (UTC+5)
    // Bu qiymatni o'zgartirish orqali butun loyihadagi timezone'ni o'zgartirish mumkin
    private static readonly TimeZoneInfo ApplicationTimeZone = TimeZoneInfo.FindSystemTimeZoneById("West Asia Standard Time");
    private static readonly TimeSpan ApplicationOffset = new TimeSpan(5, 0, 0); // UTC+5

    /// <summary>
    /// Application timezone'ini olish (Toshkent - UTC+5)
    /// </summary>
    public static TimeZoneInfo GetApplicationTimeZone() => ApplicationTimeZone;

    /// <summary>
    /// Application timezone offset'ini olish (UTC+5)
    /// </summary>
    public static TimeSpan GetApplicationOffset() => ApplicationOffset;

    /// <summary>
    /// Har qanday formatdagi string sanani application timezone'ida DateTime'ga parse qilish
    /// Qo'llab-quvvatlanadigan formatlar:
    /// - ISO 8601: "2026-01-10T09:23:23.744Z", "2026-01-10T09:23:23+05:00"
    /// - PostgreSQL: "2026-01-10 20:48:48.158+05"
    /// - Oddiy format: "2026-01-10", "2026-01-10 20:48:48"
    /// </summary>
    /// <param name="dateString">Parse qilinadigan sana string</param>
    /// <returns>Application timezone'ida DateTime (UTC'ga konvertatsiya qilingan)</returns>
    public static DateTime ParseToApplicationTime(this string dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            throw new ArgumentException("Date string cannot be null or empty", nameof(dateString));

        DateTime parsedDate;

        // 1. DateTimeOffset orqali parse qilishga harakat (timezone bilan)
        if (DateTimeOffset.TryParse(dateString, out var dateTimeOffset))
        {
            // UTC'ga o'tkazish
            return dateTimeOffset.UtcDateTime;
        }

        // 2. Oddiy DateTime sifatida parse qilish (timezone bo'lmagan)
        if (DateTime.TryParse(dateString, out parsedDate))
        {
            // Agar Kind = Unspecified bo'lsa, uni application timezone'ida deb hisoblaymiz
            if (parsedDate.Kind == DateTimeKind.Unspecified)
            {
                // Application timezone'ida deb qabul qilib, UTC'ga o'tkazish
                return TimeZoneInfo.ConvertTimeToUtc(parsedDate, ApplicationTimeZone);
            }

            // Agar Kind = Local yoki Utc bo'lsa, to'g'ridan-to'g'ri qaytarish
            return parsedDate.ToUniversalTime();
        }

        throw new FormatException($"Unable to parse date string: {dateString}");
    }

    /// <summary>
    /// DateTime obyektini application timezone'iga o'tkazish
    /// Database'ga saqlashdan oldin ishlatiladi
    /// </summary>
    /// <param name="dateTime">Konvertatsiya qilinadigan DateTime</param>
    /// <returns>Application timezone'ida DateTime (UTC'ga konvertatsiya qilingan)</returns>
    public static DateTime ToApplicationTime(this DateTime dateTime)
    {
        switch (dateTime.Kind)
        {
            case DateTimeKind.Utc:
                // Allaqachon UTC - qaytarish
                return dateTime;

            case DateTimeKind.Local:
                // Local vaqtni UTC'ga o'tkazish
                return dateTime.ToUniversalTime();

            case DateTimeKind.Unspecified:
                // Aniqlanmagan - application timezone'ida deb qabul qilish
                return TimeZoneInfo.ConvertTimeToUtc(dateTime, ApplicationTimeZone);

            default:
                return dateTime.ToUniversalTime();
        }
    }

    /// <summary>
    /// DateTime obyektini application timezone'idan foydalanuvchiga ko'rsatish uchun format qilish
    /// API response'larda ishlatiladi
    /// </summary>
    /// <param name="dateTime">Format qilinadigan DateTime (UTC)</param>
    /// <param name="format">Format string (default: "yyyy-MM-dd HH:mm:ss")</param>
    /// <returns>Application timezone'ida formatted string</returns>
    public static string ToApplicationTimeString(this DateTime dateTime, string format = "yyyy-MM-dd HH:mm:ss")
    {
        // UTC vaqtni application timezone'iga o'tkazish
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, ApplicationTimeZone);
        return localTime.ToString(format);
    }

    /// <summary>
    /// Kun boshi (00:00:00) va oxiri (23:59:59) uchun DateTime range yaratish
    /// Database query'larda ishlatiladi
    /// </summary>
    /// <param name="date">Kun sanasi</param>
    /// <returns>Tuple: (startDate UTC, endDate UTC)</returns>
    public static (DateTime startDate, DateTime endDate) ToDateRange(this DateTime date)
    {
        // Faqat kun qismini olish
        var dateOnly = date.Date;

        // Application timezone'ida kun boshi va oxiri
        var startDateLocal = DateTime.SpecifyKind(dateOnly, DateTimeKind.Unspecified);
        var endDateLocal = DateTime.SpecifyKind(dateOnly.AddDays(1), DateTimeKind.Unspecified);

        // UTC'ga o'tkazish
        var startDate = TimeZoneInfo.ConvertTimeToUtc(startDateLocal, ApplicationTimeZone);
        var endDate = TimeZoneInfo.ConvertTimeToUtc(endDateLocal, ApplicationTimeZone);

        return (startDate, endDate);
    }

    /// <summary>
    /// String sanani parse qilib kun boshi va oxiri uchun range yaratish
    /// Database query'larda ishlatiladi
    /// </summary>
    /// <param name="dateString">Parse qilinadigan sana string</param>
    /// <returns>Tuple: (startDate UTC, endDate UTC)</returns>
    public static (DateTime startDate, DateTime endDate) ParseToDateRange(this string dateString)
    {
        var parsedDate = ParseToApplicationTime(dateString);
        return ToDateRange(parsedDate);
    }

    /// <summary>
    /// DateTime'ni database'ga saqlash uchun to'g'ri formatga o'tkazish
    /// PostgreSQL timestamptz uchun
    /// </summary>
    /// <param name="dateTime">Saqlanadigan DateTime</param>
    /// <returns>UTC DateTime (database'ga to'g'ri saqlash uchun)</returns>
    public static DateTime ForDatabase(this DateTime dateTime)
    {
        // Har qanday formatdagi DateTime'ni UTC'ga o'tkazish
        return dateTime.ToApplicationTime();
    }

    /// <summary>
    /// Hozirgi vaqtni application timezone'ida olish
    /// </summary>
    /// <returns>Application timezone'ida hozirgi DateTime (UTC)</returns>
    public static DateTime NowInApplicationTime()
    {
        return DateTime.UtcNow;
    }

    /// <summary>
    /// Hozirgi vaqtni application timezone'ida formatted string sifatida olish
    /// </summary>
    /// <param name="format">Format string (default: "yyyy-MM-dd HH:mm:ss")</param>
    /// <returns>Application timezone'ida formatted string</returns>
    public static string NowInApplicationTimeString(string format = "yyyy-MM-dd HH:mm:ss")
    {
        var utcNow = DateTime.UtcNow;
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, ApplicationTimeZone);
        return localNow.ToString(format);
    }

    /// <summary>
    /// Bugungi sana uchun date range yaratish
    /// </summary>
    /// <returns>Tuple: (startDate UTC, endDate UTC)</returns>
    public static (DateTime startDate, DateTime endDate) TodayDateRange()
    {
        var today = DateTime.UtcNow.Date;
        return ToDateRange(today);
    }
}
