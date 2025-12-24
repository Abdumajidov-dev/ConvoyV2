# Flutter Encryption Guide - AES-256-CBC with Random IV

## Overview

Bu loyihada **end-to-end encryption** ishlatiladi - barcha request va response'lar AES-256-CBC algoritmi bilan shifrlangan holda yuboriladi. **Har bir request uchun alohida random IV (Initialization Vector) generate qilinadi**, shu sababli bir xil ma'lumot har safar boshqacha shifrlangan ko'rinishda bo'ladi.

---

## Security Principles

### Why Random IV Per Request?

**Muammo (fixed IV bilan)**:
```
Request 1: {"phone": "998901234567"} ’ ABC123XYZ... (har doim bir xil)
Request 2: {"phone": "998901234567"} ’ ABC123XYZ... (bir xil!)
```
Attacker agar bir xil shifr ko'rsa, bir xil ma'lumot yuborilayotganini bilib oladi.

**Yechim (random IV bilan)**:
```
Request 1: {"phone": "998901234567"} ’ K8pQ7... (random IV + encrypted)
Request 2: {"phone": "998901234567"} ’ Mx3zL... (boshqa random IV + encrypted)
```
Har safar boshqa shifr, attacker hech narsa aniqlay olmaydi!

### How It Works

1. **Backend**:
   - Har bir response uchun random IV generate qiladi (16 bytes)
   - IV + encrypted data birgalikda Base64 string sifatida jo'natiladi
   - Format: `[IV (16 bytes)][Encrypted Data]` ’ Base64

2. **Flutter**:
   - Base64 string ni decode qiladi
   - Birinchi 16 byte'ni IV sifatida ajratib oladi
   - Qolgan qismini encrypted data sifatida decrypt qiladi
   - Request yuborishda ham xuddi shu printsipdan foydalanadi

---

## Setup Instructions

### 1. Add Dependencies

`pubspec.yaml` fayliga qo'shish:

```yaml
dependencies:
  http: ^1.1.0
  encrypt: ^5.0.3
```

Terminal'da:
```bash
flutter pub get
```

---

## 2. Create Encryption Service

`lib/services/encryption_service.dart` faylini yaratish:

```dart
import 'dart:convert';
import 'dart:typed_data';
import 'package:encrypt/encrypt.dart' as encrypt;

class EncryptionService {
  // Backend bilan AYNAN bir xil key ishlatilishi shart!
  // Keyni appsettings.json dan olish kerak
  static const String _keyBase64 = 'DMyx68VCO2K8IYpGjz6nlEn+AeXj/FEC1dVhUiWTJyQ=';

  late final encrypt.Key _key;
  late final encrypt.Encrypter _encrypter;

  EncryptionService() {
    // Key ni Base64 dan decode qilish (32 bytes = 256 bits)
    final keyBytes = base64.decode(_keyBase64);

    if (keyBytes.length != 32) {
      throw Exception('AES-256 requires 32-byte key, got ${keyBytes.length} bytes');
    }

    _key = encrypt.Key(Uint8List.fromList(keyBytes));

    // AES-256-CBC mode with PKCS7 padding
    _encrypter = encrypt.Encrypter(
      encrypt.AES(
        _key,
        mode: encrypt.AESMode.cbc,
        padding: 'PKCS7',
      ),
    );
  }

  /// Ma'lumotni shifrlash (random IV bilan)
  ///
  /// Returns: Base64 string containing [IV + Encrypted Data]
  String encryptData(String plainText) {
    try {
      // Random IV generate qilish (16 bytes)
      final iv = encrypt.IV.fromSecureRandom(16);

      // Encrypt qilish
      final encrypted = _encrypter.encrypt(plainText, iv: iv);

      // IV + encrypted data ni birlashtirib Base64 ga o'tkazish
      final combinedBytes = Uint8List.fromList([
        ...iv.bytes,              // Birinchi 16 bytes: IV
        ...encrypted.bytes,       // Qolganlari: encrypted data
      ]);

      return base64.encode(combinedBytes);
    } catch (e) {
      throw Exception('Encryption error: $e');
    }
  }

  /// Shifrlangan ma'lumotni decrypt qilish
  ///
  /// [cipherText]: Base64 string containing [IV + Encrypted Data]
  String decryptData(String cipherText) {
    try {
      // Base64 dan bytes ga o'tkazish
      final combinedBytes = base64.decode(cipherText.trim());

      if (combinedBytes.length < 16) {
        throw Exception('Invalid encrypted data: too short to contain IV');
      }

      // IV ni ajratib olish (birinchi 16 bytes)
      final ivBytes = combinedBytes.sublist(0, 16);
      final iv = encrypt.IV(Uint8List.fromList(ivBytes));

      // Encrypted data ni ajratib olish (16-bytesdan keyingi qism)
      final encryptedBytes = combinedBytes.sublist(16);
      final encrypted = encrypt.Encrypted(Uint8List.fromList(encryptedBytes));

      // Decrypt qilish
      final decrypted = _encrypter.decrypt(encrypted, iv: iv);

      return decrypted;
    } catch (e) {
      throw Exception('Decryption error: $e');
    }
  }
}
```

