# Portainer Deployment Guide

Bu guide Convoy API'ni Portainer orqali production serverga deploy qilish uchun to'liq qo'llanma.

## 📋 Kerakli Ma'lumotlar

Quyidagi ma'lumotlarni tayyorlab qo'ying:

1. ✅ Database connection string
2. ✅ Firebase credentials JSON file (`firebase-adminsdk.json`)
3. ✅ Docker image name: `jm7uz/convoy:latest`
4. ✅ Port: `3908` (external) → `8080` (internal)
5. ✅ Deployment URL: `http://location-undiruv.garant.uz`

---

## 🚀 Deploy Qilish Jarayoni

### Step 1: Firebase Credentials'ni Base64'ga O'girish

#### Windows (PowerShell):
```powershell
# Script'ni ishga tushiring
.\encode-firebase-credentials.ps1 firebase-adminsdk.json

# Output: firebase-credentials-base64.txt file yaratiladi
```

#### Linux/Mac:
```bash
# Terminal'da ishga tushiring
cat firebase-adminsdk.json | base64 -w 0 > firebase-credentials-base64.txt
```

**Output misol:**
```
ewogICJ0eXBlIjogInNlcnZpY2VfYWNjb3VudCIsCiAgInByb2plY3RfaWQiOiAiY29udm95LXByb2R1Y3Rpb24iLAogIC...
```

---

### Step 2: Portainer'da Docker Compose Config

1. **Portainer UI'ga kiring**: `http://your-server:9000`

2. **Stacks → Add Stack** bosing

3. **Name**: `convoy-api` (yoki istalgan nom)

4. **Web editor**'da quyidagi konfiguratsiyani kiriting:

```yaml
version: "3.9"

services:
  api:
    image: jm7uz/convoy:latest
    container_name: convoyapi
    ports:
      - "3908:8080"
    environment:
      # ASP.NET Core
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080

      # Database
      ConnectionStrings__DefaultConnection: "Host=172.17.0.1;Port=5432;Database=convoydb;Username=postgres;Password=GarantDockerPass;Minimum Pool Size=10;Maximum Pool Size=50;Connection Idle Lifetime=60;Command Timeout=120;Pooling=true;"

      # Firebase (CRITICAL - qo'shishni unutmang!)
      FIREBASE_CREDENTIALS_BASE64: "PASTE_YOUR_BASE64_HERE"

    restart: always

    # Health check
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

    # Logging
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

5. **`FIREBASE_CREDENTIALS_BASE64`** qiymatini Step 1'dagi base64 string bilan almashtiring

6. **Deploy the stack** bosing

---

### Step 3: Tekshirish

#### 1. Container Status
Portainer UI'da:
- **Containers → convoyapi** - Status: **Running** 🟢

#### 2. Logs Tekshirish
Portainer UI'da:
- **Containers → convoyapi → Logs**
- Quyidagi xabarlarni qidiring:

```
✅ DirectFirebaseService initialized successfully
✅ Firebase App initialized
✅ PartitionMaintenanceService started
```

**Agar `❌ Firebase initialization failed` ko'rsangiz:**
- `FIREBASE_CREDENTIALS_BASE64` to'g'ri ekanligini tekshiring
- Base64 string ichida qo'shtirnoq (`"`) yo'qligini tekshiring
- Base64 decode qilib, valid JSON ekanligini tekshiring

#### 3. Health Check
Browser yoki curl orqali:

```bash
curl http://location-undiruv.garant.uz/health

# Expected response:
{
  "status": "healthy",
  "timestamp": "2026-02-03T...",
  "version": "v2.0-2026-01-17"
}
```

#### 4. Swagger UI
Browser'da oching:
```
http://location-undiruv.garant.uz/swagger
```

#### 5. SignalR Connection
HTML test file'ni oching yoki Flutter'dan ulaning:

```bash
# Browser console'da
curl http://location-undiruv.garant.uz/hubs/location
# Expected: 404 yoki connection upgrade request
```

---

## 🔧 Agar Muammo Bo'lsa

### Problem 1: Container Start Bo'lmayapti

