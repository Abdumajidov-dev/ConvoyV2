# API Response Format

## Standart Response Template

Barcha API endpoint'lar **bir xil formatda** response qaytaradi:

```json
{
  "status": true/false,
  "message": "User uchun xabar",
  "data": { ... }
}
```

## Response Structure

| Field | Type | Description |
|-------|------|-------------|
| `status` | `boolean` | `true` - muvaffaqiyatli, `false` - xatolik |
| `message` | `string` | Foydalanuvchi uchun tushunarli xabar |
| `data` | `any` | Response ma'lumotlari (success) yoki `null` (error) |

---

## ✅ Success Response Examples

### 1. POST /api/locations (Create Locations)

**Request:**
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
      "is_moving": false
    },
    {
      "recorded_at": "2025-12-20T08:05:00Z",
      "latitude": 41.312000,
      "longitude": 69.280000,
      "is_moving": true
    }
  ]
}
```

**Response (200 OK):**
```json
{
  "status": true,
  "message": "2 ta location muvaffaqiyatli yaratildi",
  "data": [
    {
      "id": 12345,
      "user_id": 1,
      "recorded_at": "2025-12-20T08:00:00Z",
      "latitude": 41.311151,
      "longitude": 69.279737,
      "accuracy": null,
      "speed": null,
      "heading": null,
      "altitude": null,
      "activity_type": null,
      "activity_confidence": null,
      "is_moving": false,
      "battery_level": null,
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
      "accuracy": null,
      "speed": null,
      "heading": null,
      "altitude": null,
      "activity_type": null,
      "activity_confidence": null,
      "is_moving": true,
      "battery_level": null,
      "is_charging": false,
      "distance_from_previous": 125.5,
      "created_at": "2025-12-20T10:00:00Z"
    }
  ]
}
```

### 2. GET /api/locations/user/{user_id} (Get User Locations)

**Request:**
```http
GET /api/locations/user/1?start_date=2025-12-01T00:00:00Z&end_date=2025-12-31T23:59:59Z
```

**Response (200 OK):**
```json
{
  "status": true,
  "message": "Location ma'lumotlari muvaffaqiyatli olindi",
  "data": [
    {
      "id": 123,
      "user_id": 1,
      "recorded_at": "2025-12-20T08:00:00Z",
      "latitude": 41.311151,
      "longitude": 69.279737,
      "is_moving": false,
      "distance_from_previous": null,
      "created_at": "2025-12-20T10:00:00Z"
    },
    {
      "id": 124,
      "user_id": 1,
      "recorded_at": "2025-12-20T08:05:00Z",
      "latitude": 41.312000,
      "longitude": 69.280000,
      "is_moving": true,
      "distance_from_previous": 125.5,
      "created_at": "2025-12-20T10:00:00Z"
    }
  ]
}
```

### 3. GET /api/locations/user/{user_id}/last (Get Last Locations)

**Request:**
```http
GET /api/locations/user/1/last?count=10
```

**Response (200 OK):**
```json
{
  "status": true,
  "message": "Oxirgi 10 ta location olindi",
  "data": [
    { ... },
    { ... }
  ]
}
```

### 4. GET /api/locations/user/{user_id}/daily_statistics

**Request:**
```http
GET /api/locations/user/1/daily_statistics?start_date=2025-12-01&end_date=2025-12-31
```

**Response (200 OK):**
```json
{
  "status": true,
  "message": "Kunlik statistikalar muvaffaqiyatli olindi",
  "data": [
    {
      "date": "2025-12-20T00:00:00Z",
      "total_distance_meters": 5420.5,
      "total_distance_kilometers": 5.42,
      "location_count": 48
    },
    {
      "date": "2025-12-21T00:00:00Z",
      "total_distance_meters": 3210.0,
      "total_distance_kilometers": 3.21,
      "location_count": 32
    }
  ]
}
```

### 5. POST /api/auth/verify_number (Auth - Verify Phone)

**Request:**
```http
POST /api/auth/verify_number
Content-Type: application/json

{
  "phone_number": "+998901234567"
}
```

**Response (200 OK):**
```json
{
  "status": true,
  "message": "Telefon raqam tasdiqlandi",
  "data": {
    "worker_id": 123,
    "worker_name": "John Doe",
    "phone_number": "+998901234567",
    "worker_guid": "abc-123-def",
    "branch_guid": "xyz-789",
    "branch_name": "Toshkent Filial",
    "position_id": 2,
    "image": null
  }
}
```

### 6. POST /api/auth/send_otp (Auth - Send OTP)

**Request:**
```http
POST /api/auth/send_otp
Content-Type: application/json

{
  "phone_number": "+998901234567"
}
```

**Response (200 OK):**
```json
{
  "status": true,
  "message": "SMS kod yuborildi",
  "data": {
    "message": "SMS kod yuborildi"
  }
}
```

### 7. POST /api/auth/verify_otp (Auth - Verify OTP)

**Request:**
```http
POST /api/auth/verify_otp
Content-Type: application/json

