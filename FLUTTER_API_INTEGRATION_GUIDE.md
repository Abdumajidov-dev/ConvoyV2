# Flutter API Integration Guide - Convoy GPS Tracking

Complete guide for integrating Convoy API with Flutter mobile application.

## Base Configuration

### API Base URL

```dart
class ApiConfig {
  // Development (local)
  static const String devBaseUrl = 'http://192.168.1.100:5084';

  // Production (Railway)
  static const String prodBaseUrl = 'https://convoy-production-2969.up.railway.app';

  // Current environment
  static const String baseUrl = prodBaseUrl; // Change based on environment
}
```

**IMPORTANT:**
- For local testing: Use your computer's IP address (not `localhost` or `127.0.0.1`)
- For production: Use Railway deployment URL

---

## Authentication Flow (Step by Step)

### Step 1: Verify Phone Number

Check if phone number exists in system and is authorized.

**Endpoint:** `POST /api/auth/verify_number`

**Request:**
```dart
import 'dart:convert';
import 'package:http/http.dart' as http;

Future<Map<String, dynamic>> verifyPhoneNumber(String phoneNumber) async {
  final url = Uri.parse('${ApiConfig.baseUrl}/api/auth/verify_number');

  final response = await http.post(
    url,
    headers: {
      'Content-Type': 'application/json',
    },
    body: jsonEncode({
      'phone_number': phoneNumber, // snake_case format
    }),
  );

  if (response.statusCode == 200 || response.statusCode == 400) {
    return jsonDecode(response.body);
  } else {
    throw Exception('Failed to verify phone number: ${response.statusCode}');
  }
}
```

**Request Body:**
```json
{
  "phone_number": "998941033001"
}
```

**Success Response (200 OK):**
```json
{
  "status": true,
  "message": "Raqam tasdiqlandi",
  "data": {
    "worker_id": 123,
    "worker_name": "John Doe",
    "worker_guid": "uuid-here",
    "position_id": 86,
    "position_name": "Driver",
    "branch_guid": "branch-uuid",
    "branch_name": "Tashkent Branch"
  }
}
```

**Error Response (400 Bad Request):**
```json
{
  "status": false,
  "message": "Foydalanuvchi topilmadi yoki ruxsat yo'q",
  "data": null
}
```

**Usage Example:**
```dart
try {
  final result = await verifyPhoneNumber('998941033001');

  if (result['status'] == true) {
    // Phone verified - proceed to Step 2
    final workerData = result['data'];
    print('Worker ID: ${workerData['worker_id']}');
    print('Name: ${workerData['worker_name']}');

    // Move to OTP screen
    Navigator.push(context, MaterialPageRoute(
      builder: (context) => OtpScreen(phoneNumber: phoneNumber),
    ));
  } else {
    // Show error message
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(result['message'])),
    );
  }
} catch (e) {
  print('Error: $e');
  // Show error dialog
}
```

---

### Step 2: Send OTP Code

Request OTP code to be sent via SMS.

**Endpoint:** `POST /api/auth/send_otp`

**Request:**
```dart
Future<Map<String, dynamic>> sendOtp(String phoneNumber) async {
  final url = Uri.parse('${ApiConfig.baseUrl}/api/auth/send_otp');

  final response = await http.post(
    url,
    headers: {
      'Content-Type': 'application/json',
    },
    body: jsonEncode({
      'phone_number': phoneNumber,
    }),
  );

  if (response.statusCode == 200 || response.statusCode == 400) {
    return jsonDecode(response.body);
  } else {
    throw Exception('Failed to send OTP: ${response.statusCode}');
  }
}
```

**Request Body:**
```json
{
  "phone_number": "998941033001"
}
```

**Success Response (200 OK):**
```json
{
  "status": true,
  "message": "OTP kod yuborildi",
  "data": null
}
```

**Error Response (400 Bad Request):**
```json
{
  "status": false,
  "message": "Iltimos 60 soniya kuting va qayta urinib ko'ring",
  "data": null
}
```

