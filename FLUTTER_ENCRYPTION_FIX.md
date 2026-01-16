# Flutter Encryption Fix - URL-Safe Base64 + Header Encryption

## Muammo

Flutter'dan shifrlangan ma'lumot yuborilganda backend **415 Unsupported Media Type** yoki **decryption error** qaytarayapti.

**YANGI**: Header'lar ham shifrlangan holda yuboriladi (`device-info`, `Authorization`).

**Qo'shimcha qo'llanma**: `HEADER_ENCRYPTION_GUIDE.md` faylini o'qing.

**Sababi**: Standard Base64 encoding `+` va `/` belgilarini ishlatadi. HTTP request'larda bu belgilar buziladi:
- `+` → space (bo'sh joy) ga aylanadi
- `/` → ba'zan escape kerak bo'ladi

## Yechim: URL-Safe Base64 Encoding

Dart'da `base64UrlEncode` ishlatish kerak (standart `base64Encode` emas).

---

## TO'G'RI FLUTTER KODI

### Import qo'shish

```dart
import 'dart:convert';
import 'package:encrypt/encrypt.dart' as encrypt;
import 'package:dio/dio.dart';
```

### Interceptor - Request Encryption

```dart
@override
Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
) async {
  try {
    if (UserData.deviceInfo.isEmpty) await getDeviceInfo();

    // ================= HEADERLAR =================
    options.headers.addAll({
      HttpHeaders.authorizationHeader: 'Bearer ${UserData.token}',
      "device_type": Platform.isIOS ? "IOS" : "Android",
      "device-info": jsonEncode(UserData.deviceInfo),
    });

    // ================= BODY BORMI? =================
    if (options.data != null &&
        (options.method == 'POST' ||
            options.method == 'PUT' ||
            options.method == 'PATCH')) {

      // 🔐 KEY (Backend bilan bir xil)
      final keyBytes = base64Decode(
          "DMyx68VCO2K8IYpGjz6nlEn+AeXj/FEC1dVhUiWTJyQ="
      );
      final key = encrypt.Key(keyBytes);

      // 🔐 RANDOM IV (har request uchun yangi)
      final iv = encrypt.IV.fromSecureRandom(16);

      final encrypter = encrypt.Encrypter(
        encrypt.AES(key, mode: encrypt.AESMode.cbc, padding: 'PKCS7'),
      );

      // 📦 JSON → STRING
      final plainText = jsonEncode(options.data);

      // 🔐 ENCRYPT
      final encrypted = encrypter.encrypt(
        plainText,
        iv: iv,
      );

      // 📌 IV + CIPHERTEXT
      final combined = <int>[
        ...iv.bytes,
        ...encrypted.bytes,
      ];

      // ✅ URL-SAFE BASE64 ENCODING (MUHIM!)
      options.data = base64UrlEncode(combined);

      // Content-Type
      options.headers['Content-Type'] = 'text/plain';
    }

    options.path = options.path.replaceAll(RegExp(r'/[{][A-Za-z_]+\}+'), '');
    handler.next(options);
  } catch (e) {
    print("❌ Request encryption error: $e");
    handler.next(options);
  }
}
```

### Response Decryption (Interceptor)

```dart
@override
Future<void> onResponse(
    Response response,
    ResponseInterceptorHandler handler,
) async {
  try {
    // Agar response text/plain bo'lsa, bu encrypted data
    if (response.headers.value('content-type')?.contains('text/plain') == true) {
      final encryptedData = response.data.toString().trim();

      // 🔐 KEY
      final keyBytes = base64Decode(
          "DMyx68VCO2K8IYpGjz6nlEn+AeXj/FEC1dVhUiWTJyQ="
      );
      final key = encrypt.Key(keyBytes);

      // ✅ URL-SAFE BASE64 DECODING
      final combined = base64UrlDecode(encryptedData);

      // IV (birinchi 16 byte)
      final iv = encrypt.IV(Uint8List.fromList(combined.sublist(0, 16)));

      // Encrypted data (qolgan qism)
      final encryptedBytes = combined.sublist(16);

      // DECRYPT
      final encrypter = encrypt.Encrypter(
        encrypt.AES(key, mode: encrypt.AESMode.cbc, padding: 'PKCS7'),
      );

      final encrypted = encrypt.Encrypted(Uint8List.fromList(encryptedBytes));
      final decrypted = encrypter.decrypt(encrypted, iv: iv);

      // JSON parse
      response.data = jsonDecode(decrypted);
    }

    handler.next(response);
  } catch (e) {
    print("❌ Response decryption error: $e");
    handler.next(response);
  }
}
```

---

## O'ZGARISHLAR

### ❌ ESKI (XATO):
```dart
// NOTO'G'RI - Standard Base64
options.data = base64Encode(combined);

// NOTO'G'RI - Response decode
final combined = base64Decode(encryptedData);
```

### ✅ YANGI (TO'G'RI):
```dart
// ✅ TO'G'RI - URL-safe Base64
options.data = base64UrlEncode(combined);

// ✅ TO'G'RI - URL-safe decode
final combined = base64UrlDecode(encryptedData);
```

---

## FARQI NIMA?

| Standard Base64 | URL-Safe Base64 |
|----------------|-----------------|
| `+` belgisi    | `-` belgisi     |
| `/` belgisi    | `_` belgisi     |
| HTTP'da buziladi | HTTP'da xavfsiz |

### Misol:

```
STANDARD: XxVYyekF42zJO16k+CdUCpHRqmPD/9vXzlCa1rlnNAO...
          ~~~~~~~~~~~~~~~~~~~^~~~~~~^~~~~~~~~~~~~~~~~~
URL-SAFE: XxVYyekF42zJO16k-CdUCpHRqmPD_9vXzlCa1rlnNAO...
          ~~~~~~~~~~~~~~~~~~~^~~~~~~^~~~~~~~~~~~~~~~~~
                            ✅      ✅
```

---

## BACKEND QISMI

Backend **avtomatik** ravishda ikkala formatni ham qabul qiladi:
- Standard Base64 (`+` va `/`)
- URL-safe Base64 (`-` va `_`)

Siz faqat Flutter tarafda `base64UrlEncode`/`base64UrlDecode` ishlatishingiz kerak.

---

## TEST QILISH

### 1. Encryption Test Endpoint

```dart
final dio = Dio();
dio.interceptors.add(YourEncryptionInterceptor());

// Test request
final response = await dio.post(
  'http://your-server:5084/api/encryption-test/echo',
  data: {
    'message': 'Hello',
    'phone_number': '916714835',
  },
);

print(response.data); // Decrypted response
```

### 2. Auth Test

```dart
final response = await dio.post(
  'http://your-server:5084/api/auth/send_otp',
  data: {
    'phone_number': '+998901234567',
  },
);

print(response.data); // { status: true, message: "...", data: null }
```

---

## XATOLIKLARNI BARTARAF ETISH

### 415 Unsupported Media Type
- **Sabab**: `Content-Type` header noto'g'ri
- **Yechim**: `options.headers['Content-Type'] = 'text/plain';`

### Invalid Base64 format
- **Sabab**: `base64Encode` ishlatilgan (standard, `+` va `/` bor)
- **Yechim**: `base64UrlEncode` ishlating (URL-safe, `-` va `_`)

### Decryption failed
- **Sabab**: Key yoki IV mos kelmayapti
- **Yechim**: Backend'dagi key bilan bir xil key ishlatganingizni tekshiring

### Response decrypt qila olmayapti
- **Sabab**: Response `text/plain` emas, `application/json` bo'lishi mumkin
- **Yechim**: Backend encryption yoqilganini tekshiring (`Encryption:Enabled: true`)

---

## QISQACHA

1. ✅ `base64Encode` → `base64UrlEncode` ga o'zgartiring
2. ✅ `base64Decode` → `base64UrlDecode` ga o'zgartiring
3. ✅ Backend avtomatik ravishda ikkala formatni ham qabul qiladi
4. ✅ `Content-Type: text/plain` headerini qo'shing

**Shundan keyin 415 error va decryption muammolari hal bo'ladi!**

---

## BACKEND CHANGES (Already Done)

Backend `EncryptionService.cs` yangilandi va endi:
- ✅ Standard Base64 qabul qiladi (`+` va `/`)
- ✅ URL-safe Base64 qabul qiladi (`-` va `_`)
- ✅ Padding avtomatik qo'shiladi

Siz hech narsa o'zgartirmasangiz ham ishaydi, lekin **URL-safe** ishlatish **tavsiya etiladi**.
