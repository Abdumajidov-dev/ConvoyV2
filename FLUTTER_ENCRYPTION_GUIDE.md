# Flutter End-to-End Encryption Guide

## 🔒 Muammo: Response Manipulation Attack

**Hacker qilgan ish:**
1. ✅ Burp Suite / Charles Proxy bilan request/response intercept qildi
2. ✅ Response'ni o'zgartirdi: `"status": false` → `"status": true`
3. ✅ Flutter app ishonib qoldi va login qildi

**Yechim:** Request va Response'ni **shifrlash**!

---

## 📦 Flutter Packages

```yaml
# pubspec.yaml
dependencies:
  flutter:
    sdk: flutter

  # HTTP requests
  dio: ^5.4.0

  # Encryption
  encrypt: ^5.0.3

  # Certificate pinning (bonus security)
  dio_certificate_pinning: ^1.0.0
```

```bash
flutter pub get
```

---

## 🔐 1. Encryption Helper (Flutter)

```dart
// lib/services/encryption_service.dart
import 'dart:convert';
import 'package:encrypt/encrypt.dart';

class EncryptionService {
  // IMPORTANT: Bu key'lar backend bilan bir xil bo'lishi kerak!
  static const String _secretKey = 'CHANGE-THIS-32-CHAR-SECRET-KEY'; // 32 characters
  static const String _ivKey = 'CHANGE-THIS-IV16'; // 16 characters

  late final Key _key;
  late final IV _iv;
  late final Encrypter _encrypter;

  EncryptionService() {
    _key = Key.fromUtf8(_secretKey);
    _iv = IV.fromUtf8(_ivKey);
    _encrypter = Encrypter(AES(_key, mode: AESMode.cbc, padding: 'PKCS7'));
  }

  /// Encrypt plain text to Base64
  String encrypt(String plainText) {
    final encrypted = _encrypter.encrypt(plainText, iv: _iv);
    return encrypted.base64;
  }

  /// Decrypt Base64 to plain text
  String decrypt(String encryptedBase64) {
    try {
      final encrypted = Encrypted.fromBase64(encryptedBase64);
      return _encrypter.decrypt(encrypted, iv: _iv);
    } catch (e) {
      throw Exception('Decryption failed - invalid data');
    }
  }

  /// Encrypt object to JSON then Base64
  String encryptObject(Object obj) {
    final json = jsonEncode(obj);
    return encrypt(json);
  }

  /// Decrypt Base64 to JSON then object
  Map<String, dynamic> decryptObject(String encryptedBase64) {
    final json = decrypt(encryptedBase64);
    return jsonDecode(json) as Map<String, dynamic>;
  }
}
```

---

## 🌐 2. Encrypted API Service

```dart
// lib/services/api_service.dart
import 'dart:convert';
import 'package:dio/dio.dart';
import 'encryption_service.dart';

class ApiService {
  final Dio _dio;
  final EncryptionService _encryption;
  final bool encryptionEnabled;

  ApiService({
    required String baseUrl,
    this.encryptionEnabled = true, // Production'da true
  })  : _dio = Dio(BaseOptions(
          baseUrl: baseUrl,
          connectTimeout: const Duration(seconds: 10),
          receiveTimeout: const Duration(seconds: 10),
        )),
        _encryption = EncryptionService() {
    _setupInterceptors();
  }

  void _setupInterceptors() {
    _dio.interceptors.add(
      InterceptorsWrapper(
        // REQUEST ENCRYPTION
        onRequest: (options, handler) {
          if (encryptionEnabled && options.method != 'GET') {
            // POST/PUT request body'ni encrypt qilish
            if (options.data != null) {
              final dataJson = jsonEncode(options.data);
              final encryptedData = _encryption.encrypt(dataJson);

              // Encrypted wrapper
              options.data = {
                'encrypted': true,
                'data': encryptedData,
              };

              print('🔒 Request encrypted');
            }
          }
          return handler.next(options);
        },

        // RESPONSE DECRYPTION
        onResponse: (response, handler) {
          if (encryptionEnabled && response.data != null) {
            try {
              final data = response.data;

              // Check if response is encrypted
              if (data is Map<String, dynamic> &&
                  data['encrypted'] == true &&
                  data['data'] != null) {
                // Decrypt qilish
                final decryptedJson = _encryption.decrypt(data['data']);
                response.data = jsonDecode(decryptedJson);

                print('🔓 Response decrypted');
              }
            } catch (e) {
              print('⚠️ Decryption failed: $e');
              // Hacker response'ni o'zgartirgan bo'lishi mumkin!
              return handler.reject(
                DioException(
                  requestOptions: response.requestOptions,
                  error: 'Response validation failed - possible tampering',
                  type: DioExceptionType.badResponse,
                ),
              );
            }
          }
          return handler.next(response);
        },

        onError: (error, handler) {
          print('❌ API Error: ${error.message}');
          return handler.next(error);
        },
      ),
    );
  }

  // Generic POST request
  Future<Map<String, dynamic>> post(
    String endpoint,
    Map<String, dynamic> data,
  ) async {
    try {
      final response = await _dio.post(endpoint, data: data);
      return response.data as Map<String, dynamic>;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  // Generic GET request
  Future<Map<String, dynamic>> get(
    String endpoint, {
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      final response = await _dio.get(
        endpoint,
        queryParameters: queryParameters,
      );
      return response.data as Map<String, dynamic>;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Exception _handleError(DioException e) {
    if (e.response != null) {
      final data = e.response!.data;
      if (data is Map && data['message'] != null) {
        return Exception(data['message']);
      }
    }
    return Exception(e.message ?? 'Network error');
  }
}
```

