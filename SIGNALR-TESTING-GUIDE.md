# SignalR Testing Guide - Test Qilish Bo'yicha To'liq Qo'llanma

## 📍 API Server Ma'lumotlari

```
Server URL: http://0.0.0.0:5084
Swagger UI: http://0.0.0.0:5084/swagger
SignalR Hub: http://0.0.0.0:5084/hubs/location
```

## 🧪 Test API Endpoints

### 1. Health Check - SignalR Ishlayotganini Tekshirish

```http
GET http://0.0.0.0:5084/api/signalrtest/health
```

**Javob:**
```json
{
  "status": "healthy",
  "signalR": "active",
  "hubEndpoint": "/hubs/location",
  "timestamp": "2025-12-19T10:08:00Z",
  "message": "SignalR hub is running and ready to accept connections"
}
```

### 2. Connection Info - SignalR Ma'lumotlari

```http
GET http://0.0.0.0:5084/api/signalrtest/connection-info
```

**Javob:** SignalR hub URL, methodlar, eventlar va misol data'lar

### 3. Test Guide - Step-by-Step Qo'llanma

```http
GET http://0.0.0.0:5084/api/signalrtest/test-guide
```

**Javob:** SignalR'ni qanday test qilish haqida to'liq qo'llanma

### 4. Broadcast Test Location - Test Location Yuborish

```http
POST http://0.0.0.0:5084/api/signalrtest/broadcast-test/{userId}
```

**Misol:** User 1 uchun test location yuborish:
```http
POST http://0.0.0.0:5084/api/signalrtest/broadcast-test/1
```

**Javob:**
```json
{
  "success": true,
  "message": "Test location broadcasted to user_1 and all_users groups",
  "testData": {
    "id": 999999,
    "userId": 1,
    "recordedAt": "2025-12-19T10:10:00Z",
    "latitude": 41.2995,
    "longitude": 69.2401,
    "accuracy": 10.5,
    "speed": 5.2,
    "heading": 180.0,
    "altitude": 450.0,
    "activityType": "walking",
    "activityConfidence": 85,
    "isMoving": true,
    "batteryLevel": 75,
    "isCharging": false,
    "distanceFromPrevious": 50.0,
    "createdAt": "2025-12-19T10:10:00Z"
  },
  "timestamp": "2025-12-19T10:10:00Z"
}
```

### 5. Broadcast to All - Barcha Client'larga Test Xabar

```http
POST http://0.0.0.0:5084/api/signalrtest/broadcast-all
Content-Type: application/json

"Salom barcha client'lar!"
```

### 6. Broadcast to User - Specific User'ga Test Xabar

```http
POST http://0.0.0.0:5084/api/signalrtest/broadcast-user/1
Content-Type: application/json

"Salom User 1!"
```

## 🎯 SignalR Test Qilish - Step by Step

### Step 1: API Ishlab Turganini Tekshirish

Swagger'ni oching:
```
http://0.0.0.0:5084/swagger
```

Yoki health check'ni chaqiring:
```bash
curl http://0.0.0.0:5084/api/signalrtest/health
```

### Step 2: Flutter'da SignalR Client Yaratish

```dart
import 'package:signalr_netcore/signalr_client.dart';

class SignalRTestClient {
  HubConnection? _hubConnection;

  Future<void> connect() async {
    _hubConnection = HubConnectionBuilder()
        .withUrl('http://YOUR_IP:5084/hubs/location')
        .withAutomaticReconnect()
        .build();

    // LocationUpdated event'ini tinglash
    _hubConnection!.on('LocationUpdated', (data) {
      print('📍 Location keldi:');
      print(data);
    });

    // TestMessage event'ini tinglash (test uchun)
    _hubConnection!.on('TestMessage', (data) {
      print('💬 Test message keldi:');
      print(data);
    });

    await _hubConnection!.start();
    print('✅ SignalR'ga ulandi!');
  }

  Future<void> joinUserTracking(int userId) async {
    await _hubConnection?.invoke('JoinUserTracking', args: [userId]);
    print('✅ User $userId'ni track qilishga qo\'shildi');
  }

  Future<void> joinAllUsers() async {
    await _hubConnection?.invoke('JoinAllUsersTracking');
    print('✅ Barcha user\'larni track qilishga qo\'shildi');
  }
}
```

