# Railway Deployment Guide - Convoy GPS Tracking System

## 🚀 Deployed URL
**Production:** https://convoy-production-2969.up.railway.app

## 📋 Pre-Deployment Checklist

### 1. Railway Environment Variables (CRITICAL)

Bu environment variable'larni Railway dashboard'da sozlash SHART:

```bash
# Database Connection
DATABASE_URL=postgresql://USER:PASSWORD@HOST:PORT/DATABASE_NAME
ConnectionStrings__DefaultConnection=${DATABASE_URL}

# JWT Configuration
Jwt__SecretKey=your-super-secure-256-bit-secret-key-change-this-in-production
Jwt__Issuer=ConvoyApi
Jwt__Audience=ConvoyClients
Jwt__ExpirationHours=720

# Auth Configuration
Auth__AllowedPositionIds=86
Auth__OtpLength=4
Auth__OtpExpirationMinutes=1
Auth__OtpRateLimitSeconds=60

# External PHP API
PhpApi__GlobalPathForSupport=https://your-php-api.com/api/
PhpApi__Username=your-username
PhpApi__Password=your-password

# SMS Providers
SmsProviders__SmsFly__ApiKey=your-smsfly-api-key
SmsProviders__SmsFly__ApiUrl=https://api.smsfly.uz/send
SmsProviders__Sayqal__UserName=your-sayqal-username
SmsProviders__Sayqal__SecretKey=your-sayqal-secret-key
SmsProviders__Sayqal__ApiUrl=https://routee.sayqal.uz/sms/TransmitSMS

# Telegram Bot (Optional)
BotSettings__Telegram__BotToken=your-telegram-bot-token
BotSettings__Telegram__ChannelId=your-telegram-channel-id

# Encryption (Production)
Encryption__Enabled=false
Encryption__Key=GENERATE_WITH_generate-encryption-keys.ps1
Encryption__IV=GENERATE_WITH_generate-encryption-keys.ps1

# Deployment URL
DeploymentUrl=https://convoy-production-2969.up.railway.app

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:$PORT
```

### 2. Railway PostgreSQL Database Setup

Railway'da PostgreSQL database qo'shish:

1. Railway dashboard → **New** → **Database** → **PostgreSQL**
2. Database yaratilgandan keyin `DATABASE_URL` environment variable avtomatik yaratiladi
3. Database'ga ulanish:
   ```bash
   # Railway CLI orqali
   railway connect postgres

   # Yoki connection string'ni copy qilib:
   # Dashboard → PostgreSQL → Connect → Copy DATABASE_URL
   ```

4. Database schema'ni initialize qilish:
   ```bash
   # Local'dan Railway database'ga ulanish
   psql "postgresql://USER:PASSWORD@HOST:PORT/DATABASE_NAME" -f database-setup.sql
   ```

### 3. Railway CLI orqali Deployment

```bash
# Railway CLI o'rnatish
npm install -g @railway/cli

# Login qilish
railway login

# Project'ga link qilish
railway link

# Environment variables'ni set qilish
railway variables set Jwt__SecretKey="your-secret-key"
railway variables set ConnectionStrings__DefaultConnection="$DATABASE_URL"

# Deploy qilish
railway up

# Logs ko'rish
railway logs
```

### 4. GitHub Integration (Recommended)

1. Railway dashboard → **Settings** → **Service**
2. **Source** → **Connect GitHub**
3. Repository tanglang: `Abdumajidov-dev/ConvoyV2`
4. Branch tanglang: `main`
5. **Deploy Trigger**: `main` branch'ga push qilinganda avtomatik deploy bo'ladi

## 🔍 Deployment Verification

### 1. Health Check Endpoints

```bash
# Root path (404 expected - normal)
curl https://convoy-production-2969.up.railway.app

# Swagger UI (301/200 expected)
curl -L https://convoy-production-2969.up.railway.app/swagger/index.html

# SignalR Hub (400 expected - GET not allowed)
curl https://convoy-production-2969.up.railway.app/hubs/location

# API Health Check
curl -X POST https://convoy-production-2969.up.railway.app/api/auth/verify_number \
  -H "Content-Type: application/json" \
  -d '{"phone_number":"+998901234567"}'
```

### 2. Database Connection Check

Railway logs'da quyidagi xabarlarni qidiring:

```
✅ Database connection successful
✅ Partition maintenance completed
✅ Permission seed completed successfully
✅ Application started successfully
```

### 3. Common Issues & Solutions

#### Issue 1: "Database connection failed"
**Solution:**
- Railway'da PostgreSQL service running ekanligini tekshiring
- `DATABASE_URL` environment variable to'g'ri set qilinganini tekshiring
- Database schema initialized ekanligini tekshiring (run `database-setup.sql`)

#### Issue 2: "Port already in use"
**Solution:**
- Railway avtomatik `$PORT` variable beradi
- `ASPNETCORE_URLS=http://0.0.0.0:$PORT` to'g'ri set qilinganini tekshiring

#### Issue 3: "JWT validation failed"
**Solution:**
- `Jwt__SecretKey` environment variable set qilinganini tekshiring
- Key kamida 256-bit (32 characters) bo'lishi kerak

