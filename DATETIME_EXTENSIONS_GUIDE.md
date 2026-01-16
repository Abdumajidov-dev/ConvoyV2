# DateTime Extensions - Markazlashtirilgan Timezone Management

## Maqsad

Loyihaning barcha qismlarida (development, staging, production) **bir xil timezone** bilan ishlash va sana/vaqt konvertatsiyalarini **bir joydan** boshqarish.

## Muammo

**Avvalgi holat:**
- User location post qiladi: `2026-01-10T09:23:23.744Z` (ISO 8601, UTC)
- Database'da saqlanadi: `2026-01-10 20:48:48.158+05` (Toshkent vaqti, UTC+5)
- Admin'ga qaytadi: `2026-01-10 14:48:48` (noma'lum timezone)
- Har bir joyda har xil konvertatsiya logikasi

**Yangi holat:**
- User location post qiladi: `2026-01-10 12:00` (har qanday format)
- Database'da saqlanadi: `2026-01-10 12:00+05` (Toshkent vaqti)
- Admin'ga qaytadi: `2026-01-10 12:00` (Toshkent vaqti)
- Barcha konvertatsiyalar `DateTimeExtensions` orqali

---

## DateTimeExtensions Metodlari

### 1. **ParseToApplicationTime(string dateString)**

Har qanday formatdagi string sanani application timezone'ida parse qilish.

**Qo'llab-quvvatlanadigan formatlar:**
- ISO 8601: `"2026-01-10T09:23:23.744Z"`, `"2026-01-10T09:23:23+05:00"`
- PostgreSQL: `"2026-01-10 20:48:48.158+05"`
- Oddiy: `"2026-01-10"`, `"2026-01-10 20:48:48"`

**Ishlatish:**
```csharp
using Convoy.Service.Extensions;

// Client'dan kelgan sana
var clientDate = "2026-01-10T09:23:23.744Z";

// Application timezone'ida parse qilish (UTC'ga konvertatsiya)
var parsedDate = clientDate.ParseToApplicationTime();

// Natija: UTC DateTime
Console.WriteLine(parsedDate); // 2026-01-10 04:23:23 (UTC)
```

---

### 2. **ToApplicationTime(DateTime dateTime)**

DateTime obyektini application timezone'iga o'tkazish.

**Ishlatish:**
```csharp
var someDate = DateTime.Now; // Local vaqt
var utcDate = someDate.ToApplicationTime(); // UTC'ga konvertatsiya

// Database'ga saqlash
await _repository.InsertAsync(new Location {
    RecordedAt = utcDate.ForDatabase() // yoki to'g'ridan-to'g'ri utcDate
});
```

---

### 3. **ToDateRange(DateTime date)**

Kun boshi (00:00:00) va oxiri (23:59:59) uchun DateTime range yaratish.

**Ishlatish:**
```csharp
var date = DateTime.Parse("2026-01-10");
var (startDate, endDate) = date.ToDateRange();

// Database query
var locations = await _repository.GetUserLocationsAsync(
    userId,
    startDate,  // 2026-01-09 19:00:00 UTC (2026-01-10 00:00:00+05)
    endDate     // 2026-01-10 19:00:00 UTC (2026-01-11 00:00:00+05)
);
```

---

### 4. **ParseToDateRange(string dateString)**

String sanani parse qilib range yaratish.

**Ishlatish:**
```csharp
var dateString = "2026-01-10";
var (startDate, endDate) = dateString.ParseToDateRange();

// Database query
var locations = await _locationRepository.GetMultipleUsersLocationsAsync(
    userIds,
    startDate,
    endDate,
    query.StartTime,
    query.EndTime
);
```

---

### 5. **ToApplicationTimeString(DateTime dateTime, string format)**

DateTime'ni application timezone'ida formatted string sifatida qaytarish.

**Ishlatish:**
```csharp
var utcDate = DateTime.UtcNow;

// Default format: "yyyy-MM-dd HH:mm:ss"
var formatted1 = utcDate.ToApplicationTimeString();
// Natija: "2026-01-10 15:30:45"

// Custom format
var formatted2 = utcDate.ToApplicationTimeString("dd/MM/yyyy HH:mm");
// Natija: "10/01/2026 15:30"
```