### Step 3: Flutter'da Test Qilish

```dart
void main() async {
  final signalR = SignalRTestClient();

  // 1. Ulanish
  await signalR.connect();

  // 2. User 1'ni track qilish
  await signalR.joinUserTracking(1);

  // 3. Endi Swagger'dan broadcast-test/1 ni chaqiring
  // Yoki curl bilan:
  // curl -X POST http://0.0.0.0:5084/api/signalrtest/broadcast-test/1

  // Flutter consoleda location data ko'rinadi!
}
```

### Step 4: Haqiqiy Location Yaratish va SignalR Kuzatish

Flutter'da SignalR'ga ulangan holda, Swagger'dan location yarating:

```http
POST http://0.0.0.0:5084/api/locations
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "userId": 1,
  "recordedAt": "2025-12-19T10:15:00Z",
  "latitude": 41.2995,
  "longitude": 69.2401,
  "accuracy": 10.5,
  "speed": 5.2,
  "heading": 180.0,
  "altitude": 450.0,
  "activityType": "walking",
  "activityConfidence": 85,
  "isMoving": true,
  "batteryLevel": 75,
  "isCharging": false
}
```

**Natija:**
1. Location database'ga saqlanadi ✅
2. SignalR avtomatik broadcast qiladi ✅
3. Flutter app real-time location oladi ✅

## 🔍 Swagger'da Test Qilish

### 1. Swagger UI'ni Oching

```
http://0.0.0.0:5084/swagger
```

### 2. JWT Token Qo'shish (agar kerak bo'lsa)

- Swagger UI'da **Authorize** tugmasini bosing
- Bearer token kiriting: `Bearer YOUR_JWT_TOKEN`
- **Authorize** tugmasini bosing

### 3. SignalR Test Endpoint'larini Topish

Swagger'da quyidagi bo'limlarni toping:
- **SignalRTest** - SignalR test API'lari
- **Location** - Location CRUD API'lari

### 4. Test Qilish Tartibi

**A. Health Check:**
```
GET /api/signalrtest/health
→ Try it out → Execute
```

**B. Connection Info:**
```
GET /api/signalrtest/connection-info
→ Try it out → Execute
→ Response'da barcha ma'lumotlar ko'rinadi
```

**C. Test Location Broadcast:**
```
POST /api/signalrtest/broadcast-test/1
→ Try it out
→ userId = 1
→ Execute
```

**D. Real Location Yaratish:**
```
POST /api/locations
→ Try it out
→ JSON body'ni to'ldiring
→ Execute
```

## 📱 Flutter Developer Uchun Ma'lumotlar

### SignalR Hub URL

```
http://YOUR_SERVER_IP:5084/hubs/location
```

**Muhim:** `localhost` ishlamaydi! Real IP ishlatilishi kerak:
- Local network: `http://192.168.x.x:5084/hubs/location`
- Production: `http://your-domain.com/hubs/location`

### Server Methods (Flutter'dan chaqirish)

```dart
// User 123'ni track qilish
await hubConnection.invoke('JoinUserTracking', args: [123]);

// User tracking'dan chiqish
await hubConnection.invoke('LeaveUserTracking', args: [123]);

// Barcha user'larni track qilish
await hubConnection.invoke('JoinAllUsersTracking');

// Barcha'dan chiqish
await hubConnection.invoke('LeaveAllUsersTracking');
```

### Client Events (Server'dan keladi)

```dart
// Location update
hubConnection.on('LocationUpdated', (data) {
  // data[0] - LocationResponseDto
  final location = data![0] as Map<String, dynamic>;
  print('User ID: ${location['userId']}');
  print('Lat: ${location['latitude']}');
  print('Lng: ${location['longitude']}');
  print('Speed: ${location['speed']} m/s');
});

// Test messages (faqat testing uchun)
hubConnection.on('TestMessage', (data) {
  print('Test message: $data');
});
```

### LocationResponseDto Structure