**Usage Example:**
```dart
try {
  final result = await sendOtp('998941033001');

  if (result['status'] == true) {
    // OTP sent successfully
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('SMS kod yuborildi')),
    );

    // Start countdown timer (60 seconds)
    startOtpTimer();
  } else {
    // Show error (rate limit or other error)
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(result['message'])),
    );
  }
} catch (e) {
  print('Error: $e');
}
```

---

### Step 3: Verify OTP and Get JWT Token

Submit OTP code and receive authentication token.

**Endpoint:** `POST /api/auth/verify_otp`

**Request:**
```dart
Future<Map<String, dynamic>> verifyOtp(String phoneNumber, String otpCode) async {
  final url = Uri.parse('${ApiConfig.baseUrl}/api/auth/verify_otp');

  final response = await http.post(
    url,
    headers: {
      'Content-Type': 'application/json',
    },
    body: jsonEncode({
      'phone_number': phoneNumber,
      'otp_code': otpCode,
    }),
  );

  if (response.statusCode == 200 || response.statusCode == 400) {
    return jsonDecode(response.body);
  } else {
    throw Exception('Failed to verify OTP: ${response.statusCode}');
  }
}
```

**Request Body:**
```json
{
  "phone_number": "998941033001",
  "otp_code": "1234"
}
```

**Success Response (200 OK):**
```json
{
  "status": true,
  "message": "Muvaffaqiyatli autentifikatsiya qilindi",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user_id": 123,
    "name": "John Doe",
    "phone": "998941033001"
  }
}
```

**Error Response (400 Bad Request):**
```json
{
  "status": false,
  "message": "Noto'g'ri OTP kod",
  "data": null
}
```

**Usage Example:**
```dart
try {
  final result = await verifyOtp('998941033001', '1234');

  if (result['status'] == true) {
    // Authentication successful - save token
    final token = result['data']['token'];
    final userId = result['data']['user_id'];

    // Save to secure storage
    await secureStorage.write(key: 'auth_token', value: token);
    await secureStorage.write(key: 'user_id', value: userId.toString());

    // Navigate to home screen
    Navigator.pushReplacement(context, MaterialPageRoute(
      builder: (context) => HomeScreen(),
    ));
  } else {
    // Show error
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(result['message'])),
    );
  }
} catch (e) {
  print('Error: $e');
}
```

---

### Step 4: Get Current User Info

Retrieve authenticated user information.

**Endpoint:** `GET /api/auth/me`

**Request:**
```dart
Future<Map<String, dynamic>> getCurrentUser(String token) async {
  final url = Uri.parse('${ApiConfig.baseUrl}/api/auth/me');

  final response = await http.get(
    url,
    headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $token', // IMPORTANT: Include Bearer token
    },
  );

  if (response.statusCode == 200 || response.statusCode == 401) {
    return jsonDecode(response.body);
  } else {
    throw Exception('Failed to get user info: ${response.statusCode}');
  }
}
```

**No Request Body (GET method)**

**Success Response (200 OK):**
```json
{
  "status": true,
  "message": "Foydalanuvchi ma'lumotlari",
  "data": {
    "user_id": 123,
    "name": "John Doe",
    "phone": "998941033001",
    "worker_guid": "uuid-here",
    "branch_guid": "branch-uuid",
    "branch_name": "Tashkent Branch",
    "position_id": 86,
    "permissions": [
      "locations.create",
      "locations.view",
      "users.view"
    ]
  }
}
```

**Error Response (401 Unauthorized):**
```json
{
  "status": false,
  "message": "Token yaroqsiz yoki muddati tugagan",
  "data": null
}
```

