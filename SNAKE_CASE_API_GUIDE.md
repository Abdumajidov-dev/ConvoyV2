# Snake Case API Guide

## Overview

Barcha API endpoint'lar va JSON field'lar **snake_case** formatida. Bu Flutter developer'lar uchun qulay.

## ✅ Snake Case Format

### URL Endpoint'lar

```
✅ CORRECT (snake_case):
POST /api/locations/user_batch
GET  /api/locations/user/{user_id}/daily_statistics
POST /api/auth/verify_number
POST /api/auth/send_otp
POST /api/auth/verify_otp

❌ INCORRECT (camelCase/PascalCase):
POST /api/locations/userBatch
GET  /api/locations/user/{userId}/dailyStatistics
POST /api/auth/verifyNumber
```

### JSON Request/Response

```json
✅ CORRECT (snake_case):
{
  "user_id": 1,
  "recorded_at": "2025-12-18T08:00:00Z",
  "latitude": 41.311151,
  "longitude": 69.279737,
  "activity_type": "walking",
  "activity_confidence": 85,
  "is_moving": true,
  "battery_level": 95,
  "is_charging": false,
  "distance_from_previous": 125.5
}

❌ INCORRECT (camelCase):
{
  "userId": 1,
  "recordedAt": "2025-12-18T08:00:00Z",
  "activityType": "walking"
}
```

## 📍 Location Endpoints

### 1. Create Locations (UserId + Locations Array)
```http
POST /api/locations
Content-Type: application/json

{
  "user_id": 1,
  "locations": [
    {
      "recorded_at": "2025-12-18T08:00:00Z",
      "latitude": 41.311151,
      "longitude": 69.279737,
      "accuracy": 12.0,
      "speed": 0.0,
      "heading": 0.0,
      "altitude": 420.0,
      "activity_type": "still",
      "activity_confidence": 95,
      "is_moving": false,
      "battery_level": 95,
      "is_charging": false
    },
    {
      "recorded_at": "2025-12-18T08:05:00Z",
      "latitude": 41.312000,
      "longitude": 69.280000,
      "accuracy": 10.5,
      "speed": 1.5,
      "heading": 45.0,
      "altitude": 422.0,
      "activity_type": "walking",
      "activity_confidence": 85,
      "is_moving": true,
      "battery_level": 94,
      "is_charging": false
    }
  ]
}
```

**Bitta location uchun ham shu format ishlatiladi:**
```http
POST /api/locations
Content-Type: application/json

{
  "user_id": 1,
  "locations": [
    {
      "recorded_at": "2025-12-18T10:30:00Z",
      "latitude": 41.311151,
      "longitude": 69.279737,
      "accuracy": 10.5,
      "speed": 5.2,
      "heading": 180.0,
      "altitude": 420.0,
      "activity_type": "walking",
      "activity_confidence": 85,
      "is_moving": true,
      "battery_level": 75,
      "is_charging": false
    }
  ]
}
```

### 2. Get User Locations
```http
GET /api/locations/user/{user_id}?start_date=2025-12-01T00:00:00Z&end_date=2025-12-31T23:59:59Z
```

### 3. Get Last N Locations
```http
GET /api/locations/user/{user_id}/last?count=100
```

### 4. Get Daily Statistics
```http
GET /api/locations/user/{user_id}/daily_statistics?start_date=2025-12-01&end_date=2025-12-31
```

### 5. Get Location by ID
```http
GET /api/locations/{id}?recorded_at=2025-12-18T10:30:00Z
```

## 🔐 Auth Endpoints

### 1. Verify Phone Number
```http
POST /api/auth/verify_number
Content-Type: application/json

{
  "phone_number": "+998901234567"
}
```

**Response:**
```json
{
  "status": true,
  "message": "Telefon raqam tasdiqlandi",
  "data": {
    "worker_id": 123,
    "worker_name": "John Doe",
    "phone_number": "+998901234567",
    "worker_guid": "...",
    "branch_guid": "...",
    "branch_name": "...",
    "position_id": 2
  }
}
```

### 2. Send OTP
```http
POST /api/auth/send_otp
Content-Type: application/json

{
  "phone_number": "+998901234567"
}
```

**Response:**
```json
{
  "status": true,
  "message": "SMS kod yuborildi",
  "data": {
    "message": "SMS kod yuborildi"
  }
}
```

### 3. Verify OTP
```http
POST /api/auth/verify_otp
Content-Type: application/json

{
  "phone_number": "+998901234567",
  "otp_code": "123456"
}
```

**Response:**
```json
{
  "status": true,
  "message": "Muvaffaqiyatli tizimga kirildi",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

### 4. Get Current User (Protected)
```http
GET /api/auth/me
Authorization: Bearer YOUR_JWT_TOKEN
```

## 🔧 SignalR Test Endpoints

### 1. Health Check
```http
GET /api/signalr_test/health
```

### 2. Connection Info
```http
GET /api/signalr_test/connection_info
```

### 3. Broadcast Test Location
```http
POST /api/signalr_test/broadcast_test/{user_id}
```

### 4. Broadcast to All
```http
POST /api/signalr_test/broadcast_all
Content-Type: application/json

"Test message for all clients"
```

### 5. Broadcast to User
```http
POST /api/signalr_test/broadcast_user/{user_id}
Content-Type: application/json

