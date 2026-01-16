# Location 401 Unauthorized Error - Debugging Guide

## Problem

`GET /api/locations/user/{user_id}` qaytaradi **401 Unauthorized**, lekin `GET /api/users` ishlaydi.

## Why This Happens

### Controller Authorization Differences

**LocationController.cs (Line 15):**
```csharp
[ApiController]
[Route("api/locations")]
[Authorize]  // ← ALL endpoints require authentication
public class LocationController : ControllerBase
```

**UserController.cs (Line 10):**
```csharp
[ApiController]
[Route("api/users")]
// ← NO [Authorize] attribute, endpoints are public
public class UserController : ControllerBase
```

## Solution

Location endpoint'lariga murojaat qilish uchun **JWT token kerak**.

### Step-by-Step: How to Get Token

#### 1. Verify Phone Number

```bash
POST /api/auth/verify_number
Content-Type: application/json

{
  "phone_number": "+998901234567"
}
```

**Response:**
```json
{
  "status": true,
  "message": "User topildi",
  "data": {
    "worker_id": 123,
    "worker_name": "John Doe",
    "phone_number": "+998901234567"
  }
}
```

#### 2. Request OTP Code

```bash
POST /api/auth/send_otp
Content-Type: application/json

{
  "phone_number": "+998901234567"
}
```

**Response:**
```json
{
  "status": true,
  "message": "OTP yuborildi",
  "data": null
}
```

**OTP code** will be:
- Sent via SMS (if SMS providers configured)
- Logged to console (check terminal/logs)

#### 3. Verify OTP and Get Token

```bash
POST /api/auth/verify_otp
Content-Type: application/json

{
  "phone_number": "+998901234567",
  "otp_code": "1234"
}
```

**Response:**
```json
{
  "status": true,
  "message": "Login muvaffaqiyatli",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expires_at": "2026-01-07T10:00:00Z",
    "expires_in_seconds": 86400
  }
}
```

#### 4. Use Token for Location Requests

```bash
GET /api/locations/user/1?start_date=2026-01-01&end_date=2026-01-07
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response:**
```json
{
  "status": true,
  "message": "5 ta location topildi",
  "data": [...]
}
```

## Common 401 Causes

### 1. Missing Authorization Header

❌ **Wrong:**
```bash
GET /api/locations/user/1
# No Authorization header
```

✅ **Correct:**
```bash
GET /api/locations/user/1
Authorization: Bearer eyJhbGc...
```

### 2. Wrong Token Format

❌ **Wrong:**
```bash
Authorization: eyJhbGc...  # Missing "Bearer"
Authorization: Token eyJhbGc...  # Wrong prefix
```

✅ **Correct:**
```bash
Authorization: Bearer eyJhbGc...
```

### 3. Expired Token

Tokens expire after configured time (default: 24 hours).

**Check expiration:**
```json
{
  "expires_at": "2026-01-07T10:00:00Z",  // Token expires here
  "expires_in_seconds": 86400
}
```

**Solution:** Request new token via `/api/auth/send_otp` + `/api/auth/verify_otp`

### 4. Blacklisted Token (Logged Out)

If you called `/api/auth/logout`, the token is blacklisted.

**Solution:** Get new token

### 5. Wrong JWT Secret Key

If `Jwt:SecretKey` in `appsettings.json` changed, old tokens become invalid.

**Solution:** Get new token

## Testing

### Test Script

Run this script to diagnose 401 issues:

```bash
python test_location_401.py
```

**What it tests:**
1. Location endpoint WITHOUT token → Should get 401
2. Users endpoint WITHOUT token → Should work (200)
3. Location endpoint with INVALID token → Should get 401
4. Location endpoint with VALID token → Should work (200)

### Manual Testing with curl

**1. Get OTP:**
```bash
curl -X POST http://localhost:5084/api/auth/verify_number \
  -H "Content-Type: application/json" \
  -d '{"phone_number": "+998901234567"}'

curl -X POST http://localhost:5084/api/auth/send_otp \
  -H "Content-Type: application/json" \
  -d '{"phone_number": "+998901234567"}'
```

**2. Get Token (replace OTP):**
```bash
curl -X POST http://localhost:5084/api/auth/verify_otp \
  -H "Content-Type: application/json" \
  -d '{"phone_number": "+998901234567", "otp_code": "1234"}'
```

**3. Use Token:**
```bash
curl -X GET "http://localhost:5084/api/locations/user/1?start_date=2026-01-01&end_date=2026-01-07" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## Quick Fix: Make UserController Consistent

If you want UserController to also require authentication:

```csharp
[ApiController]
[Route("api/users")]
[Authorize]  // ← Add this
public class UserController : ControllerBase
```

Or if you want LocationController to be public (NOT RECOMMENDED):

```csharp
[ApiController]
[Route("api/locations")]
// [Authorize]  // ← Remove this (NOT RECOMMENDED)
public class LocationController : ControllerBase
```

**Recommendation:** Keep `[Authorize]` on LocationController, it's a security best practice.

## Authorization Flow Diagram

```
Client Request → LocationController
                      ↓
                [Authorize] attribute
                      ↓
         Check Authorization header?
                ↓           ↓
              No           Yes
                ↓           ↓
            401          Extract token
         Unauthorized         ↓
                        Validate token
                              ↓
                    Valid?   Invalid?
                      ↓         ↓
                    200       401
                  Success  Unauthorized
```

## Endpoint Authorization Summary

| Endpoint | Auth Required | Why |
|----------|--------------|-----|
| `POST /api/auth/verify_number` | ❌ No | Public - phone verification |
| `POST /api/auth/send_otp` | ❌ No | Public - OTP request |
| `POST /api/auth/verify_otp` | ❌ No | Public - login |
| `GET /api/auth/me` | ✅ Yes | Protected - user info |
| `POST /api/auth/logout` | ✅ Yes | Protected - logout |
| `POST /api/users` | ❌ No | Public (should be protected?) |
| `GET /api/users/{id}` | ❌ No | Public (should be protected?) |
| `GET /api/locations/*` | ✅ Yes | Protected - location data |
| `POST /api/locations` | ✅ Yes | Protected - create location |

## Security Recommendations

1. ✅ **Keep LocationController protected** - Location data is sensitive
2. ⚠️ **Consider protecting UserController** - User data should require auth
3. ✅ **Use HTTPS in production** - Prevent token interception
4. ✅ **Implement token refresh** - Better UX for expired tokens
5. ✅ **Monitor failed auth attempts** - Detect attacks

## Related Documentation

- **API_RESPONSE_FORMAT.md** - Complete API examples
- **SNAKE_CASE_API_GUIDE.md** - API naming conventions
- **TOKEN_EXPIRATION_GUIDE.md** - JWT token configuration

## Still Getting 401?

Check these:

1. ✅ Token format: `Authorization: Bearer <token>`
2. ✅ Token not expired: Check `expires_at`
3. ✅ Token not blacklisted: Don't use after logout
4. ✅ Correct endpoint: `/api/locations/user/{id}` not `/api/location/...`
5. ✅ Application running: API must be started
6. ✅ Database connection: Check logs for errors
7. ✅ JWT configuration: Verify `appsettings.json` Jwt section

If none of these help, check application logs for detailed error messages.
