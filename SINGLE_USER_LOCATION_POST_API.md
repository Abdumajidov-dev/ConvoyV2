# Single User Location API - POST with Body Filters

## Overview
Bitta userning locationlarini olish uchun yangi endpoint. **User ID** route'da, **filterlar** body'da yuboriladi.

## Yangi Endpoint (Tavsiya etiladi)

```
POST /api/locations/user/{user_id}/query
```

## Eski Endpoint (Deprecated)

```
GET /api/locations/user/{user_id}?start_date=...&end_date=...
```

**ESLATMA:** Eski GET endpoint hali ham ishlaydi (backward compatibility), lekin yangi POST endpoint'ini ishlatish tavsiya etiladi.

## Request Format

### Headers
```
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

### Route Parameter
- `user_id`: User ID (integer) - masalan: `123`

### Body (JSON)

```json
{
  "start_date": "2026-01-01T00:00:00Z",
  "end_date": "2026-01-07T23:59:59Z",
  "start_time": "09:30",
  "end_time": "17:45",
  "limit": 100
}
```

## Request Parameters

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `start_date` | DateTime | No | Boshlanish sanasi (ISO 8601 format) |
| `end_date` | DateTime | No | Tugash sanasi (ISO 8601 format) |
| `start_time` | string | No | Boshlanish vaqti (format: "HH:MM", masalan: "09:30") |
| `end_time` | string | No | Tugash vaqti (format: "HH:MM", masalan: "17:45") |
| `limit` | int | No | Maksimal location soni (default: 100) |

**ESLATMA:**
- Agar `start_date` va `end_date` berilmasa, oxirgi `limit` ta location qaytadi
- `start_time` va `end_time` faqat `start_date` va `end_date` bilan birgalikda ishlaydi

## Response Format

### Success Response (200 OK)

```json
{
  "status": true,
  "message": "Location ma'lumotlari muvaffaqiyatli olindi",
  "data": [
    {
      "id": 1001,
      "user_id": 123,
      "recorded_at": "2026-01-05T10:30:00Z",
      "latitude": 41.311151,
      "longitude": 69.279737,
      "accuracy": 10.5,
      "speed": 5.2,
      "heading": 45.0,
      "altitude": 350.5,
      "activity_type": "still",
      "activity_confidence": 95,
      "is_moving": false,
      "battery_level": 85,
      "is_charging": false,
      "distance_from_previous": 125.5,
      "created_at": "2026-01-05T10:30:00Z"
    }
  ]
}
```

### Error Response (400 Bad Request)

```json
{
  "status": false,
  "message": "start_time noto'g'ri formatda. Format: HH:MM (masalan: 09:30, 14:45)",
  "data": null
}
```

## Examples

### Example 1: Sana oralig'ida barcha locationlar

**Request:**
```bash
POST /api/locations/user/123/query
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN

{
  "start_date": "2026-01-01T00:00:00Z",
  "end_date": "2026-01-07T23:59:59Z"
}
```

**Response:**
- User 123ning 2026-01-01 dan 2026-01-07 gacha barcha locationlari
- Maksimal 100 ta location (default limit)

### Example 2: Vaqt filtri bilan (9:30 - 17:45)

**Request:**
```bash
POST /api/locations/user/123/query
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN

{
  "start_date": "2026-01-01T00:00:00Z",
  "end_date": "2026-01-07T23:59:59Z",
  "start_time": "09:30",
  "end_time": "17:45"
}
```

**Response:**
- Faqat 9:30 dan 17:45 gacha bo'lgan locationlar

### Example 3: Custom limit (50 ta location)

**Request:**
```bash
POST /api/locations/user/123/query
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN

{
  "start_date": "2026-01-01T00:00:00Z",
  "end_date": "2026-01-07T23:59:59Z",
  "limit": 50
}
```

**Response:**
- Maksimal 50 ta location

### Example 4: Oxirgi 100 ta location (sana filtersiz)

**Request:**
```bash
POST /api/locations/user/123/query
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN

{
  "limit": 100
}
```

**Response:**
- Eng so'nggi 100 ta location (sana qat'iy nazar)

### Example 5: Faqat bugungi kun

**Request:**
```bash
POST /api/locations/user/123/query
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN

{
  "start_date": "2026-01-07T00:00:00Z",
  "end_date": "2026-01-08T00:00:00Z"
}
```

**Response:**
- Faqat 2026-01-07 kundagi locationlar

## Testing with cURL

```bash
# Test 1: Sana oralig'ida
curl -X POST "http://localhost:5084/api/locations/user/123/query" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "start_date": "2026-01-01T00:00:00Z",
    "end_date": "2026-01-07T23:59:59Z"
  }'

# Test 2: Vaqt filtri bilan
curl -X POST "http://localhost:5084/api/locations/user/123/query" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "start_date": "2026-01-01T00:00:00Z",
    "end_date": "2026-01-07T23:59:59Z",
    "start_time": "09:30",
    "end_time": "17:45",
    "limit": 50
  }'

# Test 3: Oxirgi 100 ta location
curl -X POST "http://localhost:5084/api/locations/user/123/query" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "limit": 100
  }'
