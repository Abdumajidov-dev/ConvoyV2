# Daily Location API Guide - Bir Kunlik Locationlar

## Overview
Location API'lari yangilandi. Endi **faqat bir kunlik** locationlarni olish mumkin. `start_date` va `end_date` o'rniga bitta `date` field ishlatiladi.

## Important Changes

### ❌ Eski Format (O'chirildi)
```json
{
  "start_date": "2026-01-01T00:00:00Z",
  "end_date": "2026-01-07T23:59:59Z"
}
```

### ✅ Yangi Format (Hozirgi)
```json
{
  "date": "2026-01-07"
}
```

**ESLATMA:** Faqat **bir kunlik** locationlar qaytadi. Ko'p kunlik query qilish imkoni yo'q.

---

## 1. Bitta User Locationlari

### Endpoint
```
POST /api/locations/user/{user_id}
```

### Request Body

```json
{
  "date": "2026-01-07",
  "start_time": "09:30",
  "end_time": "17:45"
}
```

### Parameters

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `date` | DateTime | **Yes** | Sana (faqat bitta kun, format: "YYYY-MM-DD") |
| `start_time` | string | No | Kunning boshlanish vaqti (format: "HH:MM") |
| `end_time` | string | No | Kunning tugash vaqti (format: "HH:MM") |

### Examples

#### Example 1: Butun kunlik locationlar
```bash
POST /api/locations/user/123
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN

{
  "date": "2026-01-07"
}
```

**Natija:** 2026-01-07 kunining 00:00:00 dan 23:59:59 gacha barcha locationlari

#### Example 2: Vaqt filtri bilan
```bash
POST /api/locations/user/123
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN

{
  "date": "2026-01-07",
  "start_time": "09:30",
  "end_time": "17:45"
}
```

**Natija:** 2026-01-07 kunining faqat 9:30 dan 17:45 gacha locationlari

---

## 2. Ko'p Userlarning Locationlari

### Endpoint
```
POST /api/locations/multiple_users
```

### Request Body

```json
{
  "user_ids": [123, 456, 789],
  "date": "2026-01-07",
  "start_time": "09:30",
  "end_time": "17:45",
  "limit": 100
}
```

### Parameters

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `user_ids` | array[int] | **Yes** | User ID'lar ro'yxati |
| `date` | DateTime | **Yes** | Sana (faqat bitta kun, format: "YYYY-MM-DD") |
| `start_time` | string | No | Kunning boshlanish vaqti (format: "HH:MM") |
| `end_time` | string | No | Kunning tugash vaqti (format: "HH:MM") |
| `limit` | int | No | Har bir user uchun maksimal location soni (default: 100) |

### Examples

#### Example 1: Uchta user, butun kun
```bash
POST /api/locations/multiple_users
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN

{
  "user_ids": [123, 456, 789],
  "date": "2026-01-07"
}
```

**Natija:** Uchta userning 2026-01-07 kunidagi barcha locationlari

#### Example 2: Vaqt filtri va limit bilan
```bash
POST /api/locations/multiple_users
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN

{
  "user_ids": [123, 456, 789],
  "date": "2026-01-07",
  "start_time": "09:00",
  "end_time": "18:00",
  "limit": 50
}
```

**Natija:** Uchta userning ish vaqtidagi (9:00-18:00) locationlari, har biri uchun maksimal 50 ta

---

## Testing with cURL

### Single User
```bash
# Butun kunlik
curl -X POST "http://localhost:5084/api/locations/user/123" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"date": "2026-01-07"}'

# Vaqt filtri bilan
curl -X POST "http://localhost:5084/api/locations/user/123" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-01-07",
    "start_time": "09:30",
    "end_time": "17:45"
  }'
```

### Multiple Users
```bash
# Butun kunlik
curl -X POST "http://localhost:5084/api/locations/multiple_users" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "user_ids": [123, 456, 789],
    "date": "2026-01-07"
  }'

# Vaqt filtri va limit bilan
curl -X POST "http://localhost:5084/api/locations/multiple_users" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "user_ids": [123, 456, 789],
    "date": "2026-01-07",
    "start_time": "09:00",
    "end_time": "18:00",
    "limit": 50
  }'
```

---

## Testing with Python

```python
import requests
from datetime import datetime

url_single = "http://localhost:5084/api/locations/user/123"
url_multiple = "http://localhost:5084/api/locations/multiple_users"
token = "YOUR_JWT_TOKEN"
headers = {
    "Authorization": f"Bearer {token}",
    "Content-Type": "application/json"
}

# Test 1: Single user, butun kun
data = {
    "date": "2026-01-07"
}
response = requests.post(url_single, json=data, headers=headers)
print("Single User (Full Day):", response.json())

# Test 2: Single user, vaqt filtri
data = {
    "date": "2026-01-07",
    "start_time": "09:30",
    "end_time": "17:45"
}
response = requests.post(url_single, json=data, headers=headers)
print("Single User (Time Filter):", response.json())

# Test 3: Multiple users, butun kun
data = {
    "user_ids": [123, 456, 789],
    "date": "2026-01-07"
}
response = requests.post(url_multiple, json=data, headers=headers)
print("Multiple Users (Full Day):", response.json())

# Test 4: Multiple users, vaqt filtri va limit
data = {
    "user_ids": [123, 456, 789],
    "date": "2026-01-07",
    "start_time": "09:00",
    "end_time": "18:00",
    "limit": 50
}
response = requests.post(url_multiple, json=data, headers=headers)
print("Multiple Users (Time Filter + Limit):", response.json())

# Test 5: Bugungi kun (dynamic)
today = datetime.now().strftime("%Y-%m-%d")
data = {
    "user_ids": [123, 456],
    "date": today
}
response = requests.post(url_multiple, json=data, headers=headers)
print(f"Today's Locations ({today}):", response.json())
```

