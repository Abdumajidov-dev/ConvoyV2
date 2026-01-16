# Time String Filter Guide (HH:MM Format)

## Overview
User locationlarini vaqt bo'yicha filterlash funksiyasi qo'shildi. Endi `"12:30"` (12 soat 30 daqiqa) formatida vaqt filterlashingiz mumkin.

## API Endpoint

```
GET /api/locations/user/{user_id}
```

## Query Parameters

| Parameter | Type | Format | Required | Description |
|-----------|------|--------|----------|-------------|
| `start_date` | DateTime | ISO 8601 | Optional | Boshlanish sanasi |
| `end_date` | DateTime | ISO 8601 | Optional | Tugash sanasi |
| `start_time` | string | "HH:MM" | Optional | Boshlanish vaqti (masalan: "09:30") |
| `end_time` | string | "HH:MM" | Optional | Tugash vaqti (masalan: "17:45") |
| `limit` | int | - | Optional | Maksimal qaytariladigan location soni |

## Time Format

**Format:** `"HH:MM"` (24-soatlik format)
- **HH:** Soat (00-23)
- **MM:** Daqiqa (00-59)

**Valid Examples:**
- `"09:30"` ✅
- `"14:45"` ✅
- `"00:00"` ✅
- `"23:59"` ✅

