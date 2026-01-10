# Token Never Expires Configuration

## Overview

JWT token'lar **hech qachon expire bo'lmaydi**. Token faqat **logout** qilinganda yoki **manually blacklist** qilinganda bekor bo'ladi.

## Configuration Changes

### 1. appsettings.json

**File:** `Convoy.Api/appsettings.json`

```json
{
  "Jwt": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "ConvoyApi",
    "Audience": "ConvoyClients",
    "ExpirationHours": 876000  // 100 years (token hech qachon expire bo'lmaydi amalda)
  }
}
```

**ExpirationHours: 876000** = 100 yil
- Token generation'da shu qiymat ishlatiladi
- Lekin validation'da lifetime check o'chirilgan

### 2. Program.cs - JWT Validation

**File:** `Convoy.Api/Program.cs` (Line 94)

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = false, // ← O'CHIRILGAN - token expiration check yo'q
    ValidateIssuerSigningKey = true,
    ValidIssuer = jwtSettings["Issuer"],
    ValidAudience = jwtSettings["Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
};
```

**ValidateLifetime = false** - Bu token expiration check'ni butunlay o'chiradi.

## How It Works

### Token Generation

TokenService token yaratganda `ExpirationHours: 876000` ishlatadi:

```csharp
var tokenDescriptor = new SecurityTokenDescriptor
{
    Subject = new ClaimsIdentity(claims),
    Expires = DateTime.UtcNow.AddHours(876000), // 100 yil
    SigningCredentials = new SigningCredentials(
        new SymmetricSecurityKey(key),
        SecurityAlgorithms.HmacSha256Signature
    ),
    Issuer = _configuration["Jwt:Issuer"],
    Audience = _configuration["Jwt:Audience"]
};
```

### Token Validation

JWT middleware token tekshirganda:
- ✅ **Issuer** - Tekshiriladi
- ✅ **Audience** - Tekshiriladi
- ❌ **Lifetime (Expiration)** - **Tekshirilmaydi** (ValidateLifetime = false)
- ✅ **Signature** - Tekshiriladi
- ✅ **Blacklist** - Tekshiriladi (OnTokenValidated event'ida)

## Security

Token expiration o'chirilgan bo'lsa ham, xavfsizlik quyidagilar orqali ta'minlanadi:

### 1. Token Blacklist (Logout)

User logout qilganda token blacklist'ga qo'shiladi:

```csharp
POST /api/auth/logout
Authorization: Bearer <token>
```

Token `token_blacklist` table'ga qo'shiladi va keyingi request'larda rad etiladi.

### 2. Manual Token Revocation

Admin istalgan token'ni blacklist'ga qo'shishi mumkin:

```sql
INSERT INTO token_blacklist (token_hash, user_id, blacklisted_at, reason, expires_at)
VALUES ('token_jti_hash', 123, NOW(), 'security', NOW() + INTERVAL '100 years');
```

### 3. Secret Key Rotation

Agar `Jwt:SecretKey` o'zgartirilsa, barcha eski token'lar invalid bo'ladi:

```json
{
  "Jwt": {
    "SecretKey": "NEW-SECRET-KEY-HERE"  // Eski token'lar ishlamaydi
  }
}
```

### 4. Permission-Based Authorization

Token valid bo'lsa ham, user permission'i yo'q bo'lsa endpoint'ga kirish rad etiladi:

```csharp
[HasPermission("users.view")]
public async Task<IActionResult> GetUsers()
```

## Token Lifecycle

```
User Login (verify_otp)
    ↓
Token Generated (expires in 100 years)
    ↓
Token Stored by Client
    ↓
┌─────────────────────────┐
│ Token Used for Requests │ ← Token NEVER expires
└─────────────────────────┘
    ↓
User Logout
    ↓
Token Blacklisted
    ↓
Token Invalid (even if not "expired")
```

## API Examples

### Login and Get Token

```bash
# Step 1: Verify phone
POST /api/auth/verify_number
{
  "phone_number": "+998901234567"
}

# Step 2: Send OTP
POST /api/auth/send_otp
{
  "phone_number": "+998901234567"
}

# Step 3: Verify OTP and get token
POST /api/auth/verify_otp
{
  "phone_number": "+998901234567",
  "otp_code": "1234"
}

# Response:
{
  "status": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expires_at": "2126-01-01T00:00:00Z",  // 100 years from now
    "expires_in_seconds": 3153600000
  }
}
```

### Use Token Forever

Token **hech qachon expire bo'lmaydi**:

```bash
# Today
GET /api/locations/user/1
Authorization: Bearer eyJhbGc...
# ✅ Works

