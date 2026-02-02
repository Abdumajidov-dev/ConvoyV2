# Firebase Credentials Deployment Guide

## Overview

Firebase Cloud Messaging (FCM) notification yuborish uchun credentials file kerak. Bu file **SECRET** hisoblanadi va Git'ga commit qilinmasligi kerak.

## ✅ RECOMMENDED: Base64 Environment Variable

Bu usul **eng oson va xavfsiz** - Docker image'da hech qanday secret yo'q, faqat Railway environment variable orqali beriladi.

### Step 1: Firebase Credentials File'ni Base64'ga O'girish

**Linux/Mac:**
```bash
base64 -w 0 firebase-adminsdk.json
```

**Windows PowerShell:**
```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("firebase-adminsdk.json"))
```

**Windows CMD:**
```cmd
certutil -encode firebase-adminsdk.json firebase-base64.txt
# Ochilib file ichidan header/footer'ni o'chiring, faqat Base64 qolsin
```

Output'ni copy qiling (bitta uzun string bo'ladi).

### Step 2: Railway Environment Variable Qo'shish

Railway dashboard'da:
1. **Variables** tab'ga o'ting
2. **New Variable** tugmasini bosing
3. Quyidagicha kiriting:

```
Variable name:  FIREBASE_CREDENTIALS_BASE64
Variable value: <paste_base64_string_here>
```

4. **Add** tugmasini bosing
5. Deployment qayta ishga tushadi

### Step 3: Verify

Deployment log'ida quyidagi xabarni ko'rishingiz kerak:

```
[INFO] Loading Firebase credentials from FIREBASE_CREDENTIALS_BASE64 environment variable
[INFO] ✅ Firebase Admin SDK initialized from Base64 environment variable
```

---

## Alternative: Local Development (File Path)

Local development'da file path ishlatish mumkin:

1. Firebase Console'dan Service Account Key yuklab oling
2. File'ni project root'ga joylashtiring: `firebase-adminsdk.json`
3. API ishga tushganda avtomatik topib ishlatadi

**Optional**: Custom path ishlatish:
```bash
export FIREBASE_CREDENTIALS_PATH=/custom/path/firebase-adminsdk.json
dotnet run --project Convoy.Api
```

## Code Changes

### NotificationService.cs (Lines 30-47)

```csharp
// Initialize Firebase Admin SDK (agar hali initialize qilinmagan bo'lsa)
if (FirebaseApp.DefaultInstance == null)
{
    try
    {
        // FIXED: Environment variable orqali path olish
        var credentialsPath = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_PATH")
            ?? "firebase-adminsdk.json";

        // File mavjudligini tekshirish
        if (!File.Exists(credentialsPath))
        {
            _logger.LogWarning("Firebase credentials file not found at: {Path}. Firebase notifications disabled.",
                credentialsPath);
            return;
        }

        var credential = GoogleCredential.FromFile(credentialsPath);
        FirebaseApp.Create(new AppOptions
        {
            Credential = credential
        });
        _logger.LogInformation("Firebase Admin SDK initialized successfully from: {Path}", credentialsPath);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Firebase Admin SDK initialization failed");
    }
}
```

### Dockerfile (Lines 95-98)

```dockerfile
# Copy Firebase credentials (optional - agar yo'q bo'lsa skip qiladi)
# Railway deployment'da FIREBASE_CREDENTIALS_PATH environment variable ishlatiladi
COPY firebase-adminsdk.json* ./ || true
```

## Behavior

### With Credentials
- ✅ Firebase Admin SDK initialized successfully
- ✅ Push notifications yuboriladi
- ✅ Background service user offline notifications yuboradi

### Without Credentials
- ⚠️ Warning logged: "Firebase credentials file not found"
- ⚠️ Firebase notifications disabled
- ✅ API ishlashda davom etadi (notification xususiyati o'chirilgan holda)
- ✅ Background service boshqa funksiyalar ishlaydi

## Security Notes

1. **NEVER** commit `firebase-adminsdk.json` to Git
2. `.gitignore` file'da mavjud: `firebase-adminsdk.json`, `**/firebase-*.json`
3. Railway'da environment variable ishlatish RECOMMENDED
4. Credentials file server'da faqat root/admin kirishi mumkin bo'lgan joyda saqlang

## Troubleshooting

### Log'da "Firebase credentials file not found" ko'rsatiladi
- ✅ Normal: Credentials yo'q bo'lsa, notification feature disabled
- ❌ Agar notification kerak bo'lsa: FIREBASE_CREDENTIALS_PATH environment variable to'g'ri set qiling

### "Firebase Admin SDK initialization failed"
- File format noto'g'ri (JSON parse error)
- File permissions muammosi
- Invalid credentials (Firebase Console'da yangi key yaratib ko'ring)

## Testing

```bash
# Local test (file mavjud bo'lsa)
dotnet run --project Convoy.Api

# Docker test (credentials yo'q)
docker-compose up
# Expected: Warning logged, but app starts successfully

# Railway test (credentials bilan)
# Set FIREBASE_CREDENTIALS_PATH in Railway environment variables
# Expected: Firebase initialized successfully
```

---

**Status**: ✅ Firebase credentials optional, API ishlaydi har qanday holatda