**Invalid Examples:**
- `"9:30"` ❌ (soat 2 raqamli bo'lishi kerak)
- `"25:30"` ❌ (soat 0-23 oralig'ida)
- `"12:60"` ❌ (daqiqa 0-59 oralig'ida)
- `"12-30"` ❌ (`:` belgisi kerak)

## Examples

### Example 1: Vaqt filtri (9:30 dan 17:45 gacha)
```bash
GET /api/locations/user/123?start_date=2026-01-01&end_date=2026-01-07&start_time=09:30&end_time=17:45
```

**Response:**
- 9:30 dan 17:45 gacha barcha locationlar qaytadi
- 9:29 va 17:46 dan keyingilar qaytmaydi

### Example 2: Faqat boshlanish vaqti (12:30 dan keyin)
```bash
GET /api/locations/user/123?start_date=2026-01-01&end_date=2026-01-07&start_time=12:30
```

**Response:**
- 12:30 va undan keyingi barcha locationlar qaytadi

### Example 3: Faqat tugash vaqti (14:15 gacha)
```bash
GET /api/locations/user/123?start_date=2026-01-01&end_date=2026-01-07&end_time=14:15
```

**Response:**
- 14:15 va undan oldingi barcha locationlar qaytadi

### Example 4: Faqat sana filtri (vaqtsiz)
```bash
GET /api/locations/user/123?start_date=2026-01-01&end_date=2026-01-07
```

**Response:**
- Barcha kunlar uchun barcha vaqtdagi locationlar qaytadi

## SQL Implementation

Daqiqa filtri quyidagi SQL logika bilan amalga oshiriladi:

```sql
-- Soat va daqiqani daqiqalarga o'girish
EXTRACT(HOUR FROM recorded_at) * 60 + EXTRACT(MINUTE FROM recorded_at)

-- Misol: 12:30 -> 12 * 60 + 30 = 750 daqiqa
-- Misol: 09:15 -> 9 * 60 + 15 = 555 daqiqa
```

**Filter logic:**
```sql
-- Ikkala time ham berilgan
WHERE time_in_minutes >= start_time_in_minutes
  AND time_in_minutes <= end_time_in_minutes

-- Faqat start time
WHERE time_in_minutes >= start_time_in_minutes

-- Faqat end time
WHERE time_in_minutes <= end_time_in_minutes
```

## Error Responses

### 1. Noto'g'ri vaqt formati
```json
{
  "status": false,
  "message": "start_time noto'g'ri formatda. Format: HH:MM (masalan: 09:30, 14:45)",
  "data": null
}
```

**Trigger qiladigan inputlar:**
- `start_time=9:30` (soat 2 raqamli emas)
- `start_time=25:30` (soat 23 dan katta)
- `start_time=12:60` (daqiqa 59 dan katta)
- `start_time=12-30` (`:` belgisi yo'q)

## Testing with cURL

```bash
# Test 1: Vaqt filtri (9:30 - 17:45)
curl -X GET "http://localhost:5084/api/locations/user/123?start_date=2026-01-01&end_date=2026-01-07&start_time=09:30&end_time=17:45" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Test 2: Faqat boshlanish vaqti (12:30 dan keyin)
curl -X GET "http://localhost:5084/api/locations/user/123?start_date=2026-01-01&end_date=2026-01-07&start_time=12:30" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Test 3: Faqat tugash vaqti (14:15 gacha)
curl -X GET "http://localhost:5084/api/locations/user/123?start_date=2026-01-01&end_date=2026-01-07&end_time=14:15" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Test 4: Faqat sana filtri (vaqtsiz)
curl -X GET "http://localhost:5084/api/locations/user/123?start_date=2026-01-01&end_date=2026-01-07" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Testing with Python

```python
import requests

# JWT token
token = "YOUR_JWT_TOKEN"
headers = {
    "Authorization": f"Bearer {token}"
}

# Test 1: Vaqt filtri (9:30 - 17:45)
response = requests.get(
    "http://localhost:5084/api/locations/user/123",
    headers=headers,
    params={
        "start_date": "2026-01-01",
        "end_date": "2026-01-07",
        "start_time": "09:30",
        "end_time": "17:45"
    }
)
print("Test 1 Response:", response.json())

# Test 2: Faqat boshlanish vaqti (12:30 dan keyin)
response = requests.get(
    "http://localhost:5084/api/locations/user/123",
    headers=headers,
    params={
        "start_date": "2026-01-01",
        "end_date": "2026-01-07",
        "start_time": "12:30"
    }
)
print("Test 2 Response:", response.json())

# Test 3: Noto'g'ri format (xato qaytarishi kerak)
response = requests.get(
    "http://localhost:5084/api/locations/user/123",
    headers=headers,
    params={
        "start_date": "2026-01-01",
        "end_date": "2026-01-07",
        "start_time": "9:30"  # Noto'g'ri format (soat 2 raqamli emas)
    }
)
print("Test 3 Response (should fail):", response.json())
```

## Implementation Details

### Files Modified

1. **LocationDtos.cs** (Convoy.Service/DTOs/)
   - `LocationQueryDto`ga `StartTime` va `EndTime` string field'lari qo'shildi (format: "HH:MM")

2. **ILocationRepository.cs** (Convoy.Data/IRepositories/)
   - `GetUserLocationsAsync` method signature yangilandi (string time parametrlari)

3. **LocationRepository.cs** (Convoy.Data/Repositories/)
   - `GetUserLocationsAsync` SQL query yangilandi
   - Time string parsing logikasi qo'shildi (`"12:30"` → `hour=12, minute=30`)
   - Soat va daqiqani daqiqalarga o'girish logikasi qo'shildi

4. **LocationService.cs** (Convoy.Service/Services/)
   - `GetUserLocationsAsync` repository call'ga time string parametrlari qo'shildi

5. **LocationController.cs** (Convoy.Api/Controllers/)
   - `start_time` va `end_time` string parametrlari qo'shildi
   - `IsValidTimeFormat()` validation method qo'shildi
   - HH:MM format validation qo'shildi

## Important Notes

1. **String Format**: Vaqt `"HH:MM"` formatida string sifatida yuboriladi
2. **Validation**: Controller'da format validation qilinadi (HH: 0-23, MM: 0-59)
3. **Parsing**: Repository'da string parse qilinib, hour va minute ajratiladi
4. **Flexible Input**: `"9:30"` yoki `"09:30"` - validation faqat range tekshiradi, leading zero majburiy emas (lekin tavsiya etiladi)
5. **Performance**: PostgreSQL EXTRACT funksiyasi indexlarda ishlamaydi, katta dataset uchun slow bo'lishi mumkin
6. **Timezone**: `recorded_at` UTC formatida saqlanadi, filter ham UTC vaqt bo'yicha qo'llaniladi
7. **Partition Pruning**: `start_date` va `end_date` partition pruning uchun majburiy (performance optimization)

## Future Improvements

1. **Timezone Support**: User timezone'sini qo'llab-quvvatlash
2. **Time Range Validation**: Start time < End time validatsiyasi qo'shish
3. **Index Optimization**: Computed column yaratish (time_in_minutes) va index qo'shish
4. **Stricter Format Validation**: Leading zero majburiy qilish (`"09:30"` ✅, `"9:30"` ❌)
