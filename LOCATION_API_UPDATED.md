# Location API - Updated (JWT Token Based)

## Overview

Location API yangilandi:
- ✅ **JWT Token orqali authentication** - barcha endpoint'lar `[Authorize]` bilan himoyalangan
- ✅ **User ID tokendan olinadi** - body'da user_id yuborishga hojat yo'q
- ✅ **Faqat latitude va longitude required** - qolgan barcha field'lar optional
- ✅ **RecordedAt avtomatik** - agar Flutter yuborimasa, server hozirgi vaqtni yozadi

---

## Authentication

Barcha endpoint'lar **JWT token** talab qiladi:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

Token'da `user_id` claim bo'lishi kerak. Token olish uchun:

```bash
# 1. Phone number verify
POST /api/auth/verify_number

# 2. OTP request
POST /api/auth/send_otp

# 3. OTP verify va token olish
POST /api/auth/verify_otp
```

---

## POST /api/locations

Location(lar) yaratish - user_id JWT tokendan avtomatik olinadi.

### Request Headers

```
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

### Request Body

```json
{
  "locations": [
    {
      // REQUIRED FIELDS (faqat latitude va longitude)
      "latitude": 41.311151,
      "longitude": 69.279737,

      // OPTIONAL - Core location properties
      "recorded_at": "2025-12-25T10:30:00Z",  // Agar yo'q bo'lsa - server hozirgi vaqtni yozadi
      "accuracy": 10.5,
      "speed": 5.2,
      "heading": 180.0,
      "altitude": 350.0,

      // OPTIONAL - Flutter Background Geolocation Extended Coords
      "ellipsoidal_altitude": 352.5,
      "heading_accuracy": 5.0,
      "speed_accuracy": 1.5,
      "altitude_accuracy": 3.0,
      "floor": 2,

      // OPTIONAL - Activity
      "activity_type": "walking",
      "activity_confidence": 85,
      "is_moving": true,

      // OPTIONAL - Battery
      "battery_level": 75,
      "is_charging": false,

      // OPTIONAL - Flutter Background Geolocation Metadata
      "timestamp": "2025-12-25T10:30:00Z",
      "age": 500,
      "event": "motionchange",
      "mock": false,
      "sample": false,
      "odometer": 1234.56,
      "uuid": "550e8400-e29b-41d4-a716-446655440000",
      "extras": "{\"custom_field\":\"value\"}"
    }
  ]
}
```

### Minimal Request (faqat required field'lar)

```json
{
  "locations": [
    {
      "latitude": 41.311151,
      "longitude": 69.279737
    }
  ]
}
```

### Response (Success - 201 Created)

```json
{
  "status": true,
  "message": "Location muvaffaqiyatli yaratildi",
  "data": [
    {
      "id": 12345,
      "user_id": 123,
      "recorded_at": "2025-12-25T10:30:00Z",
      "latitude": 41.311151,
      "longitude": 69.279737,
      "accuracy": 10.5,
      "speed": 5.2,
      "heading": 180.0,
      "altitude": 350.0,
      "ellipsoidal_altitude": 352.5,
      "heading_accuracy": 5.0,
      "speed_accuracy": 1.5,
      "altitude_accuracy": 3.0,
      "floor": 2,
      "activity_type": "walking",
      "activity_confidence": 85,
      "is_moving": true,
      "battery_level": 75,
      "is_charging": false,
      "timestamp": "2025-12-25T10:30:00Z",
      "age": 500.0,
      "event": "motionchange",
      "mock": false,
      "sample": false,
      "odometer": 1234.56,
      "uuid": "550e8400-e29b-41d4-a716-446655440000",
      "extras": "{\"custom_field\":\"value\"}",
      "distance_from_previous": 250.5,
      "created_at": "2025-12-25T10:30:05Z"
    }
  ]
}
```

### Response (Error - 400 Bad Request)

```json
{
  "status": false,
  "message": "Locations array bo'sh bo'lmasligi kerak",
  "data": null
}
```

### Response (Error - 401 Unauthorized)

```json
{
  "status": false,
  "message": "Noto'g'ri yoki mavjud bo'lmagan user_id token'da",
  "data": null
}
```

---

## GET /api/locations/user/{user_id}

User'ning location'larini vaqt oralig'ida olish

### Request Headers

```
Authorization: Bearer YOUR_JWT_TOKEN
```

### Query Parameters

- `start_date` (optional): DateTime - Boshlang'ich vaqt
- `end_date` (optional): DateTime - Tugash vaqti
- `limit` (optional): int - Maksimal natijalar soni (default: 100)

### Example Request

```bash
GET /api/locations/user/123?start_date=2025-12-01T00:00:00Z&end_date=2025-12-25T23:59:59Z&limit=50
Authorization: Bearer YOUR_JWT_TOKEN
```

### Response (200 OK)

```json
{
  "status": true,
  "message": "Location ma'lumotlari muvaffaqiyatli olindi",
  "data": [
    {
      "id": 12345,
      "user_id": 123,
      "recorded_at": "2025-12-25T10:30:00Z",
      "latitude": 41.311151,
      "longitude": 69.279737,
      // ... barcha field'lar
    }
  ]
}
```

---

## GET /api/locations/user/{user_id}/last

User'ning oxirgi location'larini olish

### Request Headers

```
Authorization: Bearer YOUR_JWT_TOKEN
```

### Query Parameters

- `count` (optional): int - Nechta location qaytarish (default: 100)

### Example Request

```bash
GET /api/locations/user/123/last?count=10
Authorization: Bearer YOUR_JWT_TOKEN
```

### Response (200 OK)

```json
{
  "status": true,
  "message": "Oxirgi 10 ta location olindi",
  "data": [
    {
      "id": 12350,
      "user_id": 123,
      "recorded_at": "2025-12-25T10:35:00Z",
      // ... eng oxirgi location
    },
    {
      "id": 12349,
      "user_id": 123,
      "recorded_at": "2025-12-25T10:30:00Z",
      // ... oldingi location
    }
  ]
}
```

---

## GET /api/locations/user/{user_id}/daily_statistics

Kunlik statistikalar (masofa, location count)

### Request Headers

```
Authorization: Bearer YOUR_JWT_TOKEN
```

### Query Parameters

- `start_date` (required): DateTime - Boshlang'ich sana
- `end_date` (required): DateTime - Tugash sanasi

### Example Request

```bash
GET /api/locations/user/123/daily_statistics?start_date=2025-12-01T00:00:00Z&end_date=2025-12-25T23:59:59Z
Authorization: Bearer YOUR_JWT_TOKEN
```

### Response (200 OK)

```json
{
  "status": true,
  "message": "Kunlik statistikalar muvaffaqiyatli olindi",
  "data": [
    {
      "date": "2025-12-25",
      "total_distance_meters": 15234.56,
      "total_distance_kilometers": 15.23,
      "location_count": 152
    },
    {
      "date": "2025-12-24",
      "total_distance_meters": 12456.78,
      "total_distance_kilometers": 12.46,
      "location_count": 134
    }
  ]
}
```

---

## GET /api/locations/{id}

ID orqali bitta location olish

### Request Headers

```
Authorization: Bearer YOUR_JWT_TOKEN
```

### Query Parameters

- `recorded_at` (required): DateTime - Location yozilgan vaqt (partition key)

### Example Request

```bash
GET /api/locations/12345?recorded_at=2025-12-25T10:30:00Z
Authorization: Bearer YOUR_JWT_TOKEN
```

### Response (200 OK)

```json
{
  "status": true,
  "message": "Location muvaffaqiyatli olindi",
  "data": {
    "id": 12345,
    "user_id": 123,
    "recorded_at": "2025-12-25T10:30:00Z",
    "latitude": 41.311151,
    "longitude": 69.279737,
    // ... barcha field'lar
  }
}
```

### Response (404 Not Found)

```json
{
  "status": false,
  "message": "Location topilmadi",
  "data": null
}
```

---

## Flutter Integration Example

### 1. Token Olish

```dart
// AuthService.dart
class AuthService {
  String? _token;