**Usage Example:**
```dart
try {
  final token = await secureStorage.read(key: 'auth_token');
  if (token == null) {
    // Navigate to login
    return;
  }

  final result = await getCurrentUser(token);

  if (result['status'] == true) {
    // Update UI with user info
    final userData = result['data'];
    setState(() {
      userName = userData['name'];
      userPhone = userData['phone'];
      userPermissions = List<String>.from(userData['permissions']);
    });
  } else {
    // Token expired - navigate to login
    await secureStorage.delete(key: 'auth_token');
    Navigator.pushReplacement(context, MaterialPageRoute(
      builder: (context) => LoginScreen(),
    ));
  }
} catch (e) {
  print('Error: $e');
}
```

---

### Step 5: Logout

Invalidate current token (blacklist it).

**Endpoint:** `POST /api/auth/logout`

**Request:**
```dart
Future<Map<String, dynamic>> logout(String token) async {
  final url = Uri.parse('${ApiConfig.baseUrl}/api/auth/logout');

  final response = await http.post(
    url,
    headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $token',
    },
  );

  if (response.statusCode == 200 || response.statusCode == 401) {
    return jsonDecode(response.body);
  } else {
    throw Exception('Failed to logout: ${response.statusCode}');
  }
}
```

**No Request Body (POST method with Authorization header)**

**Success Response (200 OK):**
```json
{
  "status": true,
  "message": "Muvaffaqiyatli logout qilindi",
  "data": null
}
```

**Usage Example:**
```dart
try {
  final token = await secureStorage.read(key: 'auth_token');
  if (token != null) {
    await logout(token);
  }

  // Clear local storage
  await secureStorage.delete(key: 'auth_token');
  await secureStorage.delete(key: 'user_id');

  // Navigate to login
  Navigator.pushReplacement(context, MaterialPageRoute(
    builder: (context) => LoginScreen(),
  ));
} catch (e) {
  print('Error during logout: $e');
  // Still clear local data and navigate to login
}
```

---

## Complete Authentication Service Class

```dart
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class AuthService {
  static const String baseUrl = 'https://convoy-production-2969.up.railway.app';
  final _secureStorage = const FlutterSecureStorage();

  // Step 1: Verify Phone Number
  Future<Map<String, dynamic>> verifyPhoneNumber(String phoneNumber) async {
    final url = Uri.parse('$baseUrl/api/auth/verify_number');

    final response = await http.post(
      url,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'phone_number': phoneNumber}),
    );

    return jsonDecode(response.body);
  }

  // Step 2: Send OTP
  Future<Map<String, dynamic>> sendOtp(String phoneNumber) async {
    final url = Uri.parse('$baseUrl/api/auth/send_otp');

    final response = await http.post(
      url,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'phone_number': phoneNumber}),
    );

    return jsonDecode(response.body);
  }

  // Step 3: Verify OTP
  Future<Map<String, dynamic>> verifyOtp(String phoneNumber, String otpCode) async {
    final url = Uri.parse('$baseUrl/api/auth/verify_otp');

    final response = await http.post(
      url,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'phone_number': phoneNumber,
        'otp_code': otpCode,
      }),
    );

    final result = jsonDecode(response.body);

    // Save token if successful
    if (result['status'] == true && result['data'] != null) {
      await _secureStorage.write(
        key: 'auth_token',
        value: result['data']['token'],
      );
      await _secureStorage.write(
        key: 'user_id',
        value: result['data']['user_id'].toString(),
      );
    }

    return result;
  }

  // Step 4: Get Current User
  Future<Map<String, dynamic>> getCurrentUser() async {
    final token = await _secureStorage.read(key: 'auth_token');
    if (token == null) {
      throw Exception('No auth token found');
    }

    final url = Uri.parse('$baseUrl/api/auth/me');

    final response = await http.get(
      url,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
    );

    return jsonDecode(response.body);
  }

  // Step 5: Logout
  Future<void> logout() async {
    final token = await _secureStorage.read(key: 'auth_token');

    if (token != null) {
      try {
        final url = Uri.parse('$baseUrl/api/auth/logout');
        await http.post(
          url,
          headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer $token',
          },
        );
      } catch (e) {
        print('Logout API error: $e');
      }
    }

    // Clear local storage regardless of API response
    await _secureStorage.delete(key: 'auth_token');
    await _secureStorage.delete(key: 'user_id');
  }

  // Helper: Get saved token
  Future<String?> getToken() async {
    return await _secureStorage.read(key: 'auth_token');
  }

  // Helper: Check if user is authenticated
  Future<bool> isAuthenticated() async {
    final token = await getToken();
    return token != null;
  }
}
```