# 1 year later
GET /api/locations/user/1
Authorization: Bearer eyJhbGc...
# ✅ Still works

# 10 years later
GET /api/locations/user/1
Authorization: Bearer eyJhbGc...
# ✅ Still works (if not logged out or blacklisted)
```

### Logout Invalidates Token

```bash
# Logout
POST /api/auth/logout
Authorization: Bearer eyJhbGc...

# Response:
{
  "status": true,
  "message": "Muvaffaqiyatli logout qilindi"
}

# Try using same token after logout
GET /api/locations/user/1
Authorization: Bearer eyJhbGc...
# ❌ 401 Unauthorized (token blacklisted)
```

## Advantages

✅ **User Experience**: User hech qachon qayta login qilmaydi
✅ **Mobile Apps**: Token'ni saqlash va qayta-qayta ishlatish oson
✅ **IoT Devices**: Qurilmalar uzoq vaqt ishlashi mumkin
✅ **Simple Logic**: Expiration bilan bog'liq muammolar yo'q

## Disadvantages

⚠️ **Security Risk**: Token o'g'irlansa, logout qilinmaguncha ishlaydi
⚠️ **No Auto-Cleanup**: Eski token'lar abadiy valid
⚠️ **Compromise**: Bitta token buzilsa, manual blacklist kerak

## Recommendations

### For Production

1. ✅ **Use HTTPS only** - Token interception'ni oldini olish
2. ✅ **Implement device tracking** - Suspicious device'larni aniqlash
3. ✅ **Monitor unusual activity** - Bir token ko'p joydan ishlasa alert
4. ✅ **Periodic token refresh** - User'ga yangi token berish (optional)
5. ✅ **Strong secret key** - Kamida 256-bit secret key ishlatish

### For Development

- ✅ Test token expiration disabled
- ✅ Easy debugging (token hech qachon invalid bo'lmaydi)
- ✅ Faster development workflow

## Alternative: Add Manual Expiration

Agar kerakli bo'lsa, manual expiration qo'shish mumkin:

```csharp
// VerifyOtpResponseDto
public class VerifyOtpResponseDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("valid_until")]
    public DateTime? ValidUntil { get; set; } // Manual expiration (optional)
}
```

Database'da:
```sql
CREATE TABLE token_metadata (
    token_hash VARCHAR(500) PRIMARY KEY,
    user_id BIGINT,
    created_at TIMESTAMPTZ,
    valid_until TIMESTAMPTZ  -- Manual expiration
);
```

## Testing

### Test Token Never Expires

```bash
# Get token
TOKEN=$(curl -s -X POST http://localhost:5084/api/auth/verify_otp \
  -H "Content-Type: application/json" \
  -d '{"phone_number": "+998901234567", "otp_code": "1234"}' \
  | jq -r '.data.token')

# Use token today
curl http://localhost:5084/api/locations/user/1 \
  -H "Authorization: Bearer $TOKEN"
# ✅ Works

# Save token and test tomorrow, next month, next year
# Token will still work (unless logged out)
```

### Test Logout Blacklisting

```bash
# Logout
curl -X POST http://localhost:5084/api/auth/logout \
  -H "Authorization: Bearer $TOKEN"

# Try using same token
curl http://localhost:5084/api/locations/user/1 \
  -H "Authorization: Bearer $TOKEN"
# ❌ 401 Unauthorized
```

## Summary

| Feature | Status |
|---------|--------|
| Token Expiration | ❌ Disabled (never expires) |
| Token Validation | ✅ Signature, Issuer, Audience checked |
| Token Blacklist | ✅ Enabled (logout invalidates token) |
| Lifetime Check | ❌ Disabled (ValidateLifetime = false) |
| Security | ⚠️ Relies on blacklist and HTTPS |

**Token hech qachon expire bo'lmaydi, faqat logout yoki manual blacklist orqali bekor qilinadi.**

## Related Documentation

- **LOCATION_401_DEBUG.md** - 401 error troubleshooting
- **TOKEN_EXPIRATION_GUIDE.md** - Old expiration guide (deprecated)
- **API_RESPONSE_FORMAT.md** - API examples

---

**IMPORTANT NOTE**: This configuration is suitable for internal systems and mobile apps where user convenience is prioritized over token rotation. For high-security systems, consider implementing token refresh mechanism instead.