```

## Testing with Python

```python
import requests

url = "http://localhost:5084/api/locations/user/123/query"
token = "YOUR_JWT_TOKEN"
headers = {
    "Authorization": f"Bearer {token}",
    "Content-Type": "application/json"
}

# Test 1: Sana oralig'ida
data = {
    "start_date": "2026-01-01T00:00:00Z",
    "end_date": "2026-01-07T23:59:59Z"
}
response = requests.post(url, json=data, headers=headers)
print("Test 1 Response:", response.json())

# Test 2: Vaqt filtri bilan
data = {
    "start_date": "2026-01-01T00:00:00Z",
    "end_date": "2026-01-07T23:59:59Z",
    "start_time": "09:30",
    "end_time": "17:45",
    "limit": 50
}
response = requests.post(url, json=data, headers=headers)
print("Test 2 Response:", response.json())

# Test 3: Oxirgi 100 ta location
data = {
    "limit": 100
}
response = requests.post(url, json=data, headers=headers)
print("Test 3 Response:", response.json())

# Test 4: Validation error (noto'g'ri vaqt formati)
data = {
    "start_date": "2026-01-01T00:00:00Z",
    "end_date": "2026-01-07T23:59:59Z",
    "start_time": "25:30"  # Noto'g'ri format
}
response = requests.post(url, json=data, headers=headers)
print("Test 4 Response (should fail):", response.json())
```

## Comparison: GET vs POST

### GET Endpoint (Eski - Deprecated)
```
GET /api/locations/user/123?start_date=2026-01-01&end_date=2026-01-07&start_time=09:30&end_time=17:45&limit=100
```

### POST Endpoint (Yangi - Tavsiya etiladi)
```
POST /api/locations/user/123/query

Body:
{
  "start_date": "2026-01-01T00:00:00Z",
  "end_date": "2026-01-07T23:59:59Z",
  "start_time": "09:30",
  "end_time": "17:45",
  "limit": 100
}
```

**Afzalliklari:**
- ✅ Body orqali murakkab filterlar
- ✅ JSON format (clean va oson parse qilish)
- ✅ URL length limit yo'q
- ✅ Encryption qo'llab-quvvatlaydi (body shifrlash mumkin)
- ✅ Kelajakda qo'shimcha filterlar qo'shish oson

## Validation Rules

1. **start_time**: Optional, format: "HH:MM" (HH: 0-23, MM: 0-59)
2. **end_time**: Optional, format: "HH:MM" (HH: 0-23, MM: 0-59)
3. **start_date & end_date**: Optional, lekin ikkalasi ham berilishi kerak (agar biri berilsa)
4. **limit**: Optional, default: 100

## Error Responses

### 1. Noto'g'ri vaqt formati
```json
{
  "status": false,
  "message": "start_time noto'g'ri formatda. Format: HH:MM (masalan: 09:30, 14:45)",
  "data": null
}
```

**Trigger:**
- `start_time: "25:30"` (soat 23 dan katta)
- `start_time: "12:60"` (daqiqa 59 dan katta)
- `start_time: "12-30"` (`:` belgisi yo'q)

## Important Notes

1. **User ID location**: Route'da (`/user/{user_id}/query`)
2. **Filters location**: Body'da (JSON)
3. **Backward compatibility**: Eski GET endpoint hali ham ishlaydi
4. **Default behavior**: Agar sana berilmasa, oxirgi `limit` ta location qaytadi
5. **Time filter requirement**: `start_time` va `end_time` faqat `start_date` va `end_date` bilan ishlaydi
6. **Timezone**: Barcha vaqtlar UTC formatida (ISO 8601)

## Migration Guide

### Eski usuldan (GET) yangi usulga (POST) o'tish

**Eski:**
```javascript
// Flutter/Dart
final response = await http.get(
  Uri.parse('http://api.example.com/api/locations/user/123?start_date=2026-01-01&end_date=2026-01-07'),
  headers: {'Authorization': 'Bearer $token'},
);
```

**Yangi:**
```javascript
// Flutter/Dart
final response = await http.post(
  Uri.parse('http://api.example.com/api/locations/user/123/query'),
  headers: {
    'Authorization': 'Bearer $token',
    'Content-Type': 'application/json',
  },
  body: jsonEncode({
    'start_date': '2026-01-01T00:00:00Z',
    'end_date': '2026-01-07T23:59:59Z',
  }),
);
```

## Use Cases

1. **Dashboard**: Userning kunlik harakati
2. **Reports**: Vaqt oralig'idagi hisobotlar
3. **Analytics**: Userning harakatini tahlil qilish
4. **History**: Oxirgi N ta locationni ko'rish
5. **Working Hours**: Faqat ish vaqtidagi locationlar (9:00-18:00)

## Future Improvements

1. **Geofence filter**: Geografik hudud bo'yicha filterlash
2. **Activity filter**: Faqat ma'lum activity type'lari
3. **Speed filter**: Tezlik bo'yicha filterlash
4. **Distance filter**: Masofa bo'yicha filterlash
5. **Pagination**: Katta dataset uchun page-based pagination