```dart
{
  "id": 123456,
  "userId": 1,
  "recordedAt": "2025-12-19T10:15:00Z",
  "latitude": 41.2995,
  "longitude": 69.2401,
  "accuracy": 10.5,
  "speed": 5.2,
  "heading": 180.0,
  "altitude": 450.0,
  "activityType": "walking",
  "activityConfidence": 85,
  "isMoving": true,
  "batteryLevel": 75,
  "isCharging": false,
  "distanceFromPrevious": 50.0,
  "createdAt": "2025-12-19T10:15:00Z"
}
```

## 🐛 Troubleshooting

### SignalR'ga Ulanavman

**Muammo:** Connection failed

**Yechim:**
1. API ishlab turganini tekshiring: `GET /api/signalrtest/health`
2. URL to'g'ri ekanligini tekshiring (localhost emas, real IP)
3. CORS sozlangan (bizda AllowAll)
4. Flutter package versiyasi: `signalr_netcore: ^1.3.7`

### Location Keladi Lekin SignalR Ishlamaydi

**Muammo:** Location database'ga saqlanadi, lekin Flutter'ga kelmaydi

**Yechim:**
1. Group'ga qo'shildingizmi tekshiring: `JoinUserTracking(userId)`
2. Event name to'g'rimi: `LocationUpdated` (case-sensitive)
3. Connection active ekanligini tekshiring: `hubConnection.state`

### Test Broadcast Ishlamaydi

**Muammo:** `broadcast-test` endpoint chaqirish hech narsa bermaydi

**Yechim:**
1. Flutter app ulangani tasdiqlang
2. Group'ga qo'shilganingizni tekshiring
3. Console'da log'lar ko'ring: `hubConnection.on('LocationUpdated', ...)`

## 📊 Real-time Flow

```
┌─────────────┐
│   Flutter   │
│  Mobile App │
└──────┬──────┘
       │ 1. Connect to SignalR Hub
       │    /hubs/location
       ↓
┌──────────────────────┐
│   SignalR Hub        │
│   (LocationHub.cs)   │
└──────┬───────────────┘
       │ 2. JoinUserTracking(1)
       │    → Added to "user_1" group
       ↓
┌──────────────────────┐
│  Another Device      │
│  POST /api/locations │
│  { userId: 1, ... }  │
└──────┬───────────────┘
       │ 3. LocationService
       │    - Save to database ✓
       │    - Broadcast via SignalR ✓
       ↓
┌──────────────────────┐
│   SignalR Hub        │
│  Clients.Group()     │
│  .SendAsync()        │
└──────┬───────────────┘
       │ 4. Event: "LocationUpdated"
       │    { id, lat, lng, ... }
       ↓
┌──────────────────────┐
│   Flutter App        │
│  on('LocationUpdated')│
│  → Update UI ✓       │
└──────────────────────┘
```

## 🎬 Quick Test Scenario

### Scenario: Ikkita Qurilmada Test

**Device 1 - Flutter App (Tracking):**
```dart
// 1. SignalR'ga ulanish
await signalR.connect();

// 2. User 1'ni track qilish
await signalR.joinUserTracking(1);

// 3. Kutish...
```

**Device 2 - Swagger / Postman (Sending):**
```http
POST http://0.0.0.0:5084/api/locations
{
  "userId": 1,
  "latitude": 41.2995,
  "longitude": 69.2401,
  ...
}
```

**Natija:** Device 1'da real-time location data ko'rinadi! 🎉

## 📝 Flutter Developer'ga Berilishi Kerak Bo'lgan Ma'lumotlar

1. **Server URL:** `http://YOUR_IP:5084`
2. **SignalR Hub:** `/hubs/location`
3. **Swagger UI:** `http://YOUR_IP:5084/swagger`
4. **Connection Info Endpoint:** `GET /api/signalrtest/connection-info`
5. **Test Guide:** `GET /api/signalrtest/test-guide`
6. **Example Code:** `FLUTTER-SIGNALR-EXAMPLE.md` fayli

Swagger'dan barcha endpoint'lar va DTO'lar ko'rinadi! 🚀
