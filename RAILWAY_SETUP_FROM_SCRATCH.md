# Railway Setup - Noldan Boshlash (From Scratch)

## 🚀 Railway'da Yangi Project Yaratish va Database Ulash

### STEP 1: Railway'da Yangi Project Yaratish

#### 1.1 Railway'ga Kiring
- **URL**: https://railway.app/
- **Login**: GitHub account bilan login qiling

#### 1.2 Yangi Project Yarating
```
1. Railway Dashboard → "New Project" tugmasini bosing
2. "Deploy from GitHub repo" ni tanlang
3. GitHub'dan repository tanlang: "ConvoyV2"
4. Branch tanlang: "main"
5. "Deploy Now" bosing
```

**YOKI CLI orqali:**
```bash
# Railway CLI o'rnatish
npm install -g @railway/cli

# Login qilish
railway login

# Yangi project yaratish
railway init

# GitHub repo'ni link qilish
railway link
```

---

### STEP 2: PostgreSQL Database Qo'shish ✅

#### 2.1 Database Service Yaratish

Railway Dashboard'da:
```
1. Proyektingizni oching
2. "New" tugmasini bosing (o'ng yuqori burchakda)
3. "Database" → "Add PostgreSQL" ni tanlang
4. PostgreSQL service avtomatik yaratiladi (30 sekund)
```

#### 2.2 Database Connection String Olish

PostgreSQL service yaratilgandan keyin:
```
1. PostgreSQL service'ni bosing
2. "Variables" tabga o'ting
3. Quyidagi o'zgaruvchilar ko'rinadi:
   - DATABASE_URL (to'liq connection string)
   - PGHOST
   - PGPORT
   - PGUSER
   - PGPASSWORD
   - PGDATABASE
```

**DATABASE_URL format:**
```
postgresql://postgres:PASSWORD@HOST:PORT/railway
```

---

### STEP 3: API Service'ga Database'ni Ulash

#### 3.1 Environment Variables Sozlash

API service'ingizni oching → **Variables** tab → quyidagilarni qo'shing:

```bash
# Database Connection (PostgreSQL service'dan reference)
ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}

# CRITICAL - JWT Configuration
Jwt__SecretKey=create-secure-256-bit-key-minimum-32-characters-long-change-this
Jwt__Issuer=ConvoyApi
Jwt__Audience=ConvoyClients
Jwt__ExpirationHours=720

# Auth Settings
Auth__AllowedPositionIds=86
Auth__OtpLength=4
Auth__OtpExpirationMinutes=1
Auth__OtpRateLimitSeconds=60

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production

# Deployment URL (Railway beradi, deploy bo'lgandan keyin)
DeploymentUrl=https://YOUR-SERVICE-NAME.up.railway.app
```

#### 3.2 Reference Syntax (Muhim!)

Railway'da boshqa service'ga reference qilish:
```bash
# Format:
${{SERVICE_NAME.VARIABLE_NAME}}

# Misol:
ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}

# Bu PostgreSQL service'ning DATABASE_URL variable'ini oladi
```

---

### STEP 4: Database Schema Initialize Qilish 🔧

Database yaratildi, lekin bo'sh. Endi table'larni yaratish kerak.

#### 4.1 Railway CLI orqali (Eng Oson)

```bash
# Railway CLI install (agar yo'q bo'lsa)
npm install -g @railway/cli

# Login
railway login

# Project'ga link
railway link

# PostgreSQL'ga connect
railway connect Postgres

# Database schema yuklash
\i database-setup.sql

# Yoki copy-paste qilish
# database-setup.sql faylini oching, kodni copy qiling
# Railway console'ga paste qiling va Enter bosing

# Verify tables
\dt

# Exit
\q
```

#### 4.2 Local'dan Remote Database'ga Connect (Alternative)

```bash
# Railway'dan DATABASE_URL'ni copy qiling
# Dashboard → PostgreSQL → Variables → DATABASE_URL

# psql orqali connect
psql "postgresql://postgres:PASSWORD@HOST:PORT/railway" -f database-setup.sql

# Yoki pgAdmin/DBeaver ishlatish:
# Host: railway.app'dan
# Port: 5432 (yoki boshqa)
# Database: railway
# User: postgres
# Password: Railway'dan
```

#### 4.3 Railway Console orqali (Web Browser)

```bash
# Railway Dashboard
1. PostgreSQL service'ni oching
2. "Data" tab → "Query" tugmasi
3. SQL query editor ochiladi
4. database-setup.sql faylini oching
5. Barcha SQL kodini copy qiling
6. Query editor'ga paste qiling
7. "Run" bosing
```

---

### STEP 5: External Services Sozlash (Ixtiyoriy)

Agar SMS va PHP API kerak bo'lsa:

```bash
# External PHP API
PhpApi__GlobalPathForSupport=https://your-php-api.com/api/
PhpApi__Username=your-username
PhpApi__Password=your-password

# SMS Providers (Failover: SmsFly → Sayqal)
SmsProviders__SmsFly__ApiKey=your-smsfly-api-key
SmsProviders__SmsFly__ApiUrl=https://api.smsfly.uz/send

SmsProviders__Sayqal__UserName=your-sayqal-username
SmsProviders__Sayqal__SecretKey=your-sayqal-secret-key
SmsProviders__Sayqal__ApiUrl=https://routee.sayqal.uz/sms/TransmitSMS

# Telegram Bot (Ixtiyoriy)
BotSettings__Telegram__BotToken=your-bot-token
BotSettings__Telegram__ChannelId=your-channel-id

# Encryption (Production uchun)
Encryption__Enabled=false
Encryption__Key=GENERATE_WITH_SCRIPT
Encryption__IV=GENERATE_WITH_SCRIPT
```