---

### 6. **NowInApplicationTime()**

Hozirgi vaqtni application timezone'ida olish.

**Ishlatish:**
```csharp
var now = DateTimeExtensions.NowInApplicationTime();
// Natija: UTC DateTime (hozirgi vaqt)

var nowString = DateTimeExtensions.NowInApplicationTimeString();
// Natija: "2026-01-10 15:30:45" (Toshkent vaqti)
```

---

### 7. **ForDatabase(DateTime dateTime)**

DateTime'ni database'ga saqlash uchun to'g'ri formatga o'tkazish.

**Ishlatish:**
```csharp
var location = new Location {
    RecordedAt = DateTime.Now.ForDatabase(), // UTC'ga konvertatsiya
    CreatedAt = DateTimeExtensions.NowInApplicationTime()
};
```

---

## Ishlatish Namunalari

### Service Layer'da

```csharp
using Convoy.Service.Extensions;

public class LocationService : ILocationService
{
    public async Task<ServiceResult<LocationResponseDto>> CreateUserLocationAsync(int userId, LocationDataDto locationData)
    {
        // RecordedAt'ni application timezone'ida parse qilish
        var recordedAt = locationData.RecordedAt.HasValue
            ? locationData.RecordedAt.Value.ToApplicationTime()
            : DateTimeExtensions.NowInApplicationTime();

        var location = new Location {
            UserId = userId,
            RecordedAt = recordedAt.ForDatabase(), // Database uchun
            Latitude = locationData.Latitude,
            Longitude = locationData.Longitude,
            CreatedAt = DateTimeExtensions.NowInApplicationTime()
        };

        await _locationRepository.InsertAsync(location);
        return ServiceResult<LocationResponseDto>.Created(responseDto, "Location yaratildi");
    }

    public async Task<ServiceResult<IEnumerable<LocationResponseDto>>> GetUserLocationsAsync(int userId, string date)
    {
        // String sanani parse qilib range yaratish
        var (startDate, endDate) = date.ParseToDateRange();

        var locations = await _locationRepository.GetUserLocationsAsync(userId, startDate, endDate);

        var result = locations.Select(l => new LocationResponseDto {
            RecordedAt = l.RecordedAt.ToApplicationTimeString() // Response uchun format
        });

        return ServiceResult<IEnumerable<LocationResponseDto>>.Ok(result, "Ma'lumotlar olindi");
    }
}
```

---

### Controller Layer'da

```csharp
[HttpPost("locations")]
public async Task<IActionResult> CreateLocation([FromBody] LocationRequest request)
{
    // Request'dan kelgan sanani parse qilish
    var recordedAt = request.RecordedAt.ParseToApplicationTime();

    var result = await _locationService.CreateUserLocationAsync(userId, new LocationDataDto {
        RecordedAt = recordedAt,
        Latitude = request.Latitude,
        Longitude = request.Longitude
    });

    return StatusCode(result.StatusCode, new {
        status = result.Success,
        message = result.Message,
        data = result.Data
    });
}

[HttpGet("locations")]
public async Task<IActionResult> GetLocations([FromQuery] string date)
{
    // String sanani parse qilish
    var (startDate, endDate) = date.ParseToDateRange();

    var result = await _locationService.GetUserLocationsAsync(userId, startDate, endDate);

    return Ok(new {
        status = true,
        message = "Locations retrieved",
        data = result.Data
    });
}
```

---

### Repository Layer'da

```csharp
public async Task<IEnumerable<Location>> GetUserLocationsAsync(int userId, DateTime startDate, DateTime endDate)
{
    // startDate va endDate allaqachon UTC formatida
    // DateTimeExtensions orqali to'g'ri konvertatsiya qilingan

    const string sql = @"
        SELECT * FROM locations
        WHERE user_id = @UserId
          AND recorded_at >= @StartDate
          AND recorded_at < @EndDate
        ORDER BY recorded_at DESC";

    var locations = await _connection.QueryAsync<Location>(sql, new {
        UserId = userId,
        StartDate = startDate, // UTC DateTime
        EndDate = endDate      // UTC DateTime
    });

    return locations;
}
```

