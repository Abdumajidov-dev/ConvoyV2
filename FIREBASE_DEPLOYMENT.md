# Firebase Notification Deployment Guide

## 🔐 Security First

**ASLO `firebase-adminsdk.json` faylini Git'ga commit QILMANG!**

GitHub Push Protection bloklaydi va credentials public bo'lishi mumkin.

## ✅ Production Deployment (Recommended)

### Step 1: Convert to Base64

**Windows PowerShell:**
```powershell
# Firebase JSON'ni Base64'ga convert qilish
$json = Get-Content firebase-adminsdk.json -Raw
$bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
$base64 = [Convert]::ToBase64String($bytes)

# Clipboard'ga copy qilish
$base64 | Set-Clipboard
Write-Host "✅ Base64 string copied to clipboard"
```

**Linux/Mac:**
```bash
cat firebase-adminsdk.json | base64 -w 0 > firebase-base64.txt
cat firebase-base64.txt
```

### Step 2: Set Environment Variable on Server

```bash
# Docker run with environment variable
docker run -d \
  -p 8080:8080 \
  -e FIREBASE_CREDENTIALS_BASE64="eyJ0eXBlIjoic2VydmljZV9hY2NvdW50..." \
  -e ConnectionStrings__DefaultConnection="Host=db;Port=5432;..." \
  --name convoy-api \
  jm7uz/convoy:latest
```

### Step 3: Verify

```bash
# Check logs
docker logs convoy-api | grep Firebase

# Expected output:
# ✅ Firebase Admin SDK initialized from Base64 environment variable
```

## 🧪 Local Development

Place `firebase-adminsdk.json` in project root (already in .gitignore):

```bash
# Run API
dotnet run --project Convoy.Api

# Expected log:
# ✅ Firebase Admin SDK initialized from file: firebase-adminsdk.json
```

## 🔧 Troubleshooting

### "Firebase Admin SDK initialization failed"

**Solution**: Set environment variable correctly

```bash
# Check variable
echo $FIREBASE_CREDENTIALS_BASE64

# Set if empty
export FIREBASE_CREDENTIALS_BASE64="your-base64-string"
docker restart convoy-api
```

### "NullReferenceException in SendNotificationToAdminAsync"

**Cause**: Firebase not initialized

**Solution**: Check logs for warnings and set environment variable

## 📊 Test Notification

```bash
curl -X POST http://your-server:8080/api/notifications/send \
  -H "Content-Type: application/json" \
  -d '{
    "user_id": 5475,
    "title": "Test",
    "message": "Firebase working!",
    "data": {"test": "true"}
  }'
```

## 🚀 Docker Hub Deployment

```bash
# Build image
docker build -t jm7uz/convoy:latest .

# Push to Docker Hub
docker login
docker push jm7uz/convoy:latest

# Pull and run on server
docker pull jm7uz/convoy:latest
docker run -d \
  -e FIREBASE_CREDENTIALS_BASE64="$FIREBASE_CREDENTIALS_BASE64" \
  -p 8080:8080 \
  jm7uz/convoy:latest
```

---

**Summary**: Firebase credentials are loaded from `FIREBASE_CREDENTIALS_BASE64` environment variable in production, or from `firebase-adminsdk.json` file in local development.