---

### STEP 6: Deployment Verify Qilish ✅

#### 6.1 Deployment Logs Ko'rish

Railway Dashboard:
```
1. API service'ni oching
2. "Deployments" tab
3. Latest deployment'ni bosing
4. Logs ko'rinadi
```

**CLI orqali:**
```bash
railway logs --follow
```

#### 6.2 Expected Logs (Success)

```
✅ Database connection successful
✅ Partition maintenance completed
✅ Permission seed completed successfully
✅ Now listening on: http://[::]:XXXX
✅ Application started. Press Ctrl+C to shut down.
```

#### 6.3 Deployment URL Olish

```
Railway Dashboard → API Service → Settings → Domains

Yoki avtomatik generate qilingan:
https://your-service-name-production-xxxx.up.railway.app
```

---

### STEP 7: API Test Qilish 🧪

#### 7.1 Swagger UI (Root Path)

```bash
https://your-service.up.railway.app/
```

Browser'da oching - Swagger UI ko'rinadi.

#### 7.2 API Endpoints Test

```bash
# Health check
curl https://your-service.up.railway.app/swagger/v1/swagger.json

# Auth endpoint
curl -X POST https://your-service.up.railway.app/api/auth/verify_number \
  -H "Content-Type: application/json" \
  -d '{"phone_number":"+998901234567"}'

# Expected: JSON response (not 404)
```

---

## 🔍 Troubleshooting

### Issue 1: "Database connection failed"

**Check:**
```bash
# Railway Dashboard → PostgreSQL → Status
# Should show "Running" (green)

# API Service → Variables
# Check: ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
```

**Fix:**
```bash
# Restart API service:
railway restart

# Yoki logs ko'ring:
railway logs --filter "database"
```

### Issue 2: "Tables not found"

**Check:**
```bash
# PostgreSQL'ga connect
railway connect Postgres

# Tables list
\dt

# Agar bo'sh bo'lsa:
\i database-setup.sql
```

### Issue 3: "JWT validation failed"

**Check:**
```bash
# Railway Dashboard → API Service → Variables
# Jwt__SecretKey mavjudligini tekshiring (minimum 32 characters)
```

### Issue 4: "Application start timeout"

**Check:**
```bash
# Logs:
railway logs --follow

# Port binding:
# Should show: Now listening on: http://[::]:XXXX
```

---

## 📊 Railway Architecture

```
┌─────────────────────────────────────┐
│   Railway Project: "Convoy"         │
├─────────────────────────────────────┤
│                                     │
│  ┌─────────────────────────────┐   │
│  │  Service: "Convoy-API"      │   │
│  │  - Dockerfile build         │   │
│  │  - GitHub: main branch      │   │
│  │  - Port: Dynamic ($PORT)    │   │
│  │  - URL: convoy-xxxx.up...   │   │
│  └─────────────────────────────┘   │
│              ↓                       │
│         (references)                │
│              ↓                       │
│  ┌─────────────────────────────┐   │
│  │  Service: "Postgres"        │   │
│  │  - PostgreSQL 16            │   │
│  │  - DATABASE_URL             │   │
│  │  - Tables: users, locations │   │
│  └─────────────────────────────┘   │
└─────────────────────────────────────┘
```

---

## ✅ Step-by-Step Checklist

- [ ] 1. Railway'ga login qildim
- [ ] 2. Yangi project yaratdim
- [ ] 3. GitHub repo'ni connect qildim (ConvoyV2)
- [ ] 4. PostgreSQL database qo'shdim
- [ ] 5. Environment variables sozladim:
  - [ ] `ConnectionStrings__DefaultConnection`
  - [ ] `Jwt__SecretKey`
  - [ ] `Jwt__Issuer`, `Jwt__Audience`
  - [ ] `Auth__AllowedPositionIds`
- [ ] 6. Database schema initialize qildim (`database-setup.sql`)
- [ ] 7. Deployment logs ko'rdim (success)
- [ ] 8. Swagger UI'ni ochib test qildim
- [ ] 9. API endpoints test qildim

---

## 🎯 Quick Commands Reference

```bash
# Railway CLI setup
npm install -g @railway/cli
railway login
railway link

# Logs
railway logs --follow
railway logs --filter "error"

# Database connect
railway connect Postgres

# Service restart
railway restart

# Environment variables
railway variables
railway variables set KEY=VALUE

# Deploy
railway up
```

---

## 📞 Support

**Railway Documentation:** https://docs.railway.app/
**Railway Discord:** https://discord.gg/railway

**Project Files:**
- `database-setup.sql` - Database schema
- `Dockerfile` - Container configuration
- `railway.toml` - Railway configuration
- `RAILWAY_DEPLOYMENT.md` - Full deployment guide

---

## 🚀 Production Ready Checklist

- [ ] PostgreSQL database created and initialized
- [ ] All environment variables configured
- [ ] JWT secret key is secure (32+ characters)
- [ ] Database has all required tables
- [ ] API responds with 200/400 (not 404)
- [ ] Swagger UI accessible
- [ ] SMS providers configured (if needed)
- [ ] PHP API connected (if needed)
- [ ] Logs show no errors
- [ ] Custom domain configured (optional)

**Deploy URL:** https://your-service.up.railway.app

**Status:** ✅ Production Ready
