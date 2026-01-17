# Railway Environment Variables Setup Guide

This document lists all required environment variables for deploying Convoy API to Railway.

## Critical Environment Variables (MUST SET)

### 1. Database Connection
```bash
ConnectionStrings__DefaultConnection
```
**Value format:**
```
postgresql://<username>:<password>@<host>:<port>/<database>?sslmode=require
```

**Example (Railway PostgreSQL Plugin):**
```
postgresql://postgres:password123@containers-us-west-123.railway.app:5432/railway?sslmode=require
```

**Note:** Railway PostgreSQL plugin automatically creates this variable. If using external database, set manually.

---

### 2. JWT Configuration
```bash
Jwt__SecretKey
```
**Value:** Minimum 32 characters for HS256 algorithm
**Example:**
```
convoy-production-jwt-secret-key-2025-minimum-32-characters-required-for-HS256
```

**Current Production Value (already in appsettings.json):**
```
convoy-production-jwt-secret-key-2025-minimum-32-characters-required-for-HS256
```

---

## Optional Environment Variables (Override appsettings.json)

### 3. SMS Providers (Already in appsettings.json)
```bash
SmsProviders__SmsFly__ApiKey=9b9ea1f9-6699-11ed-b8e4-0242ac120003
SmsProviders__Sayqal__UserName=ismoilovdb
SmsProviders__Sayqal__SecretKey=298174a623207364db70a02ebb57124e
```

### 4. Telegram Bot (Already in appsettings.json)
```bash
BotSettings__Telegram__BotToken=8514698197:AAF2gfXtFExW9bwmGQRNZQisod5ShAy167w
BotSettings__Telegram__ChannelId=-1003584246932
```

### 5. PHP API Integration (Already in appsettings.json)
```bash
PhpApi__GlobalPathForSupport=https://garant-hr.uz/api/
PhpApi__Username=login
PhpApi__Password=password
```

### 6. Encryption (Already in appsettings.json - DISABLED by default)
```bash
Encryption__Enabled=false
Encryption__Key=DMyx68VCO2K8IYpGjz6nlEn+AeXj/FEC1dVhUiWTJyQ=
```

---

## How to Set Environment Variables in Railway

### Method 1: Railway Dashboard (Recommended)
1. Go to your Railway project
2. Click on your service (Convoy API)
3. Go to **Variables** tab
4. Click **+ New Variable**
5. Add variable name and value
6. Click **Save** (automatic redeploy)

### Method 2: Railway CLI
```bash
railway variables set ConnectionStrings__DefaultConnection="postgresql://..."
railway variables set Jwt__SecretKey="your-secret-key"
```

### Method 3: `.env` file (Local testing only - DO NOT commit)
```bash
# .env (for local Railway testing)
ConnectionStrings__DefaultConnection=postgresql://...
Jwt__SecretKey=your-secret-key
```

---

## Environment Variable Priority

Railway uses this order (highest to lowest priority):

1. **Railway Environment Variables** (set in Dashboard/CLI)
2. **appsettings.Production.json** (if exists)
3. **appsettings.json** (default values)

**Important:** Railway environment variables OVERRIDE appsettings.json values.

---

## Current Setup Status

✅ **Already configured in appsettings.json:**
- JWT SecretKey (production-ready)
- SMS Providers (SmsFly, Sayqal)
- Telegram Bot (token, channel ID)
- PHP API credentials
- Encryption settings (disabled)
- Auth settings (OTP, position IDs)

❌ **MUST configure in Railway:**
- `ConnectionStrings__DefaultConnection` (use Railway PostgreSQL plugin variable)

---

## Testing Deployment

After setting environment variables, test endpoints:

1. **Health check:**
   ```bash
   curl https://convoy-production-2969.up.railway.app/health
   ```
   Expected: `{"status":"healthy","timestamp":"...","environment":"Production"}`

2. **Swagger UI:**
   ```
   https://convoy-production-2969.up.railway.app/swagger
   ```

3. **Test authentication:**
   ```bash
   curl -X POST https://convoy-production-2969.up.railway.app/api/auth/verify_number \
     -H "Content-Type: application/json" \
     -d '{"phone_number":"998941033001"}'
   ```

---

## Troubleshooting

### "Connection string not found" error
- Check if `ConnectionStrings__DefaultConnection` is set in Railway Variables
- Use double underscore `__` not single `_` or `:` (Railway format)

### "JWT SecretKey not configured" error
- Check if `Jwt__SecretKey` is set (should be in appsettings.json by default)
- Minimum 32 characters required for HS256

### Database connection timeout
- Verify PostgreSQL service is running in Railway
- Check connection string format (use `postgresql://` not `Host=...`)
- Ensure `sslmode=require` is included

### Healthcheck fails
- Check Railway logs: `railway logs`
- Verify application started successfully
- Check if background services (DatabaseInitializerService, PartitionMaintenanceService) completed

---

## Security Best Practices

✅ **DO:**
- Use Railway Variables for database connection strings
- Rotate JWT secret keys periodically
- Use Railway's built-in PostgreSQL plugin (auto-configured)
- Keep sensitive data in Railway Variables (not in code)

❌ **DON'T:**
- Commit `.env` files to Git
- Hardcode passwords in appsettings.json
- Share Railway project tokens publicly
- Use weak JWT secret keys (< 32 chars)

---

## Quick Setup Checklist

For new Railway deployment:

- [ ] Create Railway project
- [ ] Add PostgreSQL plugin (auto-creates `DATABASE_URL` variable)
- [ ] Set `ConnectionStrings__DefaultConnection` = `${{DATABASE_URL}}`
- [ ] Verify `Jwt__SecretKey` in appsettings.json (already set)
- [ ] Deploy from GitHub main branch
- [ ] Wait for healthcheck to pass
- [ ] Test `/health` endpoint
- [ ] Test `/swagger` UI
- [ ] Test authentication flow

---

**Last Updated:** 2025-01-17
**Railway Project:** convoy-production-2969
**Deployment URL:** https://convoy-production-2969.up.railway.app