**Check logs:**
```bash
# Portainer UI: Containers → convoyapi → Logs
# Yoki terminal'da:
docker logs convoyapi
```

**Mumkin sabab:**
- Database connection xatosi
- Firebase credentials xato
- Port busy (3908 port band)

### Problem 2: Firebase Error

**Error message:**
```
❌ Firebase initialization failed: Error reading credentials
```

**Yechim:**
1. Base64 string'ni qayta generate qiling
2. Base64 string to'liq ekanligini tekshiring (qisqa bo'lmasin)
3. Decode qilib test qiling:

```powershell
# PowerShell
$base64 = "YOUR_BASE64_STRING"
$bytes = [Convert]::FromBase64String($base64)
$json = [System.Text.Encoding]::UTF8.GetString($bytes)
$json | ConvertFrom-Json  # Valid JSON bo'lishi kerak
```

### Problem 3: Database Connection Failed

**Error message:**
```
❌ Connection to database failed: connection refused
```

**Yechim:**
1. Database server ishlab turganligini tekshiring:
```bash
psql -h 172.17.0.1 -p 5432 -U postgres -d convoydb
```

2. Connection string'dagi IP address to'g'riligini tekshiring:
   - `172.17.0.1` - Docker bridge network (default)
   - Yoki database server'ning IP addressi

3. PostgreSQL `pg_hba.conf` file'da Docker container'lardan ulanishga ruxsat berilganligini tekshiring

### Problem 4: Port Already in Use

**Error message:**
```
Error: Bind for 0.0.0.0:3908 failed: port is already allocated
```

**Yechim:**
1. Port'ni o'zgartiring:
```yaml
ports:
  - "3909:8080"  # 3908 o'rniga 3909
```

2. Yoki existing container'ni to'xtating:
```bash
docker stop convoyapi
docker rm convoyapi
```

---

## 📝 Environment Variables (Qo'shimcha)

Agar appsettings.json'dagi qiymatlarni override qilmoqchi bo'lsangiz, quyidagi environment variable'larni qo'shishingiz mumkin:

### JWT Settings
```yaml
Jwt__SecretKey: "your-secret-key-minimum-32-characters"
Jwt__ExpirationHours: 876000
```

### PHP API Settings
```yaml
PhpApi__GlobalPathForSupport: "http://delivery.garant.uz/api/"
PhpApi__Username: "login"
PhpApi__Password: "password"
```

### Auth Settings
```yaml
Auth__AllowedPositionIds: "86"
Auth__OtpExpirationMinutes: 1
Auth__OtpRateLimitSeconds: 60
```

### Telegram Bot
```yaml
BotSettings__Telegram__BotToken: "YOUR_BOT_TOKEN"
BotSettings__Telegram__ChannelId: "YOUR_CHANNEL_ID"
```

### SMS Providers
```yaml
SmsProviders__SmsFly__ApiKey: "YOUR_API_KEY"
SmsProviders__Sayqal__UserName: "YOUR_USERNAME"
SmsProviders__Sayqal__SecretKey: "YOUR_SECRET_KEY"
```

---

## 🎯 To'liq Misol (Barcha Settings Bilan)

