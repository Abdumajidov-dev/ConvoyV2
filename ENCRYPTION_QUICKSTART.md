# Encryption Quick Start Guide

## What is this?

End-to-end AES-256-CBC encryption for request/response data between Flutter app and C# backend with **random IV per request** for maximum security.

---

## Setup (Backend - C#)

### 1. Generate Encryption Key

```powershell
# Windows
powershell -ExecutionPolicy Bypass -File generate-encryption-keys.ps1

# Linux/Mac
bash generate-encryption-keys.sh
```

**NOTE**: IV endi configuration'da emas! Har bir request uchun avtomatik random generate qilinadi.

### 2. Add Key to appsettings.json

```json
{
  "Encryption": {
    "Enabled": true,
    "Key": "DMyx68VCO2K8IYpGjz6nlEn+AeXj/FEC1dVhUiWTJyQ="
  }
}
```

**IMPORTANT**: `IV` ni configuration'ga qo'shish shart emas! Backend har safar random IV yaratadi.

### 3. Done! Middleware automatically encrypts/decrypts

---

## Setup (Flutter - Dart)

### 1. Add Dependency

```yaml
# pubspec.yaml
dependencies:
  encrypt: ^5.0.3
  http: ^1.1.0
```

### 2. Create Encryption Service

```dart
import 'dart:convert';
import 'dart:typed_data';
import 'package:encrypt/encrypt.dart' as encrypt;

class EncryptionService {
  // SAME key from backend! (appsettings.json)
  static const String _keyBase64 = 'DMyx68VCO2K8IYpGjz6nlEn+AeXj/FEC1dVhUiWTJyQ=';

  late final encrypt.Key _key;
  late final encrypt.Encrypter _encrypter;

  EncryptionService() {
    // Key ni Base64 dan decode qilish
    final keyBytes = base64.decode(_keyBase64);
    _key = encrypt.Key(Uint8List.fromList(keyBytes));

    _encrypter = encrypt.Encrypter(
      encrypt.AES(
        _key,
        mode: encrypt.AESMode.cbc,
        padding: 'PKCS7',
      ),
    );
  }

  // Encrypt with random IV
  String encryptData(String text) {
    // Random IV generate qilish
    final iv = encrypt.IV.fromSecureRandom(16);

    // Encrypt
    final encrypted = _encrypter.encrypt(text, iv: iv);

    // IV + encrypted data ni birlashtirib Base64 ga o'tkazish
    final combinedBytes = Uint8List.fromList([
      ...iv.bytes,
      ...encrypted.bytes,
    ]);

    return base64.encode(combinedBytes);
  }

  // Decrypt with extracted IV
  String decryptData(String cipherText) {
    // Base64 dan decode
    final combinedBytes = base64.decode(cipherText.trim());

    // IV ni ajratish (birinchi 16 bytes)
    final ivBytes = combinedBytes.sublist(0, 16);
    final iv = encrypt.IV(Uint8List.fromList(ivBytes));

    // Encrypted data (16-dan keyin)
    final encryptedBytes = combinedBytes.sublist(16);
    final encrypted = encrypt.Encrypted(Uint8List.fromList(encryptedBytes));

    // Decrypt
    return _encrypter.decrypt(encrypted, iv: iv);
  }
}
```

### 3. Use in API Calls

```dart
import 'dart:convert';
import 'package:http/http.dart' as http;

class ApiService {
  final _encryption = EncryptionService();

  Future<Map<String, dynamic>> post(String url, Map<String, dynamic> body) async {
    // 1. Encrypt request with random IV
    final encrypted = _encryption.encryptData(jsonEncode(body));

    // 2. Send as plain text (NOT JSON)
    final response = await http.post(
      Uri.parse(url),
      headers: {'Content-Type': 'text/plain'},
      body: encrypted,  // Direct encrypted string (contains IV + data)
    );

    // 3. Decrypt response (extracts IV automatically)
    final decrypted = _encryption.decryptData(response.body);
    return jsonDecode(decrypted);
  }
}
```

---

## How It Works

### Request Flow (Flutter → Backend)

1. **Flutter**: Generate random IV → Encrypt JSON → Prepend IV → Base64 encode
2. **Backend Middleware**: Base64 decode → Extract IV → Decrypt → Process
3. **Backend**: Generate random IV → Encrypt response → Prepend IV → Base64 encode
4. **Flutter**: Base64 decode → Extract IV → Decrypt → Use data

### Data Format

**Encrypted data structure**:
```
[IV (16 bytes)][Encrypted Data (variable)] → Base64 String
```

### Example

**Original Request (Flutter)**:
```json
{
  "phone_number": "+998901234567",
  "otp_code": "1234"
}
```

**Request 1 (on network)** - Random IV, different each time:
```
K8pQ7xZmN3vL2qP5Yw1Rt9... (IV: K8pQ7xZmN3vL2qP5)
```

**Request 2 (same data!)** - Different random IV:
```
Mx3zLw9Yr2qP8Kj4Hs6Vn2... (IV: Mx3zLw9Yr2qP8Kj4)
```

**Why different?** Random IV har safar! Attacker bir xil ma'lumotni taniy olmaydi.

**Response (on network)** - Also has random IV:
```
Qw7Xt2Mn9Ls4Pz8Yv3Kj1R... (IV + encrypted response)
```

**Decrypted Response (Flutter)**:
```json
{
  "status": true,
  "message": "Success",
  "data": {"token": "eyJhbGc..."}
}
```

---

## Development Mode (Disable Encryption)

### Backend
```json
{
  "Encryption": {
    "Enabled": false  // ← Disable for testing
  }
}
```

### Flutter
```dart
final apiService = ApiService(encryptionEnabled: false);
```

---

## Security Checklist

- [x] Keys generated with cryptographically secure RNG
- [x] AES-256-CBC with PKCS7 padding
- [x] **Random IV per request** (maximum security)
- [x] IV prepended to encrypted data (no separate transmission)
- [x] Keys stored in environment variables (production)
- [x] Keys added to .gitignore
- [x] HTTPS enabled (TLS + Encryption = double protection)
- [x] Different keys for Dev/Staging/Production

---

## Troubleshooting

### "Decryption failed"
→ Check keys match exactly between backend and Flutter
→ Ensure IV is being prepended correctly (first 16 bytes)

### "Invalid padding"
→ Ensure both use AES-256-CBC with PKCS7
→ Check that IV extraction logic is correct

### "Invalid encrypted data: too short to contain IV"
→ Encrypted data must be at least 16 bytes (IV size)
→ Check that backend is prepending IV correctly

### "Keys not found"
→ Run generate-encryption-keys.ps1 first
→ Only Key is needed now, IV is auto-generated

### "Same encrypted data for same input"
→ This should NOT happen! Check that random IV is being generated
→ Verify `aes.GenerateIV()` is called in C#
→ Verify `encrypt.IV.fromSecureRandom(16)` is called in Flutter

---

## Files

- `Convoy.Service/Services/EncryptionService.cs` - C# encryption service
- `Convoy.Api/Middleware/EncryptionMiddleware.cs` - Auto encrypt/decrypt middleware
- `generate-encryption-keys.ps1` - Key generator (Windows)
- `generate-encryption-keys.sh` - Key generator (Linux/Mac)
- `FLUTTER_ENCRYPTION_GUIDE.md` - Complete Flutter guide
- `.gitignore` - Prevents committing keys

---

## Need Help?

See `FLUTTER_ENCRYPTION_GUIDE.md` for complete examples.
