# Multiple Users Location API Guide

## Overview
Ko'p userlarning locationlarini bir vaqtning o'zida olish uchun endpoint. User ID'lar va filterlar **body** orqali yuboriladi (route'da emas).

## API Endpoint

```
POST /api/locations/multiple_users
```

## Request Format

### Headers
```
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

### Body (JSON)

```json
{
  "user_ids": [123, 456, 789],
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
| `user_ids` | array[int] | **Yes** | User ID'lar ro'yxati (masalan: `[123, 456, 789]`) |
| `start_date` | DateTime | **Yes** | Boshlanish sanasi (ISO 8601 format) |
| `end_date` | DateTime | **Yes** | Tugash sanasi (ISO 8601 format) |
| `start_time` | string | No | Boshlanish vaqti (format: "HH:MM", masalan: "09:30") |
| `end_time` | string | No | Tugash vaqti (format: "HH:MM", masalan: "17:45") |
| `limit` | int | No | Har bir user uchun maksimal location soni (default: 100) |

## Response Format

### Success Response (200 OK)

```json
{
  "status": true,
  "message": "3 ta user uchun 250 ta location olindi",
  "data": [
    {
      "id": 1001,
      "user_id": 123,
      "recorded_at": "2026-01-05T10:30:00Z",
      "latitude": 41.311151,
      "longitude": 69.279737,
      "accuracy": 10.5,
      "speed": 5.2,
      ...
    },
    {
      "id": 1002,
      "user_id": 456,
      "recorded_at": "2026-01-05T10:25:00Z",
      "latitude": 41.321234,
      "longitude": 69.289876,
      ...
    }
  ]
}
```

### Error Response (400 Bad Request)

```json
{
  "status": false,
  "message": "user_ids bo'sh bo'lmasligi kerak",
  "data": null
}
```

## Examples

### Example 1: Uch userning locationlari (sana filtri bilan)

**Request:**
```bash
POST /api/locations/multiple_users
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN

{
  "user_ids": [123, 456, 789],
  "start_date": "2026-01-01T00:00:00Z",
  "end_date": "2026-01-07T23:59:59Z"
}
```

**Response:**
- Uchta userning 2026-01-01 dan 2026-01-07 gacha barcha locationlari
- Har bir user uchun maksimal 100 ta location (default limit)

### Example 2: Vaqt filtri bilan (9:30 - 17:45)

**Request:**
```bash
POST /api/locations/multiple_users
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN

{
  "user_ids": [123, 456],
  "start_date": "2026-01-01T00:00:00Z",
  "end_date": "2026-01-07T23:59:59Z",
  "start_time": "09:30",
  "end_time": "17:45"
}
```

**Response:**
- Ikki userning faqat 9:30 dan 17:45 gacha bo'lgan locationlari

### Example 3: Custom limit (har bir user uchun 50 ta)

**Request:**
```bash
POST /api/locations/multiple_users
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN

{
  "user_ids": [123, 456, 789, 101, 102],
  "start_date": "2026-01-01T00:00:00Z",
  "end_date": "2026-01-07T23:59:59Z",
  "limit": 50
}
```

**Response:**
- Beshta userning har biri uchun maksimal 50 ta location
- Jami: maksimal 250 ta location (5 user * 50 limit)

### Example 4: Bitta user (single user query)

**Request:**
```bash
POST /api/locations/multiple_users
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN

{
  "user_ids": [123],
  "start_date": "2026-01-01T00:00:00Z",
  "end_date": "2026-01-07T23:59:59Z"
}
```

**Response:**
- Bitta userning locationlari (bu endpoint bitta user uchun ham ishlatilishi mumkin)

## Testing with cURL

```bash
# Test 1: Uchta user, sana filtri
curl -X POST "http://localhost:5084/api/locations/multiple_users" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "user_ids": [123, 456, 789],
    "start_date": "2026-01-01T00:00:00Z",
    "end_date": "2026-01-07T23:59:59Z"
  }'

# Test 2: Vaqt filtri bilan
curl -X POST "http://localhost:5084/api/locations/multiple_users" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "user_ids": [123, 456],
    "start_date": "2026-01-01T00:00:00Z",
    "end_date": "2026-01-07T23:59:59Z",
    "start_time": "09:30",
    "end_time": "17:45",
    "limit": 50
  }'
```

## Testing with Python

```python
import requests

url = "http://localhost:5084/api/locations/multiple_users"
token = "YOUR_JWT_TOKEN"
headers = {
    "Authorization": f"Bearer {token}",
    "Content-Type": "application/json"
}

# Test 1: Uchta user, sana filtri
data = {
    "user_ids": [123, 456, 789],
    "start_date": "2026-01-01T00:00:00Z",
    "end_date": "2026-01-07T23:59:59Z"
}
response = requests.post(url, json=data, headers=headers)
print("Test 1 Response:", response.json())

# Test 2: Vaqt filtri bilan
data = {
    "user_ids": [123, 456],
    "start_date": "2026-01-01T00:00:00Z",
    "end_date": "2026-01-07T23:59:59Z",
    "start_time": "09:30",
    "end_time": "17:45",
    "limit": 50
}
response = requests.post(url, json=data, headers=headers)
print("Test 2 Response:", response.json())

# Test 3: Validation error (bo'sh user_ids)
data = {
    "user_ids": [],
    "start_date": "2026-01-01T00:00:00Z",
    "end_date": "2026-01-07T23:59:59Z"
}
response = requests.post(url, json=data, headers=headers)
print("Test 3 Response (should fail):", response.json())
```

## SQL Implementation

Ushbu endpoint quyidagi SQL query'dan foydalanadi:

```sql
-- ROW_NUMBER() bilan har bir user uchun limitni qo'llash
SELECT * FROM (
    SELECT
        *,
        ROW_NUMBER() OVER (PARTITION BY user_id ORDER BY recorded_at DESC) as row_num
    FROM locations
    WHERE user_id = ANY(ARRAY[123, 456, 789])
        AND recorded_at >= '2026-01-01'
        AND recorded_at < '2026-01-07'
        -- Vaqt filtri (agar berilgan bo'lsa)
        AND EXTRACT(HOUR FROM recorded_at) * 60 + EXTRACT(MINUTE FROM recorded_at) >= 570  -- 9:30
        AND EXTRACT(HOUR FROM recorded_at) * 60 + EXTRACT(MINUTE FROM recorded_at) <= 1065  -- 17:45
) AS ranked_locations
WHERE row_num <= 100  -- Har bir user uchun limit
ORDER BY user_id, recorded_at DESC
```

**Key Features:**
- `user_id = ANY(ARRAY[...])`: PostgreSQL array operator (ko'p userlar uchun)
- `ROW_NUMBER() OVER (PARTITION BY user_id ...)`: Har bir user uchun alohida ranking
- `row_num <= limit`: Har bir user uchun maksimal location soni

## Validation Rules

1. **user_ids**: Bo'sh bo'lmasligi kerak (kamida 1 ta user ID)
2. **start_date**: Majburiy (required)
3. **end_date**: Majburiy (required)
4. **start_time**: Optional, format: "HH:MM" (HH: 0-23, MM: 0-59)
5. **end_time**: Optional, format: "HH:MM" (HH: 0-23, MM: 0-59)
6. **limit**: Optional, default: 100 (har bir user uchun)

## Error Responses

### 1. Bo'sh user_ids
```json
{
  "status": false,
  "message": "user_ids bo'sh bo'lmasligi kerak",
  "data": null
}
```

### 2. Start/End date yo'q
```json
{
  "status": false,
  "message": "start_date va end_date majburiy",
  "data": null
}
```

### 3. Noto'g'ri vaqt formati
```json
{
  "status": false,
  "message": "start_time noto'g'ri formatda. Format: HH:MM (masalan: 09:30, 14:45)",
  "data": null
}
```

## Performance Considerations

1. **Limit per user**: `limit` parametri har bir user uchun qo'llaniladi (jami emas)
   - Agar 5 ta user va limit=100 bo'lsa → maksimal 500 ta location qaytadi
2. **ROW_NUMBER()**: PostgreSQL'da efficient, lekin katta dataset'da slow bo'lishi mumkin
3. **Partition Pruning**: `start_date` va `end_date` partitioned table uchun optimal
4. **Time Filter**: EXTRACT funksiyasi index ishlatmaydi (performance impact)

## Important Notes

1. **POST method**: Body orqali user_ids yuboriladi (route'da emas)
2. **Array format**: `user_ids` JSON array sifatida: `[123, 456, 789]`
3. **Limit behavior**: Har bir user uchun alohida limit (global limit emas)
4. **Ordering**: Locationlar `user_id` va `recorded_at DESC` bo'yicha tartiblanadi
5. **Timezone**: Barcha vaqtlar UTC formatida (ISO 8601)

## Comparison with Single User Endpoint

| Feature | Single User (`GET /user/{id}`) | Multiple Users (`POST /multiple_users`) |
|---------|-------------------------------|----------------------------------------|
| HTTP Method | GET | POST |
| User ID | Route parameter | Body array |
| Filters | Query parameters | Body fields |
| Limit behavior | Total limit | Per-user limit |
| Max users | 1 | Unlimited (array) |

## Use Cases

1. **Dashboard**: Barcha userlarning real-time locationlarini ko'rsatish
2. **Fleet Management**: Bir nechta haydovchilarni kuzatish
3. **Report Generation**: Ko'p userlar uchun location hisobotlari
4. **Bulk Export**: Bir nechta userning ma'lumotlarini export qilish
5. **Analytics**: Ko'p userlarning harakatini tahlil qilish

## Future Improvements

1. **Grouping by user**: Response'ni user_id bo'yicha group qilish
2. **Pagination**: Katta dataset uchun pagination qo'shish
3. **Total limit**: Global maksimal location soni
4. **User validation**: User ID'larning mavjudligini tekshirish
5. **Caching**: Tez-tez so'raladigan query'larni cache qilish