---

## Response Format

### Success Response (200 OK)

```json
{
  "status": true,
  "message": "2026-01-07 uchun 45 ta location olindi",
  "data": [
    {
      "id": 1001,
      "user_id": 123,
      "recorded_at": "2026-01-07T10:30:00Z",
      "latitude": 41.311151,
      "longitude": 69.279737,
      "accuracy": 10.5,
      "speed": 5.2,
      ...
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

---

## Date Format

### ✅ Valid Formats
- `"2026-01-07"` - ISO 8601 sana
- `"2026-01-07T00:00:00Z"` - ISO 8601 sana + vaqt (vaqt ignore qilinadi)
- `"2026-01-07T10:30:00"` - Local datetime (faqat sana olinadi)

### How It Works
```csharp
// Backend logic:
var startDate = query.Date.Date;  // 2026-01-07 00:00:00
var endDate = startDate.AddDays(1);  // 2026-01-08 00:00:00

// SQL query:
WHERE recorded_at >= '2026-01-07 00:00:00'
  AND recorded_at < '2026-01-08 00:00:00'
```

**Natija:** Faqat 2026-01-07 kunidagi locationlar

---

## Time Filter Format

### Valid Time Format: `"HH:MM"`
- `"09:30"` ✅
- `"14:45"` ✅
- `"00:00"` ✅
- `"23:59"` ✅

### Invalid Formats
- `"9:30"` ❌ (soat 2 raqamli bo'lishi kerak)
- `"25:30"` ❌ (soat 0-23 oralig'ida)
- `"12:60"` ❌ (daqiqa 0-59 oralig'ida)

---

## Use Cases

### 1. Bugungi Kun Locationlari
```python
from datetime import datetime

today = datetime.now().strftime("%Y-%m-%d")
data = {
    "user_ids": [123, 456],
    "date": today
}
```

### 2. Kecha Locationlari
```python
from datetime import datetime, timedelta

yesterday = (datetime.now() - timedelta(days=1)).strftime("%Y-%m-%d")
data = {
    "user_ids": [123],
    "date": yesterday
}
```

### 3. Ish Vaqti Locationlari
```python
data = {
    "user_ids": [123, 456, 789],
    "date": "2026-01-07",
    "start_time": "09:00",
    "end_time": "18:00"
}
```

### 4. Tushlik Vaqti Locationlari
```python
data = {
    "user_ids": [123],
    "date": "2026-01-07",
    "start_time": "12:00",
    "end_time": "13:00"
}
```

---

## Migration Guide

### Eski Code (Multi-day Range)
```python
# ❌ Eski - endi ishlamaydi
data = {
    "start_date": "2026-01-01",
    "end_date": "2026-01-07"
}
```

### Yangi Code (Single Day)
```python
# ✅ Yangi - har bir kun uchun alohida query
from datetime import datetime, timedelta

start = datetime(2026, 1, 1)
end = datetime(2026, 1, 7)

all_locations = []
current = start
while current <= end:
    date_str = current.strftime("%Y-%m-%d")
    data = {
        "user_ids": [123],
        "date": date_str
    }
    response = requests.post(url, json=data, headers=headers)
    all_locations.extend(response.json()["data"])
    current += timedelta(days=1)
```

---

## Important Notes

1. **Bir kun limit**: Faqat bitta kunlik locationlar qaytadi
2. **Date majburiy**: `date` field har doim berilishi kerak
3. **Time optional**: `start_time` va `end_time` ixtiyoriy
4. **Limit per user**: Ko'p user uchun limit har biriga alohida
5. **Timezone**: Barcha vaqtlar UTC (server timezone)
6. **Date-only**: Sana berilganda faqat sana qismi ishlatiladi (vaqt ignore)

---

## Performance Tips

1. **Partition pruning**: Bir kunlik query partitioned table uchun optimal
2. **Index usage**: Date filter index'lardan foydalanadi
3. **Batch queries**: Ko'p kunlar uchun parallel query'lar ishlatish mumkin
4. **Limit wisely**: Har bir user uchun kerakli miqdorda limit qo'ying
5. **Time filter**: Faqat kerakli vaqt oralig'ini filter qiling

---

## Common Errors

### 1. Noto'g'ri vaqt formati
```json
{
  "date": "2026-01-07",
  "start_time": "25:30"
}
```
**Error:** `start_time noto'g'ri formatda. Format: HH:MM`

### 2. User IDs bo'sh
```json
{
  "user_ids": [],
  "date": "2026-01-07"
}
```
**Error:** `user_ids bo'sh bo'lmasligi kerak`

---

## Future Improvements

1. **Week view**: Bir haftalik summary
2. **Month view**: Oylik statistika
3. **Date range**: Ko'p kunlik query support (agar kerak bo'lsa)
4. **Caching**: Tez-tez so'raladigan kunlarni cache qilish
5. **Aggregation**: Kunlik summary (total distance, etc.)
