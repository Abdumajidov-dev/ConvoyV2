# Hour Filter Feature Guide

## Overview

Soat bo'yicha filter funksiyasi location'larni ma'lum soat oralig'ida olish imkonini beradi. Bu ish vaqti, tunda yoki boshqa soatlar bo'yicha ma'lumotlarni tahlil qilish uchun foydali.

## API Endpoint

```
GET /api/locations/user/{user_id}?start_date=...&end_date=...&start_hour=9&end_hour=18
```

## Query Parameters

| Parameter | Type | Required | Range | Description |
|-----------|------|----------|-------|-------------|
| `user_id` | int | Yes | - | User ID (route parameter) |
| `start_date` | DateTime | No | - | Boshlanish sanasi (ISO 8601 format) |
| `end_date` | DateTime | No | - | Tugash sanasi (ISO 8601 format) |
| `start_hour` | int | No | 0-23 | Minimal soat (inclusive) |
| `end_hour` | int | No | 0-23 | Maksimal soat (inclusive) |
| `limit` | int | No | - | Maksimal natijalar soni |

## Examples

### 1. Barcha location'lar (filter yo'q)

```http
GET /api/locations/user/1?start_date=2026-01-01T00:00:00Z&end_date=2026-01-07T23:59:59Z
Authorization: Bearer {token}
```

**Response:**
```json
{
  "status": true,
  "message": "1 ta location topildi",
  "data": [
    {
      "id": 123,
      "user_id": 1,
      "recorded_at": "2026-01-06T14:30:00Z",
      "latitude": 41.311151,
      "longitude": 69.279737,
      ...
    }
  ]
}
```

### 2. Ish vaqti (9:00 - 18:00)

```http
GET /api/locations/user/1?start_date=2026-01-01T00:00:00Z&end_date=2026-01-07T23:59:59Z&start_hour=9&end_hour=18
Authorization: Bearer {token}
```

Faqat 9:00 dan 18:00 gacha yozilgan location'lar qaytariladi.

### 3. Tunda (22:00 - 6:00) - Ikki query

Tungi soatlarni olish uchun ikki alohida query kerak (0-6 va 22-23):

**Query 1: Kechki tun (22:00 - 23:59)**
```http
GET /api/locations/user/1?start_date=2026-01-01T00:00:00Z&end_date=2026-01-07T23:59:59Z&start_hour=22
```

**Query 2: Ertalabki tun (00:00 - 06:59)**
```http
GET /api/locations/user/1?start_date=2026-01-01T00:00:00Z&end_date=2026-01-07T23:59:59Z&end_hour=6
```

### 4. Faqat start_hour (22:00 dan keyin)

```http
GET /api/locations/user/1?start_date=2026-01-01T00:00:00Z&end_date=2026-01-07T23:59:59Z&start_hour=22
Authorization: Bearer {token}
```

22:00 va undan keyingi barcha soatlar (22:00, 23:00).

### 5. Faqat end_hour (6:00 dan oldin)

```http
GET /api/locations/user/1?start_date=2026-01-01T00:00:00Z&end_date=2026-01-07T23:59:59Z&end_hour=6
Authorization: Bearer {token}
```

6:00 va undan oldingi barcha soatlar (0:00, 1:00, ..., 6:00).

## Validation

### Valid Hours (0-23)

- `start_hour` va `end_hour` 0 dan 23 gacha bo'lishi kerak
- Noto'g'ri qiymatlar 400 Bad Request qaytaradi

**Invalid Example:**
```http
GET /api/locations/user/1?start_hour=25
```

**Error Response:**
```json
{
  "status": false,
  "message": "start_hour 0-23 oralig'ida bo'lishi kerak",
  "data": null
}
```

## Implementation Details

### Backend (SQL)

PostgreSQL `EXTRACT(HOUR FROM recorded_at)` funksiyasidan foydalanadi:

```sql
SELECT * FROM locations
WHERE user_id = @UserId
  AND recorded_at >= @StartDate
  AND recorded_at < @EndDate
  AND EXTRACT(HOUR FROM recorded_at) >= @StartHour
  AND EXTRACT(HOUR FROM recorded_at) <= @EndHour
ORDER BY recorded_at DESC
```

### DTO Structure

**LocationQueryDto:**
```csharp
public class LocationQueryDto
{
    public int UserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? StartHour { get; set; }  // 0-23 (optional)
    public int? EndHour { get; set; }    // 0-23 (optional)
    public int? Limit { get; set; } = 100;
}
```

## Use Cases

### 1. Ish vaqtini tekshirish

Xodim ish vaqtida (9:00-18:00) qayerda bo'lganini ko'rish:

```
start_hour=9&end_hour=18
```

### 2. Tunda harakatni kuzatish

Tunda (22:00-6:00) location'larni tekshirish (2 query):

```
Query 1: start_hour=22
Query 2: end_hour=6
```

### 3. Tushlik vaqtini aniqlash

Tushlik (12:00-14:00) vaqtidagi location'lar:

```
start_hour=12&end_hour=14
```

### 4. Ertalabki va kechki harakatni tahlil qilish

**Ertalab (6:00-9:00):**
```
start_hour=6&end_hour=9
```

**Kechqurun (18:00-22:00):**
```
start_hour=18&end_hour=22
```

## Testing

Test skripti mavjud: `test_hour_filter.py`

**Run test:**
```bash
python test_hour_filter.py
```

Test quyidagi scenariylarni tekshiradi:
1. Filter yo'q (barcha location'lar)
2. Ish vaqti (9:00-18:00)
3. Kechki tun (22:00 dan keyin)
4. Ertalabki tun (6:00 gacha)
5. Invalid hour range (validation test)

## Notes

- **Optional parameters**: `start_hour` va `end_hour` ixtiyoriy
- **Inclusive range**: Start va end soatlar ham qo'shiladi (9:00 va 18:00 ham kiritiladi)
- **UTC timezone**: Barcha vaqtlar UTC da saqlanadi
- **Performance**: `EXTRACT(HOUR)` funksiyasi index'siz bajariladi, lekin partition pruning `recorded_at` bo'yicha ishlaydi
- **Midnight crossing**: Tun vaqti uchun (22:00-6:00) ikki alohida query kerak

## Database Performance

PostgreSQL partitioning tufayli `recorded_at` filter juda tez ishlaydi:

- ✅ `recorded_at >= @StartDate AND recorded_at < @EndDate` → Partition pruning
- ⚠️ `EXTRACT(HOUR FROM recorded_at)` → Sequential scan (but on filtered partitions)

Tez natija olish uchun `start_date` va `end_date` ni har doim belgilash tavsiya etiladi.

## Related Documentation

- **API Response Format**: `API_RESPONSE_FORMAT.md`
- **Snake Case Guide**: `SNAKE_CASE_API_GUIDE.md`
- **Service Result Pattern**: `SERVICE_RESULT_PATTERN.md`
