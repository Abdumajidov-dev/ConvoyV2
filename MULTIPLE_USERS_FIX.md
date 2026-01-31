# Multiple Users Locations Fix

## Muammo

`POST /api/locations/multiple_users` endpoint bo'sh array qaytaryapti, lekin database'da locationlar bor.

## Sabab

**UserService.GetByIdAsync()** method `users.id` bo'yicha qidiryapti, lekin LocationService `user_id` (PHP worker_id) yuborayapti.

### Log evidence:
```
warn: Convoy.Service.Services.LocationService[0]
      User not found: UserId=5277

SELECT u.* FROM users AS u WHERE u.id = @__id_0  // ❌ users.id ishlatyapti
```

Lekin kerak:
```sql
SELECT * FROM users WHERE u.user_id = 5277  // ✅ users.user_id kerak
```

## Yechim

### 1. IUserService interface'ga yangi method qo'shildi:

```csharp
/// <summary>
/// PHP API worker_id (user_id) bo'yicha user DTO'sini olish
/// Multiple users locations uchun kerak
/// </summary>
Task<UserResponseDto?> GetByUserIdDtoAsync(int userId);
```

### 2. UserService'da implementation:

```csharp
public async Task<UserResponseDto?> GetByUserIdDtoAsync(int userId)
{
    var user = await _context.Users
        .FirstOrDefaultAsync(u => u.UserId == userId);  // ✅ u.UserId ishlatildi

    if (user == null)
    {
        return null;
    }

    return _mapper.Map<UserResponseDto>(user);
}
```

### 3. LocationService'da chaqiruv o'zgartirildi:

**Oldin:**
```csharp
var user = await userService.GetByIdAsync(userId);  // ❌ Noto'g'ri method
```

**Endi:**
```csharp
var user = await userService.GetByUserIdDtoAsync(userId);  // ✅ To'g'ri method
```

## Test

```json
POST /api/locations/multiple_users
Content-Type: application/json

{
  "user_ids": [5277, 5475],
  "branch_guid": null,
  "date": "2026-01-31",
  "start_hour": null,
  "end_hour": null,
  "limit": 100
}
```

**Kutilayotgan response:**
```json
{
  "status": true,
  "message": "2 ta user (user_ids) uchun 2026-01-31 kunida 43 ta location olindi",
  "data": [
    {
      "id": 1,
      "user_id": 5277,
      "name": "...",
      "locations": [...]
    },
    {
      "id": 2,
      "user_id": 5475,
      "name": "...",
      "locations": [...]
    }
  ]
}
```

## O'zgartirilgan fayllar

1. `Convoy.Service/Interfaces/IUserService.cs` - Yangi method signature
2. `Convoy.Service/Services/UserService.cs` - Method implementation
3. `Convoy.Service/Services/LocationService.cs` - Method chaqiruvi o'zgartirildi

## Build va Run

```bash
dotnet build
dotnet run --project Convoy.Api
```

Yoki Visual Studio'da F5 bosing.