"Test message for specific user"
```

### 6. Test Guide
```http
GET /api/signalr_test/test_guide
```

## 📱 Flutter Integration

### JSON Serialization

Flutter'da snake_case avtomatik handle qilinadi:

```dart
// Dart model
class LocationData {
  final int userId;
  final DateTime recordedAt;
  final double latitude;
  final double longitude;
  final String? activityType;
  final bool isMoving;

  LocationData({
    required this.userId,
    required this.recordedAt,
    required this.latitude,
    required this.longitude,
    this.activityType,
    required this.isMoving,
  });

  // JSON serialization
  Map<String, dynamic> toJson() {
    return {
      'user_id': userId,
      'recorded_at': recordedAt.toIso8601String(),
      'latitude': latitude,
      'longitude': longitude,
      'activity_type': activityType,
      'is_moving': isMoving,
    };
  }

  factory LocationData.fromJson(Map<String, dynamic> json) {
    return LocationData(
      userId: json['user_id'],
      recordedAt: DateTime.parse(json['recorded_at']),
      latitude: json['latitude'],
      longitude: json['longitude'],
      activityType: json['activity_type'],
      isMoving: json['is_moving'],
    );
  }
}
```

### API Request Example

```dart
import 'package:http/http.dart' as http;
import 'dart:convert';

Future<void> createUserLocationBatch(int userId, List<LocationData> locations) async {
  final url = Uri.parse('http://your-api.com/api/locations/user_batch');

  final requestBody = {
    'user_id': userId,
    'locations': locations.map((loc) => loc.toJson()).toList(),
  };

  final response = await http.post(
    url,
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode(requestBody),
  );

  if (response.statusCode == 201) {
    final List<dynamic> responseData = jsonDecode(response.body);
    // Process created locations
  }
}
```

## 🎯 Key Points

1. **All URLs**: snake_case (`user_batch`, `daily_statistics`, `verify_number`)
2. **All JSON fields**: snake_case (`user_id`, `recorded_at`, `activity_type`)
3. **Query parameters**: snake_case (`start_date`, `end_date`, `user_id`)
4. **Route parameters**: snake_case (`{user_id}`, `{location_id}`)

## ✨ Example: Complete Location Request

```http
POST /api/locations
Content-Type: application/json

{
  "user_id": 1,
  "locations": [
    {
      "recorded_at": "2025-12-20T08:00:00Z",
      "latitude": 41.311151,
      "longitude": 69.279737,
      "accuracy": 12.0,
      "speed": 0.0,
      "heading": 0.0,
      "altitude": 420.0,
      "activity_type": "still",
      "activity_confidence": 95,
      "is_moving": false,
      "battery_level": 95,
      "is_charging": false
    },
    {
      "recorded_at": "2025-12-20T08:05:00Z",
      "latitude": 41.312000,
      "longitude": 69.280000,
      "accuracy": 10.5,
      "speed": 1.5,
      "heading": 45.0,
      "altitude": 422.0,
      "activity_type": "walking",
      "activity_confidence": 85,
      "is_moving": true,
      "battery_level": 94,
      "is_charging": false
    },
    {
      "recorded_at": "2025-12-20T08:10:00Z",
      "latitude": 41.313000,
      "longitude": 69.281000,
      "accuracy": 8.0,
      "speed": 12.5,
      "heading": 90.0,
      "altitude": 425.0,
      "activity_type": "automotive",
      "activity_confidence": 90,
      "is_moving": true,
      "battery_level": 93,
      "is_charging": false
    }
  ]
}
```

**Response:**
```json
[
  {
    "id": 12345,
    "user_id": 1,
    "recorded_at": "2025-12-20T08:00:00Z",
    "latitude": 41.311151,
    "longitude": 69.279737,
    "accuracy": 12.0,
    "speed": 0.0,
    "heading": 0.0,
    "altitude": 420.0,
    "activity_type": "still",
    "activity_confidence": 95,
    "is_moving": false,
    "battery_level": 95,
    "is_charging": false,
    "distance_from_previous": null,
    "created_at": "2025-12-20T10:00:00Z"
  },
  {
    "id": 12346,
    "user_id": 1,
    "recorded_at": "2025-12-20T08:05:00Z",
    "latitude": 41.312000,
    "longitude": 69.280000,
    "accuracy": 10.5,
    "speed": 1.5,
    "heading": 45.0,
    "altitude": 422.0,
    "activity_type": "walking",
    "activity_confidence": 85,
    "is_moving": true,
    "battery_level": 94,
    "is_charging": false,
    "distance_from_previous": 125.5,
    "created_at": "2025-12-20T10:00:00Z"
  },
  {
    "id": 12347,
    "user_id": 1,
    "recorded_at": "2025-12-20T08:10:00Z",
    "latitude": 41.313000,
    "longitude": 69.281000,
    "accuracy": 8.0,
    "speed": 12.5,
    "heading": 90.0,
    "altitude": 425.0,
    "activity_type": "automotive",
    "activity_confidence": 90,
    "is_moving": true,
    "battery_level": 93,
    "is_charging": false,
    "distance_from_previous": 145.8,
    "created_at": "2025-12-20T10:00:00Z"
  }
]
```

---

**Happy Coding! 🚀**
