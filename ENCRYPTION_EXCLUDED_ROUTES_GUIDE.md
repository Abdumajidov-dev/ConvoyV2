# Encryption Excluded Routes - Configuration Guide

Ba'zi route'larni encryption'dan chiqarib tashlash uchun qo'llanma.

## 🎯 Maqsad

Ba'zi endpoint'lar (masalan, `/api/locations`) shifrlangan request qabul qilmaydi va oddiy JSON kutadi. Bu endpoint'larni encryption'dan **exclude** qilish kerak.

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "Encryption": {
    "Enabled": true,
    "Key": "YOUR_BASE64_KEY",
    "ExcludedRoutes": [
      "/api/locations",           // Exact match
      "/api/locations/*",         // Wildcard - /api/locations/ bilan boshlanadigan barcha route'lar
      "/swagger",                 // Swagger UI
      "/swagger/*",               // Swagger related
      "/health",                  // Health check
      "/hubs/*"                   // SignalR hubs
    ]
  }
}
```

### appsettings.Development.json

Development mode uchun ko'proq route'larni exclude qilish mumkin:

```json
{
  "Encryption": {
    "Enabled": true,
    "Key": "YOUR_BASE64_KEY",
    "ExcludedRoutes": [
      "/api/locations",
      "/api/locations/*",
      "/api/permissions",        // Permission management
      "/api/permissions/*",
      "/swagger",
      "/swagger/*",
      "/health",
      "/hubs/*"
    ]
  }
}
```

### Production Configuration

Production'da encryption'ni to'liq yoqish:

```json
{
  "Encryption": {
    "Enabled": true,
    "Key": "PRODUCTION_BASE64_KEY",
    "ExcludedRoutes": [
      "/swagger",                // Faqat Swagger va health check
      "/swagger/*",
      "/health"
    ]
  }
}
```

---

## 📝 Pattern Types

### 1. Exact Match

```json
"ExcludedRoutes": [
  "/api/locations"              // Faqat /api/locations
]
```

Bu faqat `/api/locations` ni exclude qiladi. `/api/locations/123` yoki `/api/locations/user/1` encrypt qilinadi.

### 2. Wildcard Match (/*)

```json
"ExcludedRoutes": [
  "/api/locations/*"            // /api/locations/ bilan boshlanadigan BARCHA route'lar
]
```

Bu quyidagilarni exclude qiladi:
- ✅ `/api/locations/123`
- ✅ `/api/locations/user/1`
- ✅ `/api/locations/batch`
- ✅ `/api/locations/user/1/daily-statistics`

### 3. Advanced Wildcard

```json
"ExcludedRoutes": [
  "/api/*/health"               // Har qanday controller'ning health endpoint'i
]
```

Bu quyidagilarni exclude qiladi:
- ✅ `/api/locations/health`
- ✅ `/api/users/health`
- ✅ `/api/reports/health`

---

## 🚀 Usage Examples

### Example 1: Location Route'larini Exclude Qilish

**Problem**: `/api/locations` shifrlangan request qabul qilmaydi, oddiy JSON kutadi.

**Solution**:

```json
{
  "Encryption": {
    "Enabled": true,
    "Key": "YOUR_KEY",
    "ExcludedRoutes": [
      "/api/locations",
      "/api/locations/*"
    ]
  }
}
```

**Test**:
```bash
# Oddiy JSON request - Ishlay
curl -X POST http://localhost:5084/api/locations \
  -H "Content-Type: application/json" \
  -d '{
    "user_id": 123,
    "locations": [
      {
        "recorded_at": "2025-12-25T10:00:00Z",
        "latitude": 41.0,
        "longitude": 69.0
      }
    ]
  }'
```

### Example 2: Permission Management Exclude Qilish

**Problem**: Admin panel'dan permission management qilishda encryption kerak emas.

**Solution**:

```json
{
  "Encryption": {
    "Enabled": true,
    "Key": "YOUR_KEY",
    "ExcludedRoutes": [
      "/api/permissions",
      "/api/permissions/*"
    ]
  }
}
```

### Example 3: Development Mode - Ko'p Route'lar

Development mode'da encryption'ni faqat muhim endpoint'larga qo'llash:

```json
{
  "Encryption": {
    "Enabled": true,
    "Key": "DEV_KEY",
    "ExcludedRoutes": [
      "/api/locations/*",
      "/api/permissions/*",
      "/api/signalrtest/*",
      "/swagger/*",
      "/health",
      "/hubs/*"
    ]
  }
}
```

### Example 4: Production - Minimal Exclude

Production'da faqat zarur route'larni exclude qilish:

```json
{
  "Encryption": {
    "Enabled": true,
    "Key": "PRODUCTION_KEY",
    "ExcludedRoutes": [
      "/health",                 // Health check monitoring uchun
      "/swagger"                 // Agar production'da Swagger bo'lsa
    ]
  }
}
```

---

## 🧪 Testing

### Test 1: Excluded Route (Encryption O'chirilgan)

```bash
# Location endpoint - excluded
curl -X POST http://localhost:5084/api/locations \
  -H "Content-Type: application/json" \
  -d '{"user_id": 123, "locations": [...]}'