#### Issue 4: "SMS not sending"
**Solution:**
- SMS provider credentials to'g'ri set qilinganini tekshiring
- Railway logs'da SMS provider errors borligini tekshiring

#### Issue 5: "Swagger not accessible"
**Solution:**
- Production'da Swagger default o'chirilgan
- `appsettings.json`'da `ASPNETCORE_ENVIRONMENT=Production` bo'lsa, Swagger o'chiriladi
- Development uchun `ASPNETCORE_ENVIRONMENT=Development` set qiling (NOT recommended for production)

## 📊 Railway Dashboard Monitoring

### Metrics to Monitor:

1. **CPU Usage**: Normal < 50%
2. **Memory Usage**: Normal < 512MB
3. **Response Time**: Target < 500ms
4. **Error Rate**: Target < 1%

### Log Monitoring:

```bash
# Real-time logs
railway logs --follow

# Filter by level
railway logs --filter "ERROR"
railway logs --filter "WARNING"

# Export logs
railway logs > deployment.log
```

## 🔐 Security Checklist (Production)

- [ ] `Jwt__SecretKey` - Strong 256-bit key generated
- [ ] Database credentials - Secure password set
- [ ] SMS provider credentials - Stored as environment variables
- [ ] PHP API credentials - Stored as environment variables
- [ ] Telegram bot token - Stored as environment variables (if used)
- [ ] CORS policy - Configured for specific origins (not `AllowAll`)
- [ ] Encryption - Enabled with secure keys (if needed)
- [ ] HTTPS - Railway provides automatically
- [ ] Rate limiting - OTP rate limit configured (60s recommended)

## 🚀 Deployment Workflow

### Manual Deployment:
1. Local'da test qiling: `dotnet run --project Convoy.Api`
2. Changes'ni commit qiling: `git commit -m "feat: ..."`
3. Main branch'ga push qiling: `git push origin main`
4. Railway avtomatik deploy qiladi (GitHub integration bo'lsa)
5. Logs'ni monitor qiling: `railway logs --follow`
6. Deployment verify qiling: Test endpoints'ni chaqiring

### Rollback (Agar kerak bo'lsa):
1. Railway dashboard → **Deployments** tab
2. Previous successful deployment'ni tanlang
3. **Rollback** tugmasini bosing

## 📞 API Endpoints (Production)

Base URL: `https://convoy-production-2969.up.railway.app`

### Authentication
- `POST /api/auth/verify_number` - Verify phone number
- `POST /api/auth/send_otp` - Send OTP code
- `POST /api/auth/verify_otp` - Verify OTP and get JWT
- `GET /api/auth/me` - Get current user info (requires JWT)
- `POST /api/auth/logout` - Logout (blacklist token)

### Locations
- `POST /api/locations` - Create location
- `POST /api/locations/batch` - Create multiple locations
- `GET /api/locations/user/{userId}` - Get user locations
- `POST /api/locations/user_batch` - Get locations for multiple users
- `GET /api/locations/range` - Get locations in date range

### Users (Admin)
- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Permissions (Admin)
- `GET /api/permissions` - Get all permissions
- `GET /api/roles` - Get all roles
- `POST /api/permissions/users/{userId}/roles/{roleId}` - Assign role to user

### SignalR Hub
- `wss://convoy-production-2969.up.railway.app/hubs/location` - Real-time location updates

## 🔄 Environment-Specific Configuration

### Development (Local)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;..."
  },
  "Jwt": {
    "ExpirationHours": 24
  },
  "Auth": {
    "OtpRateLimitSeconds": 0
  }
}
```

### Production (Railway)
```bash
# All via environment variables
ConnectionStrings__DefaultConnection=${DATABASE_URL}
Jwt__ExpirationHours=720
Auth__OtpRateLimitSeconds=60
```

## 📝 Deployment Checklist Summary

- [x] ✅ Railway project created
- [x] ✅ PostgreSQL database added
- [x] ✅ Environment variables configured
- [x] ✅ GitHub integration enabled
- [x] ✅ Dockerfile configured
- [x] ✅ railway.toml created
- [x] ✅ Database schema initialized
- [x] ✅ First deployment successful
- [ ] ⏳ Health checks passing
- [ ] ⏳ API endpoints responding
- [ ] ⏳ SignalR hub working
- [ ] ⏳ SMS providers tested
- [ ] ⏳ Telegram bot tested (optional)

## 🆘 Support & Troubleshooting

### Railway Logs:
```bash
railway logs --follow
```

### Database Connection Test:
```bash
railway connect postgres
\dt  # List all tables
\d users  # Describe users table
```

### Service Status:
```bash
railway status
```

### Environment Variables:
```bash
railway variables
```

---

**Deployment URL:** https://convoy-production-2969.up.railway.app

**Swagger UI:** https://convoy-production-2969.up.railway.app/swagger (if enabled)

**SignalR Hub:** wss://convoy-production-2969.up.railway.app/hubs/location

**Health Check:** `GET /swagger/index.html` (returns 200 if healthy)