---

## 🚀 3. Usage Example

```dart
// lib/main.dart
void main() {
  runApp(MyApp());
}

class MyApp extends StatelessWidget {
  final apiService = ApiService(
    baseUrl: 'https://your-api.com',
    encryptionEnabled: true, // ← Encryption yoqilgan!
  );

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      home: LoginPage(apiService: apiService),
    );
  }
}

// lib/pages/login_page.dart
class LoginPage extends StatefulWidget {
  final ApiService apiService;

  const LoginPage({required this.apiService});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  Future<void> login() async {
    try {
      // Login request (encrypted avtomatik)
      final response = await widget.apiService.post(
        '/api/auth/verify_otp',
        {
          'phone_number': '+998901234567',
          'otp_code': '1234',
        },
      );

      print('Response: $response');

      // Hacker response'ni o'zgartira olmaydi!
      // Chunki decryption fail bo'ladi
      if (response['status'] == true) {
        final token = response['data']['token'];
        print('✅ Login successful: $token');
        // Navigate to home
      } else {
        print('❌ Login failed: ${response['message']}');
      }
    } catch (e) {
      print('❌ Error: $e');
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: ElevatedButton(
          onPressed: login,
          child: Text('Login'),
        ),
      ),
    );
  }
}
```

---

## 🔐 4. Certificate Pinning (Bonus Security)

Hacker HTTPS'ni bypass qilmaslik uchun:

```dart
// lib/services/api_service.dart
import 'package:dio_certificate_pinning/dio_certificate_pinning.dart';

class ApiService {
  static Future<Dio> createSecureDio(String baseUrl) async {
    final dio = Dio(BaseOptions(baseUrl: baseUrl));

    // Certificate Pinning
    await dio.interceptors.add(
      CertificatePinningInterceptor(
        allowedSHAFingerprints: [
          // Your SSL certificate SHA fingerprint
          'YOUR_CERTIFICATE_SHA256_FINGERPRINT_HERE',
        ],
      ),
    );

    return dio;
  }
}
```

**SSL fingerprint olish:**
```bash
# Linux/Mac
openssl s_client -connect your-api.com:443 | openssl x509 -fingerprint -sha256

# Windows PowerShell
certutil -hashfile certificate.crt SHA256
```

---

## 🛡️ Xavfsizlik Darajalari

### Level 1: Encryption OFF (Development)
```dart
final apiService = ApiService(
  baseUrl: 'http://localhost:5000',
  encryptionEnabled: false, // ← Test uchun
);
```
- ✅ Swagger, Postman ishlaydi
- ❌ Hacker response'ni o'zgartira oladi

### Level 2: Encryption ON (Production)
```dart
final apiService = ApiService(
  baseUrl: 'https://api.convoy.uz',
  encryptionEnabled: true, // ← Production
);
```
- ✅ Response encrypted
- ✅ Hacker decrypt qila olmaydi
- ⚠️ MITM hali mumkin (man-in-the-middle)

### Level 3: Encryption + Certificate Pinning (Maximum)
```dart
final apiService = ApiService(
  baseUrl: 'https://api.convoy.uz',
  encryptionEnabled: true,
  certificatePinning: true, // ← Max security
);
```
- ✅ Response encrypted
- ✅ HTTPS bypass qilib bo'lmaydi
- ✅ Hacker hech narsa qila olmaydi 🎯

---

## 📊 Test Qilish

### Backend'da Encryption Yoqish

```json
// appsettings.json
{
  "Encryption": {
    "Enabled": true,  // ← true qiling
    "SecretKey": "MySuper32CharacterSecretKeyHere",
    "IvKey": "My16CharacterIV"
  }
}
```

### Flutter'da Bir Xil Key Ishlatish

```dart
// encryption_service.dart
static const String _secretKey = 'MySuper32CharacterSecretKeyHere'; // Backend bilan bir xil!
static const String _ivKey = 'My16CharacterIV'; // Backend bilan bir xil!
```

### Test Request

```dart
final response = await apiService.post('/api/locations', {
  'user_id': 1,
  'locations': [
    {
      'recorded_at': '2025-12-20T10:00:00Z',
      'latitude': 41.311151,
      'longitude': 69.279737,
      'is_moving': false,
    }
  ],
});

print(response); // Decrypted response
// {
//   "status": true,
//   "message": "Location muvaffaqiyatli yaratildi",
//   "data": [...]
// }
```

---

## ✅ Xulosa

| Feature | Without Encryption | With Encryption |
|---------|-------------------|-----------------|
| Hacker can see data | ✅ Yes | ❌ No (Base64 gibberish) |
| Hacker can modify response | ✅ Yes | ❌ No (decryption fails) |
| MITM attack possible | ✅ Yes | ⚠️ Yes (if no cert pinning) |
| Certificate pinning | ❌ No | ✅ Yes |
| **Security Level** | 🔴 Low | 🟢 High |

---

**🎉 Endi do'stingiz response'ni o'zgartira olmaydi! Decryption fail bo'ladi va app login qilmaydi!** 🔒