# Response: 200 OK (oddiy JSON response)
```

### Test 2: Encrypted Route (Encryption Yoniq)

```bash
# Auth endpoint - encrypted
curl -X POST http://localhost:5084/api/auth/verify_number \
  -H "Content-Type: text/plain" \
  -d "ENCRYPTED_BASE64_STRING"

# Response: 200 OK (encrypted response)
```

### Test 3: Log Tekshirish

Application log'larida quyidagilarni ko'rishingiz kerak:

```
[Debug] Route /api/locations excluded from encryption
[Debug] Route /api/locations/user/123 excluded from encryption
[Info] 🔐 Attempting to decrypt request for /api/auth/verify_number
```

---

## ⚠️ Important Notes

### 1. Security Considerations

**Good Practice:**
```json
// Production - minimal exclude
"ExcludedRoutes": [
  "/health"
]
```

**Bad Practice:**
```json
// Production - juda ko'p exclude (INSECURE!)
"ExcludedRoutes": [
  "/api/*"                      // ❌ Barcha API'ni exclude qilish xavfli!
]
```

### 2. Case Insensitive

Route matching case-insensitive:
```json
"ExcludedRoutes": [
  "/API/LOCATIONS"              // ✅ /api/locations bilan bir xil
]
```

### 3. Trailing Slash

Trailing slash ahamiyatli:
```json
"ExcludedRoutes": [
  "/api/locations"              // ✅ /api/locations
  "/api/locations/"             // ⚠️ /api/locations/ (har xil!)
]
```

### 4. Order Doesn't Matter

Pattern'lar tartib bilan bog'liq emas - eng birinchi match topilganda to'xtaydi.

---

## 🔧 Advanced Configuration

### Environment-Specific Configuration

```bash
# Development
export Encryption__ExcludedRoutes__0="/api/locations"
export Encryption__ExcludedRoutes__1="/api/locations/*"

# Production (Docker)
docker run -e Encryption__ExcludedRoutes__0="/health" convoy-api
```

### Dynamic Configuration (Future Enhancement)

```csharp
// Runtime'da excluded route'larni o'zgartirish
public class EncryptionConfigService
{
    private List<string> _excludedRoutes = new();

    public void AddExcludedRoute(string route)
    {
        _excludedRoutes.Add(route);
    }

    public void RemoveExcludedRoute(string route)
    {
        _excludedRoutes.Remove(route);
    }
}
```

---

## 📊 Default Excluded Routes

Agar `appsettings.json` da `ExcludedRoutes` bo'lmasa, default qiymatlar:

```json
[
  "/swagger",
  "/swagger/*",
  "/health",
  "/hubs/*"
]
```

---

## 🐛 Troubleshooting

### Problem: "Encryption failed" - Location endpoint

**Reason**: `/api/locations` excluded emas, encryption kutmoqda.

**Solution**:
```json
{
  "Encryption": {
    "ExcludedRoutes": [
      "/api/locations",
      "/api/locations/*"
    ]
  }
}
```

### Problem: Swagger ishlamayapti

**Reason**: Swagger route excluded emas.

**Solution**:
```json
{
  "Encryption": {
    "ExcludedRoutes": [
      "/swagger",
      "/swagger/*"
    ]
  }
}
```

### Problem: SignalR connection fails

**Reason**: SignalR hub excluded emas.

**Solution**:
```json
{
  "Encryption": {
    "ExcludedRoutes": [
      "/hubs/*"
    ]
  }
}
```

---

## 📝 Best Practices

### ✅ DO

1. **Minimal Exclude** - Faqat zarur route'larni exclude qiling
2. **Environment-Specific** - Development va Production uchun har xil config
3. **Document Routes** - Har bir excluded route uchun comment yozing
4. **Test Thoroughly** - Barcha excluded route'larni test qiling
5. **Monitor Logs** - Excluded route'lar log'da ko'rinadi

### ❌ DON'T

1. **Over-Exclude** - Juda ko'p route'larni exclude qilmang (security risk!)
2. **Wildcard Abuse** - `"/api/*"` kabi umumiy pattern ishlatmang
3. **Hard-Code** - Code'da hard-code qilmang, appsettings ishlatning
4. **Forget SignalR** - SignalR hub'larni exclude qilishni unutmang
5. **Same Config Everywhere** - Dev va Prod uchun bir xil config ishlatmang

---

## 🎯 Summary

**Excluded Routes** sistemasi sizga:
- ✅ Ba'zi endpoint'larni encryption'dan exclude qilish imkonini beradi
- ✅ appsettings.json orqali boshqariladi
- ✅ Wildcard pattern support mavjud
- ✅ Environment-specific configuration
- ✅ Default fallback mavjud

**Configuration location**:
```
appsettings.json → Encryption → ExcludedRoutes
```

**Example**:
```json
{
  "Encryption": {
    "Enabled": true,
    "Key": "YOUR_KEY",
    "ExcludedRoutes": [
      "/api/locations",
      "/api/locations/*",
      "/swagger",
      "/swagger/*",
      "/health",
      "/hubs/*"
    ]
  }
}
```

---

Happy Coding! 🚀
