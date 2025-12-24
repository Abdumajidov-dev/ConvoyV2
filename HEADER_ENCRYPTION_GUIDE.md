# Header Encryption Guide

## Overview

Header'lar ham request body kabi shifrlangan holda yuboriladi. Backend avtomatik ravishda decrypt qiladi.

---

## Backend - Qaysi Header'lar Decrypt Qilinadi?

Backend quyidagi header'larni decrypt qiladi:

1. `device-info` - Qurilma ma'lumotlari
2. `Authorization` - JWT token (agar shifrlangan bo'lsa)
3. `X-Custom-Data` - Maxsus ma'lumotlar

**ESLATMA**: `device-type` header'i **shifrlangan emas** - oddiy text.

---

## Flutter - Header Encryption

### Encryption Funksiyasi

```dart
/// Header qiymatini shifrlash (URL-safe Base64)
String encryptHeader(String plainText) {
  // 🔐 KEY (Backend bilan bir xil)
  final keyBytes = base64Decode(
      "DMyx68VCO2K8IYpGjz6nlEn+AeXj/FEC1dVhUiWTJyQ="
  );
  final key = encrypt.Key(keyBytes);

  // 🔥 RANDOM IV (har request uchun yangi)
  final iv = encrypt.IV.fromSecureRandom(16);

  final encrypter = encrypt.Encrypter(
    encrypt.AES(key, mode: encrypt.AESMode.cbc, padding: 'PKCS7'),
  );

  // 🔐 ENCRYPT
  final encrypted = encrypter.encrypt(plainText, iv: iv);

  // 📌 IV + CIPHERTEXT
  final combined = <int>[
    ...iv.bytes,
    ...encrypted.bytes,
  ];

  // ✅ URL-SAFE BASE64 (MUHIM!)
  return base64UrlEncode(combined);
}
```

---

## Flutter - Interceptor Implementation

### Request Interceptor (Headers bilan)

```dart
@override
Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
) async {
  try {
    if (UserData.deviceInfo.isEmpty) await getDeviceInfo();

    // ================= SHIFRLANGAN HEADERLAR =================

    // 1️⃣ device-info (SHIFRLANGAN)
    final deviceInfoJson = jsonEncode(UserData.deviceInfo);
    final encryptedDeviceInfo = encryptHeader(deviceInfoJson);

    // 2️⃣ Authorization (SHIFRLANGAN)
    // Token shifrlangan holda yuboriladi
    String? encryptedAuth;
    if (UserData.token.isNotEmpty) {
      encryptedAuth = encryptHeader(UserData.token);
    }

    options.headers.addAll({
      // ✅ SHIFRLANGAN HEADERLAR
      "device-info": encryptedDeviceInfo,
      if (encryptedAuth != null)
        HttpHeaders.authorizationHeader: 'Bearer $encryptedAuth',

      // ❌ ODDIY HEADERLAR (shifrlangan emas)
      "device_type": Platform.isIOS ? "IOS" : "Android",
    });

    // ================= BODY ENCRYPTION =================
    if (options.data != null &&
        (options.method == 'POST' ||
            options.method == 'PUT' ||
            options.method == 'PATCH')) {

      final keyBytes = base64Decode(
          "DMyx68VCO2K8IYpGjz6nlEn+AeXj/FEC1dVhUiWTJyQ="
      );
      final key = encrypt.Key(keyBytes);
      final iv = encrypt.IV.fromSecureRandom(16);

      final encrypter = encrypt.Encrypter(
        encrypt.AES(key, mode: encrypt.AESMode.cbc, padding: 'PKCS7'),
      );

      final plainText = jsonEncode(options.data);
      final encrypted = encrypter.encrypt(plainText, iv: iv);

      final combined = <int>[
        ...iv.bytes,
        ...encrypted.bytes,
      ];

      // ✅ URL-SAFE BASE64
      options.data = base64UrlEncode(combined);
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

---

## Flutter - Helper Function

```dart
import 'dart:convert';
import 'dart:io';
import 'package:encrypt/encrypt.dart' as encrypt;

class EncryptionHelper {
  static final _keyBytes = base64Decode(
      "DMyx68VCO2K8IYpGjz6nlEn+AeXj/FEC1dVhUiWTJyQ="
  );
  static final _key = encrypt.Key(_keyBytes);

  /// Header yoki body'ni shifrlash
  static String encryptData(String plainText) {
    final iv = encrypt.IV.fromSecureRandom(16);

    final encrypter = encrypt.Encrypter(
      encrypt.AES(_key, mode: encrypt.AESMode.cbc, padding: 'PKCS7'),
    );

    final encrypted = encrypter.encrypt(plainText, iv: iv);

    final combined = <int>[
      ...iv.bytes,
      ...encrypted.bytes,
    ];

    return base64UrlEncode(combined);
  }

  /// Shifrlangan ma'lumotni decrypt qilish
  static String decryptData(String encryptedData) {
    final combined = base64UrlDecode(encryptedData);

    final iv = encrypt.IV(Uint8List.fromList(combined.sublist(0, 16)));
    final encryptedBytes = combined.sublist(16);

    final encrypter = encrypt.Encrypter(
      encrypt.AES(_key, mode: encrypt.AESMode.cbc, padding: 'PKCS7'),
    );

    final encrypted = encrypt.Encrypted(Uint8List.fromList(encryptedBytes));
    return encrypter.decrypt(encrypted, iv: iv);
  }
}
```

---

## Flutter - Usage Example

```dart
// Interceptor'da ishlatish
@override
Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
) async {
  // Device info shifrlash
  final deviceInfo = {"model": "iPhone 13", "os": "iOS 15"};
  options.headers["device-info"] = EncryptionHelper.encryptData(
      jsonEncode(deviceInfo)
  );

  // Token shifrlash (agar bor bo'lsa)
  if (UserData.token.isNotEmpty) {
    final encryptedToken = EncryptionHelper.encryptData(UserData.token);
    options.headers[HttpHeaders.authorizationHeader] = 'Bearer $encryptedToken';
  }

  // Body shifrlash
  if (options.data != null) {
    options.data = EncryptionHelper.encryptData(
        jsonEncode(options.data)
    );
    options.headers['Content-Type'] = 'text/plain';
  }

  handler.next(options);
}
```

---

## Backend - DecryptHeaders Method

Backend avtomatik decrypt qiladi:

```csharp
private void DecryptHeaders(HttpContext context, IEncryptionService encryptionService)
{
    var headersToDecrypt = new[] { "device-info", "Authorization", "X-Custom-Data" };

    foreach (var headerName in headersToDecrypt)
    {
        if (!context.Request.Headers.ContainsKey(headerName))
            continue;

        var encryptedValue = context.Request.Headers[headerName].ToString();

        // Authorization "Bearer " prefixi bilan keladi
        var valueToDecrypt = encryptedValue;
        var hasBearer = false;

        if (headerName == "Authorization" &&
            encryptedValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            valueToDecrypt = encryptedValue.Substring(7);
            hasBearer = true;
        }

        try
        {
            var decryptedValue = encryptionService.Decrypt(valueToDecrypt);

            context.Request.Headers.Remove(headerName);

            if (hasBearer)
            {
                context.Request.Headers.Append(headerName, $"Bearer {decryptedValue}");
            }
            else
            {
                context.Request.Headers.Append(headerName, decryptedValue);
            }
        }
        catch (Exception ex)
        {
            // Decrypt qilib bo'lmasa, original qiymatni qoldirish
            _logger.LogWarning(ex, "Failed to decrypt header: {Header}", headerName);
        }
    }
}
```

---

## Test Qilish

### Python Script (Test uchun)

```python
import base64
import json
from Crypto.Cipher import AES
from Crypto.Util.Padding import pad
from Crypto.Random import get_random_bytes