```yaml
version: "3.9"

services:
  api:
    image: jm7uz/convoy:latest
    container_name: convoyapi
    ports:
      - "3908:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080

      # Database
      ConnectionStrings__DefaultConnection: "Host=172.17.0.1;Port=5432;Database=convoydb;Username=postgres;Password=GarantDockerPass;Minimum Pool Size=10;Maximum Pool Size=50;Connection Idle Lifetime=60;Command Timeout=120;Pooling=true;"

      # Firebase
      FIREBASE_CREDENTIALS_BASE64: "ewogICJ0eXBlIjogInNlcnZpY2VfYWNjb3VudCIsCiAgInByb2plY3RfaWQiOiAiY29udm95IiwKICAicHJpdmF0ZV9rZXlfaWQiOiAiYWJjMTIzIiwKICAicHJpdmF0ZV9rZXkiOiAiLS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tXG4uLi5cbi0tLS0tRU5EIFBSSVZBVEUgS0VZLS0tLS1cbiIsCiAgImNsaWVudF9lbWFpbCI6ICJmaXJlYmFzZS1hZG1pbnNka0Bjb252b3kuaWFtLmdzZXJ2aWNlYWNjb3VudC5jb20iLAogICJjbGllbnRfaWQiOiAiMTIzNDU2Nzg5IiwKICAiYXV0aF91cmkiOiAiaHR0cHM6Ly9hY2NvdW50cy5nb29nbGUuY29tL28vb2F1dGgyL2F1dGgiLAogICJ0b2tlbl91cmkiOiAiaHR0cHM6Ly9vYXV0aDIuZ29vZ2xlYXBpcy5jb20vdG9rZW4iLAogICJhdXRoX3Byb3ZpZGVyX3g1MDlfY2VydF91cmwiOiAiaHR0cHM6Ly93d3cuZ29vZ2xlYXBpcy5jb20vb2F1dGgyL3YxL2NlcnRzIiwKICAiY2xpZW50X3g1MDlfY2VydF91cmwiOiAiaHR0cHM6Ly93d3cuZ29vZ2xlYXBpcy5jb20vcm9ib3QvdjEvbWV0YWRhdGEveDUwOS9maXJlYmFzZS1hZG1pbnNka0Bjb252b3kuaWFtLmdzZXJ2aWNlYWNjb3VudC5jb20iLAogICJ1bml2ZXJzZV9kb21haW4iOiAiZ29vZ2xlYXBpcy5jb20iCn0K"

      # JWT (optional - uses appsettings.json defaults)
      Jwt__SecretKey: "convoy-production-jwt-secret-key-2025-minimum-32-characters-required-for-HS256"
      Jwt__ExpirationHours: 876000

      # PHP API (optional - uses appsettings.json defaults)
      PhpApi__GlobalPathForSupport: "http://delivery.garant.uz/api/"
      PhpApi__Username: "login"
      PhpApi__Password: "password"

      # Auth (optional - uses appsettings.json defaults)
      Auth__AllowedPositionIds: "86"

      # Telegram (optional - uses appsettings.json defaults)
      BotSettings__Telegram__BotToken: "8514698197:AAF2gfXtFExW9bwmGQRNZQisod5ShAy167w"
      BotSettings__Telegram__ChannelId: "-1003584246932"

    restart: always
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

---

## ✅ Final Checklist

Deploy qilishdan oldin tekshiring:

- [ ] Firebase credentials base64 encoded
- [ ] Database connection string to'g'ri
- [ ] Port 3908 available (yoki boshqa port tanlang)
- [ ] Docker image `jm7uz/convoy:latest` pull qilingan
- [ ] Portainer stack name kiritilgan
- [ ] Deploy tugmasini bosdingiz
- [ ] Container running (🟢 green status)
- [ ] Logs'da error yo'q
- [ ] `/health` endpoint ishlayapti
- [ ] `/swagger` endpoint ochinmoqda
- [ ] SignalR `/hubs/location` endpoint mavjud

---

## 🎉 Deploy Muvaffaqiyatli!

Agar barcha checklist'lar ✅ bo'lsa, API production'da ishga tushdi!

**Test qilish:**

1. **Health check:**
   ```bash
   curl http://location-undiruv.garant.uz/health
   ```

2. **Swagger UI:**
   ```
   http://location-undiruv.garant.uz/swagger
   ```

3. **SignalR (HTML test):**
   ```html
   <!-- test-production-signalr.html file'ni oching -->
   ```

4. **Flutter client:**
   ```dart
   final hubUrl = "http://location-undiruv.garant.uz/hubs/location";
   ```

---

## 📞 Qo'shimcha Yordam

Agar muammo yuzaga kelsa:

1. **Logs tekshiring:** Portainer → Containers → convoyapi → Logs
2. **Container restart qiling:** Portainer → Containers → convoyapi → Restart
3. **Stack'ni rebuild qiling:** Portainer → Stacks → convoy-api → Update/Redeploy

---

**Oxirgi yangilanish:** 2026-02-03
**Version:** v2.0