---

## 3. Create API Service

`lib/services/api_service.dart` faylini yaratish:

```dart
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'encryption_service.dart';

class ApiService {
  final String baseUrl;
  final EncryptionService _encryption = EncryptionService();
  final bool encryptionEnabled;

  String? _authToken;

  ApiService({
    required this.baseUrl,
    this.encryptionEnabled = true,
  });

  /// JWT token ni set qilish
  void setAuthToken(String token) {
    _authToken = token;
  }

  /// JWT token ni o'chirish
  void clearAuthToken() {
    _authToken = null;
  }

  /// POST request (encrypted)
  Future<Map<String, dynamic>> post(
    String endpoint,
    Map<String, dynamic> body,
  ) async {
    try {
      final url = Uri.parse('$baseUrl$endpoint');

      String requestBody;
      Map<String, String> headers = {};

      if (encryptionEnabled) {
        // 1. JSON ni string ga o'tkazish
        final jsonString = jsonEncode(body);

        // 2. String ni shifrlash (random IV bilan)
        requestBody = _encryption.encryptData(jsonString);

        // 3. Content-Type: text/plain (chunki encrypted Base64 string)
        headers['Content-Type'] = 'text/plain';
      } else {
        // Development mode: plain JSON
        requestBody = jsonEncode(body);
        headers['Content-Type'] = 'application/json';
      }

      // JWT token qo'shish (agar mavjud bo'lsa)
      if (_authToken != null) {
        headers['Authorization'] = 'Bearer $_authToken';
      }

      print('= Sending encrypted request to: $endpoint');

      // 4. Request yuborish
      final response = await http.post(
        url,
        headers: headers,
        body: requestBody,
      );

      print('=å Response status: ${response.statusCode}');

      if (response.statusCode >= 200 && response.statusCode < 300) {
        if (encryptionEnabled) {
          // 5. Response ni decrypt qilish
          final decryptedJson = _encryption.decryptData(response.body);
          print(' Response decrypted successfully');
          return jsonDecode(decryptedJson);
        } else {
          return jsonDecode(response.body);
        }
      } else {
        throw Exception('HTTP ${response.statusCode}: ${response.body}');
      }
    } catch (e) {
      print('L API Error: $e');
      rethrow;
    }
  }

  /// GET request (encrypted response)
  Future<Map<String, dynamic>> get(String endpoint) async {
    try {
      final url = Uri.parse('$baseUrl$endpoint');

      Map<String, String> headers = {};

      // JWT token qo'shish
      if (_authToken != null) {
        headers['Authorization'] = 'Bearer $_authToken';
      }

      print('=ä Sending GET request to: $endpoint');

      final response = await http.get(url, headers: headers);

      print('=å Response status: ${response.statusCode}');

      if (response.statusCode >= 200 && response.statusCode < 300) {
        if (encryptionEnabled) {
          // Response ni decrypt qilish
          final decryptedJson = _encryption.decryptData(response.body);
          print(' Response decrypted successfully');
          return jsonDecode(decryptedJson);
        } else {
          return jsonDecode(response.body);
        }
      } else {
        throw Exception('HTTP ${response.statusCode}: ${response.body}');
      }
    } catch (e) {
      print('L API Error: $e');
      rethrow;
    }
  }
}
```

---

## 4. Usage Example - Authentication Flow

```dart
import 'services/api_service.dart';

class AuthService {
  final ApiService _api = ApiService(
    baseUrl: 'http://YOUR_SERVER_IP:5084/api',
    encryptionEnabled: true,
  );

  /// Step 1: Verify phone number
  Future<Map<String, dynamic>> verifyPhoneNumber(String phoneNumber) async {
    final response = await _api.post(
      '/auth/verify_number',
      {'phone_number': phoneNumber},
    );

    // Response format: { status: true/false, message: "...", data: {...} }
    if (response['status'] == true) {
      return response['data'];
    } else {
      throw Exception(response['message'] ?? 'Verification failed');
    }
  }

  /// Step 2: Send OTP
  Future<void> sendOtp(String phoneNumber) async {
    final response = await _api.post(
      '/auth/send_otp',
      {'phone_number': phoneNumber},
    );

    if (response['status'] != true) {
      throw Exception(response['message'] ?? 'Failed to send OTP');
    }
  }

  /// Step 3: Verify OTP and get JWT token
  Future<String> verifyOtp(String phoneNumber, String otpCode) async {
    final response = await _api.post(
      '/auth/verify_otp',
      {
        'phone_number': phoneNumber,
        'otp_code': otpCode,
      },
    );

    if (response['status'] == true && response['data'] != null) {
      final token = response['data']['token'];

      // Token ni save qilish
      _api.setAuthToken(token);

      return token;
    } else {
      throw Exception(response['message'] ?? 'Invalid OTP');
    }
  }

  /// Logout
  void logout() {
    _api.clearAuthToken();
  }
}
```

