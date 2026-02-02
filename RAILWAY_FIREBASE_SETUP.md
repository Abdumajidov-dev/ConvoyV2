# 🚀 Railway Firebase Setup - Quick Guide

## Firebase Credentials'ni Railway'ga Qo'yish (5 minut)

### Step 1: Firebase Credentials File Olish

1. [Firebase Console](https://console.firebase.google.com/) ga kiring
2. Project'ni tanlang
3. **Project Settings** > **Service Accounts** tab
4. **Generate new private key** tugmasini bosing
5. JSON file yuklab oling (`firebase-adminsdk-xxx.json`)

### Step 2: Base64'ga O'girish

**Windows PowerShell** (RECOMMENDED):
```powershell
# File yo'lini o'zgartiring
$base64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("C:\path\to\firebase-adminsdk.json"))
$base64 | Out-File firebase-base64.txt
Write-Host "✅ Base64 string saved to firebase-base64.txt"
Write-Host "📋 Copy content and paste to Railway environment variable"
```

**Linux/Mac**:
```bash
base64 -w 0 firebase-adminsdk.json > firebase-base64.txt
echo "✅ Base64 string saved to firebase-base64.txt"
```

**Output**: Bitta uzun string (masalan: `eyJ0eXBlIjoic2VydmljZV9hY2NvdW50IiwicHJva...`)

### Step 3: Railway Environment Variable Qo'shish

1. Railway dashboard'ga kiring: https://railway.app/
2. Project'ni tanlang
3. **Variables** tab'ga o'ting
4. **New Variable** tugmasini bosing
5. Quyidagicha kiriting:

```
Name:  FIREBASE_CREDENTIALS_BASE64
Value: <paste_base64_string_here>
```

6. **Add** tugmasini bosing
7. ✅ Done! Deployment avtomatik restart bo'ladi

### Step 4: Verify

Deployment log'ida quyidagilarni tekshiring:

**✅ Success (notification ishlaydi):**
```
[INFO] Loading Firebase credentials from FIREBASE_CREDENTIALS_BASE64 environment variable
[INFO] ✅ Firebase Admin SDK initialized from Base64 environment variable
```

**⚠️ Warning (notification o'chirilgan):**
```
[WARN] ⚠️ Firebase credentials not found. Checked:
[WARN]   1. FIREBASE_CREDENTIALS_BASE64 environment variable (not set)
[WARN]   2. File path: firebase-adminsdk.json (not found)
[WARN] Firebase notifications are DISABLED. API will continue without push notifications.
```

---

## Priority Order (Code ichida)

NotificationService 3 usulda credentials qidiradi:

1. **FIREBASE_CREDENTIALS_BASE64** (environment variable) - Railway uchun
2. **FIREBASE_CREDENTIALS_PATH** (file path) - Local development uchun
3. **firebase-adminsdk.json** (default file) - Local development uchun

Agar hech biri topilmasa - API ishlaydi, lekin notification disabled.

---

## Security ✅

- ✅ Firebase credentials **HECH QACHON** Git'ga commit qilinmaydi
- ✅ `.gitignore` da mavjud: `firebase-adminsdk.json`, `**/firebase-*.json`
- ✅ Docker image'da credentials yo'q
- ✅ Faqat Railway environment variable orqali beriladi
- ✅ Base64 string Railway secret sifatida saqlanadi

---

## Troubleshooting

### "Base64 string juda uzun" - PowerShell paste muammosi

Agar PowerShell'da paste qilishda muammo bo'lsa:

```powershell
# File'dan to'g'ridan-to'g'ri environment variable'ga yozish
$base64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("firebase-adminsdk.json"))
Write-Host "Copy this to Railway:" -ForegroundColor Green
Write-Host $base64
```

### "Invalid Base64 string" xatosi

Base64 string'da:
- Probel (space) bo'lmasligi kerak
- Yangi qator (newline) bo'lmasligi kerak
- Faqat harflar, raqamlar va `+`, `/`, `=` belgilar bo'lishi kerak

### Notification ishlamayapti

1. Railway log'ini tekshiring - "Firebase Admin SDK initialized" xabari bormi?
2. FIREBASE_CREDENTIALS_BASE64 environment variable to'g'ri set qilinganmi?
3. Base64 string to'liq copy qilinganmi? (Oxirida `=` belgisi bo'lishi mumkin)

---

## Quick Test

Local'da test qilish (Base64 environment variable bilan):

```bash
# PowerShell
$env:FIREBASE_CREDENTIALS_BASE64 = "<your_base64_string>"
dotnet run --project Convoy.Api

# Linux/Mac
export FIREBASE_CREDENTIALS_BASE64="<your_base64_string>"
dotnet run --project Convoy.Api
```

Expected output:
```
[INFO] Loading Firebase credentials from FIREBASE_CREDENTIALS_BASE64 environment variable
[INFO] ✅ Firebase Admin SDK initialized from Base64 environment variable
```

---

**Status**: ✅ Ready for Railway deployment