{
  "phone_number": "+998901234567",
  "otp_code": "123456"
}
```

**Response (200 OK):**
```json
{
  "status": true,
  "message": "Muvaffaqiyatli tizimga kirildi",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

### 8. GET /api/auth/me (Auth - Get Current User)

**Request:**
```http
GET /api/auth/me
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response (200 OK):**
```json
{
  "status": true,
  "message": "Foydalanuvchi ma'lumotlari",
  "data": {
    "worker_id": 123,
    "worker_name": "John Doe",
    "phone_number": "+998901234567",
    "worker_guid": "abc-123-def",
    "branch_guid": "xyz-789",
    "branch_name": "Toshkent Filial",
    "position_id": 2,
    "image": null
  }
}
```

---

## ❌ Error Response Examples

### 1. Bad Request (400)

```json
{
  "status": false,
  "message": "Locations array bo'sh bo'lmasligi kerak",
  "data": null
}
```

### 2. Not Found (404)

```json
{
  "status": false,
  "message": "Location topilmadi",
  "data": null
}
```

### 3. Unauthorized (401)

```json
{
  "status": false,
  "message": "Token noto'g'ri yoki muddati tugagan",
  "data": null
}
```

### 4. Internal Server Error (500)

```json
{
  "status": false,
  "message": "Internal server error",
  "data": null
}
```

### 5. Validation Error

```json
{
  "status": false,
  "message": "Noto'g'ri kod yoki kod muddati tugagan",
  "data": null
}
```

### 6. Auth Error - User Not Found

```json
{
  "status": false,
  "message": "Foydalanuvchi topilmadi",
  "data": null
}
```

### 7. Auth Error - Invalid Position

```json
{
  "status": false,
  "message": "Sizning lavozimingiz tizimga kirishga ruxsat bermaydi",
  "data": null
}
```

---

## 📱 Flutter Integration

### Success Response Handling

```dart
class ApiResponse<T> {
  final bool status;
  final String message;
  final T? data;

  ApiResponse({
    required this.status,
    required this.message,
    this.data,
  });

  factory ApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(dynamic)? fromJsonData,
  ) {
    return ApiResponse(
      status: json['status'],
      message: json['message'],
      data: json['data'] != null && fromJsonData != null
          ? fromJsonData(json['data'])
          : null,
    );
  }
}
```

### Usage Example

```dart
Future<List<LocationData>> getLocations(int userId) async {
  final response = await http.get(
    Uri.parse('http://your-api.com/api/locations/user/$userId'),
  );

  if (response.statusCode == 200) {
    final apiResponse = ApiResponse<List<LocationData>>.fromJson(
      jsonDecode(response.body),
      (data) => (data as List)
          .map((item) => LocationData.fromJson(item))
          .toList(),
    );

    if (apiResponse.status) {
      // Success!
      print(apiResponse.message); // "Location ma'lumotlari muvaffaqiyatli olindi"
      return apiResponse.data ?? [];
    } else {
      // API returned error
      throw Exception(apiResponse.message);
    }
  } else {
    throw Exception('HTTP Error: ${response.statusCode}');
  }
}
```

### Create Locations Example

```dart
Future<List<LocationData>> createLocations(
  int userId,
  List<LocationData> locations,
) async {
  final response = await http.post(
    Uri.parse('http://your-api.com/api/locations'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
      'user_id': userId,
      'locations': locations.map((l) => l.toJson()).toList(),
    }),
  );

  if (response.statusCode == 200) {
    final apiResponse = ApiResponse<List<LocationData>>.fromJson(
      jsonDecode(response.body),
      (data) => (data as List)
          .map((item) => LocationData.fromJson(item))
          .toList(),
    );

    if (apiResponse.status) {
      print(apiResponse.message); // "2 ta location muvaffaqiyatli yaratildi"
      return apiResponse.data ?? [];
    } else {
      throw Exception(apiResponse.message);
    }
  } else {
    throw Exception('HTTP Error: ${response.statusCode}');
  }
}
```

---

## 🎯 Key Points

1. **Har doim bir xil format**: Barcha endpoint'lar `{ status, message, data }` qaytaradi
2. **Status field**: `true` = success, `false` = error
3. **Message field**: Foydalanuvchi uchun tushunarli xabar (O'zbek tilida)
4. **Data field**:
   - Success: Actual data (object, array, etc.)
   - Error: `null`
5. **HTTP Status Codes**:
   - `200 OK` - Successful GET/POST (barcha success response'lar)
   - `400 Bad Request` - Validation error
   - `401 Unauthorized` - Auth error
   - `404 Not Found` - Resource not found
   - `500 Internal Server Error` - Server error

---

**Happy Coding! 🚀**
