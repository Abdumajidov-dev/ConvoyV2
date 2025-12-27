# Location API - Final Version (with Encryption Support)

## Overview

Location API - to'liq versiya:
- ✅ **JWT Token authentication** - user_id tokendan olinadi
- ✅ **Encryption support** - middleware avtomatik encrypt/decrypt qiladi
- ✅ **Direct JSON object** - array emas, to'g'ridan-to'g'ri LocationDataDto
- ✅ **Minimal required fields** - faqat latitude va longitude
- ✅ **Auto RecordedAt** - kelmasa server hozirgi vaqtni yozadi

---

## Encryption Status

### ✅ HOZIRGI HOLAT - Plain JSON (Encryption Excluded)

`/api/locations` endpoint **VAQTINCHALIK** encryption'dan excluded:

```json
// appsettings.json
{
  "Encryption": {
    "Enabled": true,
    "ExcludedRoutes": [
      "/api/locations",      // Vaqtinchalik plain JSON
      "/api/locations/*"
    ]
  }
}
```

**Request/Response**: Plain JSON (encryption'siz)

**Body Format**: To'g'ridan-to'g'ri JSON object (array EMAS!)

```json
// ✅ HOZIRGI FORMAT
{
  "latitude": 41.3,
  "longitude": 69.2
}
```

### Kelajakda (Encryption Enable)

Encryption enable qilganingizda:
- **Request**: Encrypted Base64 string → Middleware decrypt → Controller
- **Response**: Controller JSON → Middleware encrypt → Encrypted Base64
- **Body Format**: O'sha formatda qoladi (to'g'ridan-to'g'ri object)
- **Controller**: Hech narsa o'zgarmaydi (middleware avtomatik)

---

## API Endpoint

### POST /api/locations

Bitta location yaratish

**Headers** (REQUIRED):
```
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

**Body Format**: To'g'ridan-to'g'ri LocationDataDto (array emas!)

---

## Request Examples

### 1. Minimal Request (faqat required fields)

```bash
POST /api/locations
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  "latitude": 41.311151,
  "longitude": 69.279737
}
```

### 2. Full Request (barcha optional fields bilan)

```json
POST /api/locations
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  // REQUIRED
  "latitude": 41.311151,
  "longitude": 69.279737,

  // OPTIONAL - Core location
  "recorded_at": "2025-12-25T10:30:00Z",
  "accuracy": 10.5,
  "speed": 5.2,
  "heading": 180.0,
  "altitude": 350.0,

  // OPTIONAL - Extended coords
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

  // OPTIONAL - Metadata
  "timestamp": "2025-12-25T10:30:00Z",
  "age": 500,
  "event": "motionchange",
  "mock": false,
  "sample": false,
  "odometer": 1234.56,
  "uuid": "550e8400-e29b-41d4-a716-446655440000",
  "extras": "{\"custom_field\":\"value\"}"
}
```

---

## Response Examples

### Success Response (201 Created)

```json
{
  "status": true,
  "message": "Location muvaffaqiyatli yaratildi",
  "data": {
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
}
```

### Error Response (401 Unauthorized)

```json
{
  "status": false,
  "message": "Noto'g'ri yoki mavjud bo'lmagan user_id token'da",
  "data": null
}
```

---

## Encryption Integration (Kelajakda)

### When Encryption is Enabled

#### Flutter Client Side

```dart
import 'package:encrypt/encrypt.dart' as encrypt;

class LocationApiService {
  final String apiUrl = 'https://api.example.com';
  final String token; // JWT token
  final encrypt.Encrypter encrypter;

  Future<void> sendLocation(bg.Location location) async {
    // 1. LocationDataDto yaratish
    final locationData = {
      'latitude': location.coords.latitude,
      'longitude': location.coords.longitude,
      'accuracy': location.coords.accuracy,
      'speed': location.coords.speed,
      // ... qolgan field'lar
    };

    // 2. JSON'ga convert qilish
    final jsonString = jsonEncode(locationData);

    // 3. Encrypt qilish
    final encrypted = encrypter.encrypt(jsonString);
    final encryptedBase64 = encrypted.base64;

    // 4. Server'ga yuborish
    final response = await http.post(
      Uri.parse('$apiUrl/api/locations'),
      headers: {
        'Authorization': 'Bearer $token',
        'Content-Type': 'text/plain', // IMPORTANT!
      },
      body: encryptedBase64, // Raw encrypted string
    );

    if (response.statusCode == 201) {
      // 5. Response'ni decrypt qilish
      final encryptedResponse = response.body;
      final decrypted = encrypter.decrypt64(encryptedResponse);
      final responseData = jsonDecode(decrypted);

      print('Location created: ${responseData['data']['id']}');
    }
  }
}
```

### Middleware Behavior

```
Request Flow:
Flutter → Encrypted JSON → EncryptionMiddleware → Decrypted JSON → Controller

Response Flow:
Controller → JSON → EncryptionMiddleware → Encrypted JSON → Flutter
```

**IMPORTANT**: Controller kodini o'zgartirish kerak emas - middleware avtomatik ishlaydi!

---

## Flutter Integration Examples

### 1. Without Encryption (Current)

```dart
import 'package:flutter_background_geolocation/flutter_background_geolocation.dart' as bg;

class LocationService {
  final String apiUrl;
  final String token;

  Future<void> sendLocation(bg.Location location) async {
    final response = await http.post(
      Uri.parse('$apiUrl/api/locations'),
      headers: {
        'Authorization': 'Bearer $token',
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        // To'g'ridan-to'g'ri LocationDataDto (array emas!)
        'latitude': location.coords.latitude,
        'longitude': location.coords.longitude,
        'accuracy': location.coords.accuracy,
        'speed': location.coords.speed,
        'heading': location.coords.heading,
        'altitude': location.coords.altitude,
        'ellipsoidal_altitude': location.coords.ellipsoidalAltitude,
        'heading_accuracy': location.coords.headingAccuracy,
        'speed_accuracy': location.coords.speedAccuracy,
        'altitude_accuracy': location.coords.altitudeAccuracy,
        'floor': location.coords.floor,
        'activity_type': location.activity.type,
        'activity_confidence': location.activity.confidence,
        'is_moving': location.isMoving,
        'battery_level': (location.battery.level * 100).toInt(),
        'is_charging': location.battery.isCharging,
        'timestamp': location.timestamp,
        'age': location.age,
        'event': location.event,
        'mock': location.mock,
        'sample': location.sample,
        'odometer': location.odometer,
        'uuid': location.uuid,
        'extras': jsonEncode(location.extras ?? {}),
      }),
    );

    if (response.statusCode == 201) {
      final data = jsonDecode(response.body);
      print('Location ID: ${data['data']['id']}');
    } else if (response.statusCode == 401) {
      // Token expired
      print('Re-authenticate needed');
    }
  }
}
```

### 2. With Encryption (Future)

```dart
import 'package:encrypt/encrypt.dart' as encrypt;