KEY_BASE64 = "DMyx68VCO2K8IYpGjz6nlEn+AeXj/FEC1dVhUiWTJyQ="
key = base64.b64decode(KEY_BASE64)

def encrypt_header(plain_text):
    iv = get_random_bytes(16)
    cipher = AES.new(key, AES.MODE_CBC, iv)
    encrypted = cipher.encrypt(pad(plain_text.encode('utf-8'), AES.block_size))
    combined = iv + encrypted
    return base64.urlsafe_b64encode(combined).decode()

# Device info shifrlash
device_info = {"model": "Test Device", "os": "Android 12"}
encrypted_device_info = encrypt_header(json.dumps(device_info))

print(f"Encrypted device-info: {encrypted_device_info}")

# curl bilan test
print(f'\ncurl -X POST http://localhost:5084/api/auth/verify_number \\')
print(f'  -H "Content-Type: text/plain" \\')
print(f'  -H "device-info: {encrypted_device_info}" \\')
print(f'  -H "device_type: Android" \\')
print(f'  --data-raw "..."')
```

---

## Muhim Eslatmalar

1. ✅ **URL-safe Base64** ishlatish (`base64UrlEncode`/`base64UrlDecode`)
2. ✅ **Random IV** har request uchun yangi
3. ✅ **IV + ciphertext** formatida yuborish
4. ✅ `Authorization` header `Bearer ` prefixi bilan yuboriladi
5. ✅ `device-type` header **shifrlangan emas** (oddiy text)
6. ✅ Backend avtomatik decrypt qiladi

---

## Xatoliklarni Bartaraf Etish

### Header decrypt qilinmayapti

**Log'larda qidiring**:
```
🔐 Attempting to decrypt header: device-info
✅ Header decrypted: device-info = {"model":"..."}
```

Agar ko'rinmasa:
- Header nomi to'g'rimi? (`device-info`, `Authorization`)
- Header qiymati URL-safe Base64 formatidami?
- IV + ciphertext to'g'ri formatdami?

### Authorization token noto'g'ri

**Tekshirish**:
```
Authorization: Bearer <encrypted_token>
```

- `Bearer ` prefixi bormi?
- Token URL-safe Base64 formatidami?
- Backend `Bearer ` ni olib tashlab decrypt qiladi

---

## Qisqacha Xulosa

1. Flutter: `device-info` va `Authorization` header'larni `EncryptionHelper.encryptData()` orqali shifrlang
2. Backend: Avtomatik decrypt qiladi (`DecryptHeaders` metodi)
3. URL-safe Base64 ishlatish **MAJBURIY**!
4. Har request uchun yangi random IV