  Future<void> authenticate(String phone, String otp) async {
    final response = await http.post(
      Uri.parse('https://api.example.com/api/auth/verify_otp'),
      body: jsonEncode({
        'phone_number': phone,
        'otp_code': otp,
      }),
      headers: {'Content-Type': 'application/json'},
    );

    final data = jsonDecode(response.body);
    if (data['status'] == true) {
      _token = data['data']['token'];
      // Save token to secure storage
    }
  }

  String? getToken() => _token;
}
```

### 2. Location Yuborish (Minimal)

```dart
import 'package:flutter_background_geolocation/flutter_background_geolocation.dart' as bg;

Future<void> sendMinimalLocation(bg.Location location) async {
  final token = authService.getToken();

  final response = await http.post(
    Uri.parse('https://api.example.com/api/locations'),
    headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $token',
    },
    body: jsonEncode({
      'locations': [
        {
          // Faqat required field'lar
          'latitude': location.coords.latitude,
          'longitude': location.coords.longitude,
        }
      ]
    }),
  );

  if (response.statusCode == 201) {
    print('Location sent successfully');
  }
}
```

### 3. Location Yuborish (To'liq)

```dart
Future<void> sendFullLocation(bg.Location location) async {
  final token = authService.getToken();

  final response = await http.post(
    Uri.parse('https://api.example.com/api/locations'),
    headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $token',
    },
    body: jsonEncode({
      'locations': [
        {
          // Required
          'latitude': location.coords.latitude,
          'longitude': location.coords.longitude,

          // Optional - Core
          'recorded_at': location.timestamp,
          'accuracy': location.coords.accuracy,
          'speed': location.coords.speed,
          'heading': location.coords.heading,
          'altitude': location.coords.altitude,

          // Optional - Extended Coords
          'ellipsoidal_altitude': location.coords.ellipsoidalAltitude,
          'heading_accuracy': location.coords.headingAccuracy,
          'speed_accuracy': location.coords.speedAccuracy,
          'altitude_accuracy': location.coords.altitudeAccuracy,
          'floor': location.coords.floor,

          // Optional - Activity
          'activity_type': location.activity.type,
          'activity_confidence': location.activity.confidence,
          'is_moving': location.isMoving,

          // Optional - Battery
          'battery_level': (location.battery.level * 100).toInt(),
          'is_charging': location.battery.isCharging,

          // Optional - Metadata
          'timestamp': location.timestamp,
          'age': location.age,
          'event': location.event,
          'mock': location.mock,
          'sample': location.sample,
          'odometer': location.odometer,
          'uuid': location.uuid,
          'extras': jsonEncode(location.extras ?? {}),
        }
      ]
    }),
  );

  if (response.statusCode == 201) {
    final data = jsonDecode(response.body);
    print('Location sent: ${data['data'][0]['id']}');
  } else if (response.statusCode == 401) {
    // Token expired, re-authenticate
    print('Token expired, need to re-authenticate');
  }
}
```

---

## Key Changes Summary

### ❌ Old API (eski)

```json
POST /api/locations
{
  "user_id": 123,  // Body'da yuborilardi
  "locations": [...]
}
```

### ✅ New API (yangi)

```json
POST /api/locations
Authorization: Bearer TOKEN  // Header'da token
{
  "locations": [...]  // user_id yo'q, tokendan olinadi
}
```

### Field Requirements

| Field | Old | New |
|-------|-----|-----|
| user_id | Required in body | Extracted from JWT token |
| latitude | Required | Required |
| longitude | Required | Required |
| recorded_at | Required | Optional (default: server time) |
| All other fields | Optional | Optional |

---

## Error Handling

### 401 Unauthorized

Token yo'q yoki noto'g'ri:

```json
{
  "status": false,
  "message": "Noto'g'ri yoki mavjud bo'lmagan user_id token'da",
  "data": null
}
```

**Solution**: Qayta authenticate qiling va yangi token oling.

### 400 Bad Request

Body validation failed:

```json
{
  "status": false,
  "message": "Locations array bo'sh bo'lmasligi kerak",
  "data": null
}
```

**Solution**: Request body'ni to'g'ri format bilan yuboring.

### 500 Internal Server Error

Server xatolik:

```json
{
  "status": false,
  "message": "Location yaratishda xatolik yuz berdi",
  "data": null
}
```

**Solution**: Loglarni tekshiring va server administratoriga xabar bering.

---

## Testing with cURL

### 1. Get Token

```bash
# Step 1: Verify phone
curl -X POST http://localhost:5084/api/auth/verify_number \
  -H "Content-Type: application/json" \
  -d '{"phone_number":"+998901234567"}'