class EncryptedLocationService {
  final String apiUrl;
  final String token;
  late encrypt.Encrypter encrypter;
  late encrypt.IV iv;

  // Constructor'da encryption setup
  EncryptedLocationService(this.apiUrl, this.token, String keyBase64, String ivBase64) {
    final key = encrypt.Key.fromBase64(keyBase64);
    iv = encrypt.IV.fromBase64(ivBase64);
    encrypter = encrypt.Encrypter(encrypt.AES(key, mode: encrypt.AESMode.cbc));
  }

  Future<void> sendLocation(bg.Location location) async {
    // 1. Location data yaratish
    final locationData = {
      'latitude': location.coords.latitude,
      'longitude': location.coords.longitude,
      // ... qolgan field'lar (yuqoridagi kabi)
    };

    // 2. JSON string
    final jsonString = jsonEncode(locationData);

    // 3. Encrypt
    final encrypted = encrypter.encrypt(jsonString, iv: iv);
    final encryptedBase64 = encrypted.base64;

    // 4. Send to server
    final response = await http.post(
      Uri.parse('$apiUrl/api/locations'),
      headers: {
        'Authorization': 'Bearer $token',
        'Content-Type': 'text/plain', // Encrypted content
      },
      body: encryptedBase64,
    );

    if (response.statusCode == 201) {
      // 5. Decrypt response
      final encryptedResponse = response.body;
      final decrypted = encrypter.decrypt64(encryptedResponse, iv: iv);
      final data = jsonDecode(decrypted);

      print('Location ID: ${data['data']['id']}');
    }
  }
}
```

---

## Migration Guide

### From Old API to New API

#### ❌ Old Format (DEPRECATED)

```json
POST /api/locations
{
  "user_id": 123,  // ❌ Kerak emas endi
  "locations": [    // ❌ Array kerak emas
    {
      "latitude": 41.3,
      "longitude": 69.2
    }
  ]
}
```

#### ✅ New Format (CURRENT)

```json
POST /api/locations
Authorization: Bearer TOKEN

