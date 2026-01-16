# Railway 404 Error - Quick Fix Guide

## 🔴 Problem
Railway'da deploy muvaffaqiyatli, lekin barcha endpoint'lar 404 qaytaryapti:
- ❌ https://convoy-production-2969.up.railway.app/api/auth/verify_number → 404
- ❌ https://convoy-production-2969.up.railway.app/swagger → 404

## ✅ Solution (PUSH QILINDI - Railway avtomatik deploy qiladi)

### O'zgartirilgan Fayllar:

#### 1. **Program.cs** - Swagger Production'da yoqildi
```csharp
// BEFORE (404 error)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// AFTER (Fixed - Swagger root'da serve qilindi)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Convoy API v1");
    c.RoutePrefix = string.Empty; // Root (/) da Swagger UI
});

// HTTPS redirection o'chirildi (Railway HTTPS'ni handle qiladi)
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
```

#### 2. **Dockerfile** - Railway PORT'ni to'g'ri ishlatish
```dockerfile
# BEFORE
ENV ASPNETCORE_URLS=http://+:8080

# AFTER
ENV ASPNETCORE_ENVIRONMENT=Production
# Railway will inject PORT environment variable at runtime
# ASPNETCORE_URLS will be set via Railway environment variables
```

---

## 🚀 Railway'da Bajarish Kerak (MUHIM!)

### 1. Environment Variables Qo'shish

Railway Dashboard → Your Service → **Variables** → **New Variable**

**MINIMAL KERAKLI VARIABLES (bularni qo'shing!):**

```bash
# CRITICAL - Railway PORT binding
ASPNETCORE_URLS=http://0.0.0.0:$PORT

# CRITICAL - JWT Configuration
Jwt__SecretKey=your-super-secure-256-bit-secret-key-minimum-32-characters-long
Jwt__Issuer=ConvoyApi
Jwt__Audience=ConvoyClients
Jwt__ExpirationHours=720

# CRITICAL - Database Connection
ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}

# Auth Settings
Auth__AllowedPositionIds=86
Auth__OtpLength=4
Auth__OtpExpirationMinutes=1
Auth__OtpRateLimitSeconds=60
```

**AGAR SMS VA EXTERNAL API ISHLASHI KERAK BO'LSA:**

```bash
# External PHP API
PhpApi__GlobalPathForSupport=https://your-php-api.com/api/
PhpApi__Username=your-username
PhpApi__Password=your-password

# SMS Providers
SmsProviders__SmsFly__ApiKey=your-api-key
SmsProviders__SmsFly__ApiUrl=https://api.smsfly.uz/send
SmsProviders__Sayqal__UserName=your-username
SmsProviders__Sayqal__SecretKey=your-secret-key
SmsProviders__Sayqal__ApiUrl=https://routee.sayqal.uz/sms/TransmitSMS
```

### 2. PostgreSQL Database Qo'shish

```bash
# Railway Dashboard
1. New → Database → Add PostgreSQL
2. Database yaratilgandan keyin, uning DATABASE_URL'ini ConnectionStrings ga reference qiling:
   ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
```

### 3. Database Schema Initialize Qilish

```bash
# Railway PostgreSQL'ga connect bo'lib database-setup.sql'ni run qiling:
railway connect postgres

# SQL file'ni run qiling:
\i /path/to/database-setup.sql

# Yoki psql orqali:
psql "$DATABASE_URL" -f database-setup.sql
```

### 4. Redeploy Qilish

O'zgarishlar push qilindi, Railway avtomatik deploy qiladi. Yoki manual:

```bash
# Railway CLI orqali
railway up

# Logs ko'rish
railway logs --follow
```

---

## ✅ Verification Steps (Deploy bo'lgandan keyin)

### 1. Root Path - Swagger UI ko'rinishi kerak
```bash
curl https://convoy-production-2969.up.railway.app/
# Expected: HTML response with Swagger UI
```

### 2. Swagger JSON
```bash
curl https://convoy-production-2969.up.railway.app/swagger/v1/swagger.json
# Expected: JSON response with API documentation
```

### 3. API Endpoint Test
```bash
curl -X POST https://convoy-production-2969.up.railway.app/api/auth/verify_number \
  -H "Content-Type: application/json" \
  -d '{"phone_number":"+998901234567"}'
# Expected: JSON response (not 404)
```

---

## 🔍 Troubleshooting

### Issue: Hali ham 404
**Check:**
1. Railway logs: `railway logs --follow`
2. Environment variables to'g'ri set qilinganligini tekshiring:
   ```bash
   railway variables
   ```
3. `ASPNETCORE_URLS=http://0.0.0.0:$PORT` mavjudligini tekshiring

### Issue: "Database connection failed"
**Check:**
1. PostgreSQL service running: Railway Dashboard → PostgreSQL → Check status
2. `ConnectionStrings__DefaultConnection` to'g'ri reference qilinganligini tekshiring:
   ```bash
   ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
   ```
3. Database schema initialized: `psql "$DATABASE_URL" -c "\dt"`

### Issue: "JWT validation failed"
**Check:**
1. `Jwt__SecretKey` set qilinganligini tekshiring (minimum 32 characters)
2. `Jwt__Issuer` va `Jwt__Audience` to'g'ri set qilinganligini tekshiring

---

## 📊 Railway Deployment Status

**Current Deployment:**
- Commit: `a485e9a` - fix: enable Swagger in production
- Branch: `main`
- Status: ⏳ Deploying... (GitHub'dan push qilindi)

**Expected Results After Deploy:**
- ✅ Root path `/` → Swagger UI
- ✅ `/swagger/v1/swagger.json` → API documentation
- ✅ `/api/auth/verify_number` → 200/400 (not 404)
- ✅ `/hubs/location` → 400 (SignalR doesn't accept GET)

---

## 🎯 Summary

### Nima qilindi?
1. ✅ Swagger Production'da yoqildi va root path'da serve qilindi
2. ✅ HTTPS redirection o'chirildi (Railway handle qiladi)
3. ✅ Dockerfile PORT binding to'g'rilandi
4. ✅ O'zgarishlar GitHub'ga push qilindi

### Nimani qilish kerak?
1. ⏳ Railway'da environment variables qo'shish (yuqorida ro'yxat)
2. ⏳ PostgreSQL database qo'shish va initialize qilish
3. ⏳ Deploy tugaguncha kutish (2-5 daqiqa)
4. ⏳ Endpoint'larni test qilish

### Next Steps:
1. Railway Dashboard'ga o'ting
2. Variables → Add required environment variables
3. Database → Add PostgreSQL → Initialize schema
4. Logs → Monitor deployment: `railway logs --follow`
5. Test → Swagger UI'ni oching: https://convoy-production-2969.up.railway.app/

**Deploy tugagach, Swagger UI root path'da ko'rinadi! 🎉**