# Step 2: Send OTP
curl -X POST http://localhost:5084/api/auth/send_otp \
  -H "Content-Type: application/json" \
  -d '{"phone_number":"+998901234567"}'

# Step 3: Verify OTP and get token
curl -X POST http://localhost:5084/api/auth/verify_otp \
  -H "Content-Type: application/json" \
  -d '{"phone_number":"+998901234567","otp_code":"1234"}'

# Response:
# {
#   "status": true,
#   "message": "...",
#   "data": {
#     "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
#   }
# }
```

### 2. Create Location (Minimal)

```bash
curl -X POST http://localhost:5084/api/locations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "locations": [
      {
        "latitude": 41.311151,
        "longitude": 69.279737
      }
    ]
  }'
```

### 3. Create Location (Full)

```bash
curl -X POST http://localhost:5084/api/locations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "locations": [
      {
        "latitude": 41.311151,
        "longitude": 69.279737,
        "accuracy": 10.5,
        "speed": 5.2,
        "activity_type": "walking",
        "is_moving": true,
        "battery_level": 75,
        "event": "motionchange"
      }
    ]
  }'
```

### 4. Get User Locations

```bash
curl -X GET "http://localhost:5084/api/locations/user/123?limit=10" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

---

## Important Notes

1. **Authentication Required**: Barcha endpoint'lar JWT token talab qiladi
2. **user_id Automatic**: Token'dagi user_id avtomatik ishlatiladi
3. **Minimal Payload**: Faqat latitude va longitude yuborish yetarli
4. **recorded_at Default**: Agar kelmasa, server UTC vaqtini yozadi
5. **All Fields Optional**: latitude/longitude'dan tashqari barcha field'lar optional
6. **Token Expiration**: Token muddati tugasa (default: 24 soat), qayta authenticate qiling

---

Happy Coding! 🚀