---

## 5. Usage Example - Location Tracking

```dart
class LocationService {
  final ApiService _api = ApiService(
    baseUrl: 'http://YOUR_SERVER_IP:5084/api',
    encryptionEnabled: true,
  );

  /// Create location
  Future<void> sendLocation({
    required int userId,
    required double latitude,
    required double longitude,
    double? accuracy,
    double? speed,
  }) async {
    final response = await _api.post(
      '/locations',
      {
        'user_id': userId,
        'latitude': latitude,
        'longitude': longitude,
        'accuracy': accuracy,
        'speed': speed,
        'recorded_at': DateTime.now().toUtc().toIso8601String(),
      },
    );

    if (response['status'] == true) {
      print('Location sent successfully: ${response['data']}');
    } else {
      throw Exception(response['message'] ?? 'Failed to send location');
    }
  }

  /// Get user locations
  Future<List<dynamic>> getUserLocations(
    int userId,
    DateTime startDate,
    DateTime endDate,
  ) async {
    final endpoint = '/locations/user_batch?'
        'user_id=$userId&'
        'start_date=${startDate.toIso8601String()}&'
        'end_date=${endDate.toIso8601String()}';

    final response = await _api.get(endpoint);

    if (response['status'] == true && response['data'] != null) {
      return response['data'] as List<dynamic>;
    } else {
      throw Exception(response['message'] ?? 'Failed to fetch locations');
    }
  }
}
```

---

## 6. Testing Encryption

Encryption to'g'ri ishlashini test qilish:

```dart
void main() {
  final encryption = EncryptionService();

  // Test 1: Bir xil ma'lumot har safar boshqacha shifr
  final text = '{"phone": "998901234567"}';

  final encrypted1 = encryption.encryptData(text);
  final encrypted2 = encryption.encryptData(text);

  print('Encrypted 1: $encrypted1');
  print('Encrypted 2: $encrypted2');
  print('Are they different? ${encrypted1 != encrypted2}'); // true bo'lishi kerak

  // Test 2: Decrypt
  final decrypted1 = encryption.decryptData(encrypted1);
  final decrypted2 = encryption.decryptData(encrypted2);

  print('Decrypted 1: $decrypted1');
  print('Decrypted 2: $decrypted2');
  print('Are they same? ${decrypted1 == decrypted2}'); // true bo'lishi kerak
  print('Matches original? ${decrypted1 == text}'); // true bo'lishi kerak
}
```

**Expected Output**:
```
Encrypted 1: K8pQ7xZmN3vL... (random)
Encrypted 2: Mx3zLw9Yr2qP... (boshqa random)
Are they different? true 
Decrypted 1: {"phone": "998901234567"}
Decrypted 2: {"phone": "998901234567"}
Are they same? true 
Matches original? true 
```

---

## 7. Troubleshooting

### Error: "Key length not 128/192/256 bits"

**Sabab**: Key Base64 dan to'g'ri decode qilinmagan.

**Yechim**:
```dart
// L WRONG
final key = encrypt.Key.fromUtf8(_keyBase64); // UTF8 emas!

//  CORRECT
final keyBytes = base64.decode(_keyBase64);  // Base64 dan decode
final key = encrypt.Key(Uint8List.fromList(keyBytes));
```

### Error: "Invalid encrypted data: too short to contain IV"

**Sabab**: Backend'dan kelgan data juda qisqa yoki noto'g'ri format.

**Yechim**: Backend'da `Encryption:Enabled` true ekanligini tekshiring.

### Error: "Decryption error"

**Sabab**: Backend va Flutter'da turli xil key ishlatilayotgan.

**Yechim**: `appsettings.json` da `Encryption:Key` va Flutter'dagi `_keyBase64` AYNAN bir xil bo'lishi kerak:
```
Backend: "DMyx68VCO2K8IYpGjz6nlEn+AeXj/FEC1dVhUiWTJyQ="
Flutter: "DMyx68VCO2K8IYpGjz6nlEn+AeXj/FEC1dVhUiWTJyQ="
```

### Plain JSON requests not working

**Sabab**: Middleware encryption enabled bo'lsa ham plain JSON ni qabul qiladi (Swagger uchun).

**Yechim**: Flutter'dan request yuborishda `Content-Type: text/plain` ishlatish va `encryptionEnabled: true` qilish.