// To'g'ridan-to'g'ri object (array emas!)
{
  "latitude": 41.3,
  "longitude": 69.2
}
```

### Flutter Code Migration

```dart
// ❌ OLD CODE
final body = {
  'user_id': userId,  // Remove this
  'locations': [      // Remove array
    {
      'latitude': lat,
      'longitude': lon,
    }
  ]
};

// ✅ NEW CODE
final body = {
  // Direct object (no user_id, no array)
  'latitude': lat,
  'longitude': lon,
};
```

---

## Important Notes

### 1. Body Format

⚠️ **CRITICAL**: Body to'g'ridan-to'g'ri LocationDataDto object!

```json
// ❌ WRONG - array ichida
{"locations": [{"latitude": 41.3, "longitude": 69.2}]}

// ✅ CORRECT - to'g'ridan-to'g'ri object
{"latitude": 41.3, "longitude": 69.2}
```

### 2. Encryption

- **Hozir**: `/api/locations` encryption'dan excluded
- **Kelajakda**: Encryption enabled bo'lganda middleware avtomatik ishlaydi
- **Controller**: Hech qanday o'zgarish kerak emas

### 3. Required Fields

Faqat 2 ta field required:
- `latitude` (decimal)
- `longitude` (decimal)

Qolganlari **optional** - kelmasa `null` yoki default value ishlatiladi:
- `recorded_at` → `DateTime.UtcNow` (server time)
- `is_moving` → `false`
- `is_charging` → `false`

### 4. User ID

- Body'da `user_id` yuborishga **HOJAT YO'Q**
- JWT token'dan avtomatik olinadi
- Token'da `user_id` claim bo'lishi **MAJBURIY**

### 5. Real-time Features

Har bir location create bo'lganda:
- ✅ **SignalR**: Real-time broadcast (`user_{userId}` va `all_users` group'lariga)
- ✅ **Telegram**: Kanal notification (agar configured bo'lsa)
- ✅ **Distance Calculation**: Oldingi location'dan masofa avtomatik hisoblanadi

---

## Testing with cURL

### 1. Get Token

```bash
# Authenticate and get JWT token
curl -X POST http://localhost:5084/api/auth/verify_otp \
  -H "Content-Type: application/json" \
  -d '{"phone_number":"+998901234567","otp_code":"1234"}'

# Response contains token
```

### 2. Send Minimal Location

```bash
curl -X POST http://localhost:5084/api/locations \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "latitude": 41.311151,
    "longitude": 69.279737
  }'
```

### 3. Send Full Location

```bash
curl -X POST http://localhost:5084/api/locations \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "latitude": 41.311151,
    "longitude": 69.279737,
    "accuracy": 10.5,
    "speed": 5.2,
    "activity_type": "walking",
    "is_moving": true,
    "battery_level": 75,
    "event": "motionchange"
  }'
```

---

## Error Handling

### 401 Unauthorized

```json
{
  "status": false,
  "message": "Noto'g'ri yoki mavjud bo'lmagan user_id token'da",
  "data": null
}
```

**Reason**: Token yo'q, noto'g'ri, yoki `user_id` claim mavjud emas

**Solution**: Re-authenticate va yangi token oling

### 500 Internal Server Error

```json
{
  "status": false,
  "message": "Location yaratishda xatolik yuz berdi",
  "data": null
}
```

**Reason**: Server-side error (database, validation, etc.)

**Solution**: Loglarni tekshiring

---

## Summary

### ✅ What Changed

| Aspect | Old | New |
|--------|-----|-----|
| Body format | `{"user_id": X, "locations": [...]}` | To'g'ridan-to'g'ri `{"lat": X, "lon": Y}` |
| User ID | Body'da | JWT tokendan |
| Required fields | Ko'p field required edi | Faqat lat/lon |
| RecordedAt | Required | Optional (default: server time) |
| Response | Array | Single object |

### ✅ Key Features

- JWT token authentication
- Encryption support (middleware level)
- Minimal payload (faqat lat/lon)
- Auto defaults (recorded_at, is_moving, etc.)
- Real-time SignalR broadcast
- Telegram notifications
- Distance calculation

### ✅ Production Ready

- Build successful
- Backwards compatible (eski DTO deprecated)
- Encryption middleware compatible
- Complete documentation

---

Happy Coding! 🚀