---

## Timezone O'zgartirish

Agar loyihani **boshqa timezone**'da ishlatish kerak bo'lsa, faqat **bir joyni** o'zgartiring:

**`DateTimeExtensions.cs`** faylida:

```csharp
// Joriy: Toshkent (UTC+5)
private static readonly TimeZoneInfo ApplicationTimeZone = TimeZoneInfo.FindSystemTimeZoneById("West Asia Standard Time");
private static readonly TimeSpan ApplicationOffset = new TimeSpan(5, 0, 0);

// Masalan, Moskva (UTC+3) uchun:
// private static readonly TimeZoneInfo ApplicationTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
// private static readonly TimeSpan ApplicationOffset = new TimeSpan(3, 0, 0);

// Yoki Dubay (UTC+4) uchun:
// private static readonly TimeZoneInfo ApplicationTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time");
// private static readonly TimeSpan ApplicationOffset = new TimeSpan(4, 0, 0);
```

**Barcha kod avtomatik yangi timezone** bilan ishlaydi!

---

## Test Qilish

### Unit Test

```csharp
[Fact]
public void ParseToApplicationTime_ISO8601_ConvertsCorrectly()
{
    // Arrange
    var isoDate = "2026-01-10T09:23:23.744Z"; // UTC

    // Act
    var result = isoDate.ParseToApplicationTime();

    // Assert
    Assert.Equal(new DateTime(2026, 1, 10, 9, 23, 23, 744, DateTimeKind.Utc), result);
}

[Fact]
public void ToDateRange_ReturnsCorrectRange()
{
    // Arrange
    var date = new DateTime(2026, 1, 10);

    // Act
    var (startDate, endDate) = date.ToDateRange();

    // Assert
    // 2026-01-10 00:00:00 Toshkent = 2026-01-09 19:00:00 UTC
    Assert.Equal(new DateTime(2026, 1, 9, 19, 0, 0, DateTimeKind.Utc), startDate);
    // 2026-01-11 00:00:00 Toshkent = 2026-01-10 19:00:00 UTC
    Assert.Equal(new DateTime(2026, 1, 10, 19, 0, 0, DateTimeKind.Utc), endDate);
}
```

---

## Advantages (Afzalliklar)

✅ **Markazlashtirilgan** - Barcha timezone logikasi bir joyda
✅ **Oson o'zgartirish** - Faqat bir joyni o'zgartirsangiz yetarli
✅ **Konsistent** - Barcha joyda bir xil konvertatsiya
✅ **Xatoliklarni kamaytiradi** - Manual konvertatsiya xatolari yo'q
✅ **Testlash oson** - Extension metodlar alohida testlanishi mumkin
✅ **Kelajak uchun tayyor** - Server location o'zgarganda muammo yo'q

---

## Migration Checklist

Loyihangizni `DateTimeExtensions`'ga o'tkazish uchun:

- [x] `DateTimeExtensions.cs` yaratish
- [x] `appsettings.json`'ga timezone configuration qo'shish
- [x] `LocationService.cs`'da ishlatish
- [ ] `AuthService.cs`'da ishlatish (agar kerak bo'lsa)
- [ ] Barcha service'larda manual timezone conversion'larni topish va almashtirish
- [ ] Unit test'lar yozish
- [ ] Integration test'lar yozish
- [ ] Documentation yangilash

---

## Xulosa

`DateTimeExtensions` orqali siz:
1. **Bir martalik** - Faqat extension'ni yozing
2. **Hamma joyda** - Barcha service/controller'larda ishlating
3. **Oson boshqarish** - Kelajakda faqat bir joyni o'zgartiring
4. **Xatosiz** - Konsistent konvertatsiya

**User 12:00 dedi → Database'da 12:00 → Admin'ga 12:00 ✅**
