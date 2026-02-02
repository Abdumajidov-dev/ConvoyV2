# 🚀 Production Deployment Guide (Docker)

## Server'ga Firebase Credentials Bilan Deploy Qilish

### Prereq uisites

- Docker va Docker Compose installed
- Firebase credentials file (`firebase-adminsdk.json`)
- Server'ga SSH access

---

## Step 1: Firebase Credentials Base64'ga O'girish

**Local machine'da** (Windows PowerShell):

```powershell
# Project directory'ga o'ting
cd C:\Users\abdum\source\repos\ConvoyV2

# Script'ni ishga tushiring
.\convert-firebase-to-base64.ps1
```

Bu script:
- ✅ `firebase-adminsdk.json` file'ni o'qiydi
- ✅ Base64'ga o'giradi
- ✅ `firebase-base64.txt` file'ga saqlaydi
- ✅ Clipboard'ga copy qiladi

**Output**: Bitta uzun Base64 string

---

## Step 2: .env File Yaratish

Server'da `.env` file yarating:

```bash
# Server'ga SSH qiling
ssh user@your-server.com

# Project directory'ga o'ting
cd /path/to/ConvoyV2

# .env file yaratish
nano .env
```

`.env` file ichiga quyidagilarni kiriting:

```bash
# Database Configuration
DB_NAME=convoy_db
DB_USER=postgres
DB_PASSWORD=YOUR_SECURE_PASSWORD
DB_PORT=5432

# API Configuration
API_PORT=8080

# Firebase Credentials (Base64 encoded)
# IMPORTANT: Paste the Base64 string from convert-firebase-to-base64.ps1
FIREBASE_CREDENTIALS_BASE64=eyJ0eXBlIjoic2VydmljZV9hY2NvdW50IiwicHJvamVjdF9pZCI6ImNvbnZveS1maXJlYmFzZSIsInByaXZhdGVfa2V5X2lkIj...
```

**IMPORTANT**:
- `DB_PASSWORD` ni o'zgartiring (production password)
- `FIREBASE_CREDENTIALS_BASE64` ga Base64 string'ni paste qiling (clipboard'dan Ctrl+V)

Save va exit (`Ctrl+O`, `Enter`, `Ctrl+X`)

---

## Step 3: Docker Image Build va Deploy

```bash
# Docker Compose bilan build va ishga tushirish
docker-compose -f docker-compose.prod.yml up -d --build

# Log'larni ko'rish
docker-compose -f docker-compose.prod.yml logs -f api
```

---

## Step 4: Verify - Firebase Initialized

Log'ida quyidagi xabarni tekshiring:

**✅ SUCCESS (notification ishlaydi):**
```
[INFO] Loading Firebase credentials from FIREBASE_CREDENTIALS_BASE64 environment variable
[INFO] ✅ Firebase Admin SDK initialized from Base64 environment variable
```

**⚠️ WARNING (notification o'chirilgan):**
```
[WARN] ⚠️ Firebase credentials not found. Checked:
[WARN]   1. FIREBASE_CREDENTIALS_BASE64 environment variable (not set)
[WARN]   2. File path: firebase-adminsdk.json (not found)
[WARN] Firebase notifications are DISABLED.
```

---

## Step 5: Test Notification

API test qilish:

```bash
curl -X POST "http://your-server.com:8080/api/notifications/test/5277" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test Notification",
    "message": "Server dan test"
  }'
```

**Expected Response (SUCCESS):**
```json
{
  "status": true,
  "message": "Notification muvaffaqiyatli yuborildi",
  "data": {
    "user_id": 5277,
    "title": "Test Notification",
    "message": "Server dan test"
  }
}
```

**Expected Response (FAIL - credentials noto'g'ri):**
```json
{
  "status": false,
  "message": "Notification yuborilmadi. Device token topilmadi yoki invalid.",
  "data": { ... }
}
```

---

## Troubleshooting

### 1. "Firebase credentials not found" Log'ida

**Sabab**: `.env` file'da `FIREBASE_CREDENTIALS_BASE64` bo'sh yoki noto'g'ri

**Yechim**:
```bash
# .env file'ni tekshiring
cat .env | grep FIREBASE_CREDENTIALS_BASE64

# Agar bo'sh bo'lsa - Base64 string'ni paste qiling
nano .env
```

### 2. "Invalid Base64 string" Xatosi

**Sabab**: Base64 string noto'g'ri copy qilingan (newline, space, etc.)

**Yechim**:
```powershell
# Local'da qaytadan generate qiling
.\convert-firebase-to-base64.ps1

# Clipboard'dan to'g'ri paste qiling (bitta qator bo'lishi kerak)
```

### 3. "Device token topilmadi" Xatosi

**Sabab**: Firebase initialized bo'lgan, lekin user'ning device token'i yo'q yoki invalid

**Yechim**:
```bash
# Database'da device token'ni tekshiring
docker exec -it convoy-postgres-prod psql -U postgres -d convoy_db

# SQL
SELECT * FROM device_tokens WHERE user_id = 5277;
```

Agar device token yo'q bo'lsa:
- Flutter app'dan token register qilish kerak
- Endpoint: `POST /api/device-tokens`

### 4. Container Restart Qilish

Agar `.env` file o'zgartirilsa:

```bash
# Container'ni restart qilish
docker-compose -f docker-compose.prod.yml restart api

# Yoki to'liq rebuild
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d --build
```

---

## Security Checklist

- ✅ `.env` file **GIT'GA COMMIT QILINMAGAN** (.gitignore'da bor)
- ✅ `firebase-adminsdk.json` **GIT'GA COMMIT QILINMAGAN** (.gitignore'da bor)
- ✅ `firebase-base64.txt` **GIT'GA COMMIT QILINMAGAN** (.gitignore'da bor)
- ✅ Server'da `.env` file **faqat root/admin** o'qishi mumkin:
  ```bash
  chmod 600 .env
  ```
- ✅ Production database password **strong va unique**

---

## Quick Commands

```bash
# Logs ko'rish
docker-compose -f docker-compose.prod.yml logs -f api

# Container status
docker-compose -f docker-compose.prod.yml ps

# Container'ga kirish (debugging)
docker exec -it convoy-api-prod sh

# Database'ga kirish
docker exec -it convoy-postgres-prod psql -U postgres -d convoy_db

# Stop all
docker-compose -f docker-compose.prod.yml down

# Start all
docker-compose -f docker-compose.prod.yml up -d
```

---

## File Structure (Server'da)

```
/path/to/ConvoyV2/
├── .env                          # SECRET - environment variables
├── docker-compose.prod.yml       # Production Docker Compose config
├── Dockerfile                    # Docker image build config
├── database-setup.sql            # Database schema
└── ... (other project files)
```

**IMPORTANT**:
- `.env` file **SERVER'DA** bo'lishi kerak
- `.env` file **GIT'GA COMMIT QILINMAYDI**
- Har bir server uchun alohida `.env` yaratiladi

---

**Status**: ✅ Ready for production deployment with Firebase notifications