---

## 8. Security Best Practices

###  DO

1. **Key ni hardcode qilmang production'da**:
   ```dart
   // Development
   static const String _keyBase64 = 'DMyx68VCO2K8IYpGjz6nlEn+AeXj/FEC1dVhUiWTJyQ=';

   // Production: environment variable dan oling
   static final String _keyBase64 = const String.fromEnvironment('ENCRYPTION_KEY');
   ```

2. **HTTPS ishlatish** (TLS + Encryption = double protection):
   ```dart
   final baseUrl = 'https://your-domain.com/api'; // http emas!
   ```

3. **Token ni secure storage'da saqlash**:
   ```dart
   import 'package:flutter_secure_storage/flutter_secure_storage.dart';

   final storage = FlutterSecureStorage();
   await storage.write(key: 'jwt_token', value: token);
   ```

4. **Error handling**:
   ```dart
   try {
     await _api.post('/endpoint', data);
   } on Exception catch (e) {
     // User'ga friendly message ko'rsatish
     print('Request failed: $e');
   }
   ```

### L DON'T

1. **Git'ga key commit qilmang**:
   - `.gitignore` ga qo'shish: `lib/config/encryption_keys.dart`
   - Environment variables ishlatish

2. **Key'ni logga chiqarmang**:
   ```dart
   print('Key: $_keyBase64'); // NEVER DO THIS!
   ```

3. **HTTP ishlatmang production'da** (faqat HTTPS):
   ```dart
   // L INSECURE
   final baseUrl = 'http://api.example.com';

   //  SECURE
   final baseUrl = 'https://api.example.com';
   ```

---

## 9. Performance Considerations

- **Encryption overhead**: ~1-2ms per request (negligible)
- **Base64 encoding**: ~30% data size increase (acceptable)
- **Random IV generation**: Cryptographically secure, no performance impact

---

## 10. Complete Example - Login Screen

```dart
import 'package:flutter/material.dart';
import 'services/auth_service.dart';

class LoginScreen extends StatefulWidget {
  @override
  _LoginScreenState createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _authService = AuthService();
  final _phoneController = TextEditingController();
  final _otpController = TextEditingController();

  bool _otpSent = false;
  bool _loading = false;

  Future<void> _sendOtp() async {
    setState(() => _loading = true);

    try {
      // Step 1: Verify number
      await _authService.verifyPhoneNumber(_phoneController.text);

      // Step 2: Send OTP
      await _authService.sendOtp(_phoneController.text);

      setState(() {
        _otpSent = true;
        _loading = false;
      });

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('OTP yuborildi!')),
      );
    } catch (e) {
      setState(() => _loading = false);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Xatolik: $e')),
      );
    }
  }

  Future<void> _verifyOtp() async {
    setState(() => _loading = true);

    try {
      // Step 3: Verify OTP and get token
      final token = await _authService.verifyOtp(
        _phoneController.text,
        _otpController.text,
      );

      setState(() => _loading = false);

      // Navigate to home screen
      Navigator.pushReplacementNamed(context, '/home');
    } catch (e) {
      setState(() => _loading = false);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Noto\'g\'ri OTP: $e')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Login')),
      body: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          children: [
            TextField(
              controller: _phoneController,
              decoration: InputDecoration(labelText: 'Telefon raqam'),
              keyboardType: TextInputType.phone,
              enabled: !_otpSent && !_loading,
            ),
            SizedBox(height: 16),
            if (_otpSent)
              TextField(
                controller: _otpController,
                decoration: InputDecoration(labelText: 'OTP kod'),
                keyboardType: TextInputType.number,
                enabled: !_loading,
              ),
            SizedBox(height: 24),
            _loading
                ? CircularProgressIndicator()
                : ElevatedButton(
                    onPressed: _otpSent ? _verifyOtp : _sendOtp,
                    child: Text(_otpSent ? 'Tasdiqlash' : 'OTP yuborish'),
                  ),
          ],
        ),
      ),
    );
  }
}
```

---

## Summary

 **Backend**: Har bir response uchun random IV generate qiladi va IV + encrypted data ni Base64 string sifatida jo'natadi

 **Flutter**: Kelgan Base64 string'dan IV ni ajratib oladi va decrypt qiladi. Request yuborishda ham xuddi shu printsipdan foydalanadi.

 **Security**: Bir xil ma'lumot har safar boshqacha shifr (random IV tufayli)

 **Performance**: Minimal overhead, production-ready

 **Testing**: Swagger bilan ham ishlaydi (plain JSON qabul qiladi)

---

**Keyingsi qadamlar**:
1. Key'ni environment variable ga o'tkazish
2. HTTPS setup (production)
3. Secure token storage (flutter_secure_storage)
4. Error handling va user feedback