---

## Required Flutter Packages

Add to `pubspec.yaml`:

```yaml
dependencies:
  http: ^1.1.0
  flutter_secure_storage: ^9.0.0
  # Optional for better state management
  provider: ^6.1.1
```

Install:
```bash
flutter pub get
```

---

## Usage in Flutter App

### Login Flow Example

```dart
class LoginScreen extends StatefulWidget {
  @override
  _LoginScreenState createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _authService = AuthService();
  final _phoneController = TextEditingController();
  bool _isLoading = false;

  Future<void> _handleLogin() async {
    setState(() => _isLoading = true);

    try {
      // Step 1: Verify phone
      final verifyResult = await _authService.verifyPhoneNumber(
        _phoneController.text,
      );

      if (verifyResult['status'] == true) {
        // Navigate to OTP screen
        Navigator.push(
          context,
          MaterialPageRoute(
            builder: (context) => OtpScreen(
              phoneNumber: _phoneController.text,
            ),
          ),
        );
      } else {
        // Show error
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(verifyResult['message'])),
        );
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Xatolik: $e')),
      );
    } finally {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Kirish')),
      body: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          children: [
            TextField(
              controller: _phoneController,
              decoration: InputDecoration(
                labelText: 'Telefon raqam',
                hintText: '998901234567',
              ),
              keyboardType: TextInputType.phone,
            ),
            SizedBox(height: 20),
            ElevatedButton(
              onPressed: _isLoading ? null : _handleLogin,
              child: _isLoading
                  ? CircularProgressIndicator()
                  : Text('Davom etish'),
            ),
          ],
        ),
      ),
    );
  }
}
```

---

## Important Notes

### 1. **snake_case JSON Convention**
ALL API fields use snake_case:
- ✅ `phone_number` (correct)
- ❌ `phoneNumber` (wrong)
- ✅ `otp_code` (correct)
- ❌ `otpCode` (wrong)

### 2. **Authorization Header Format**
```dart
'Authorization': 'Bearer $token'  // CORRECT - with "Bearer " prefix
'Authorization': token            // WRONG - missing "Bearer " prefix
```

### 3. **Token Storage**
Use `flutter_secure_storage` for secure token storage (NOT SharedPreferences).

### 4. **Error Handling**
All responses have same format:
```json
{
  "status": true/false,
  "message": "User-friendly message",
  "data": {...} or null
}
```

Always check `status` field first, then handle `message` and `data`.

### 5. **Network Configuration**
- **Android**: Add internet permission to `AndroidManifest.xml`:
  ```xml
  <uses-permission android:name="android.permission.INTERNET"/>
  ```
- **iOS**: Configure `Info.plist` for HTTP requests (if using HTTP in development)

---

## Test Phone Numbers (Development Only)

For testing without real SMS:

```dart
// These phone numbers use fixed OTP code "1111"
'998941033001' -> OTP: '1111'
'998916714835' -> OTP: '1111'
'998901234567' -> OTP: '1111'
```

**IMPORTANT:** Test phone numbers only work in development environment.

---

## Next Steps

After authentication is working:
1. Implement GPS location tracking (see `FLUTTER_BACKGROUND_GEOLOCATION_INTEGRATION.md`)
2. Integrate SignalR for real-time updates (see `FLUTTER-SIGNALR-EXAMPLE.md`)
3. Add location submission endpoints

---

**Last Updated:** 2026-01-17
**API Base URL:** https://convoy-production-2969.up.railway.app
**API Version:** v1.0
