# Token Expiration Guide

## Overview

JWT token'lar muayyan muddat amal qiladi va avtomatik ravishda expire bo'ladi. Bu xavfsizlik uchun muhim feature.

## Configuration

### appsettings.json

```json
{
  "Jwt": {
    "SecretKey": "your-256-bit-secret-key-here",
    "Issuer": "ConvoyApi",
    "Audience": "ConvoyClients",
    "ExpirationHours": 24  // ← Token muddati (soatlarda)
  }
}
```

**Misollar:**
- `24` = 1 kun (default)
- `168` = 1 hafta
- `720` = 30 kun
- `1` = 1 soat (test uchun)
- `0.5` = 30 daqiqa

## API Response Format

### POST /api/auth/verify_otp

**Request:**
```json
{
  "phone_number": "+998901234567",
  "otp_code": "1234"
}
```

**Response (Success):**
```json
{
  "status": true,
  "message": "Muvaffaqiyatli tizimga kirildi",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expires_at": "2026-01-06T11:30:00.000Z",
    "expires_in_seconds": 86400
  }
}
```

**Response Fields:**
- `token` (string): JWT access token
- `expires_at` (DateTime): Token muddati tugash vaqti (UTC timezone)
- `expires_in_seconds` (long): Token qancha soniyadan keyin expire bo'ladi

## Token Validation

### Middleware (Automatic)

API middleware avtomatik ravishda har bir request'da token'ni tekshiradi:

```csharp
// Program.cs
ValidateLifetime = true  // ✅ Enabled
```

Agar token expired bo'lsa:
- **Status**: `401 Unauthorized`
- **Message**: Token muddati tugagan

### Manual Validation

TokenService orqali qo'lda tekshirish:

```csharp
var expiresAt = _tokenService.GetExpiryFromToken(token);
var isExpired = expiresAt.HasValue && expiresAt.Value < DateTime.UtcNow;
```

## Client-Side Implementation

### Flutter Example

```dart
class AuthToken {
  final String token;
  final DateTime expiresAt;
  final int expiresInSeconds;

  AuthToken({
    required this.token,
    required this.expiresAt,
    required this.expiresInSeconds,
  });

  factory AuthToken.fromJson(Map<String, dynamic> json) {
    return AuthToken(
      token: json['token'],
      expiresAt: DateTime.parse(json['expires_at']),
      expiresInSeconds: json['expires_in_seconds'],
    );
  }

  bool get isExpired => DateTime.now().isAfter(expiresAt);

  Duration get timeUntilExpiry => expiresAt.difference(DateTime.now());
}

// Usage:
final response = await http.post(
  Uri.parse('$baseUrl/api/auth/verify_otp'),
  body: jsonEncode({
    'phone_number': phoneNumber,
    'otp_code': otpCode,
  }),
);

final data = jsonDecode(response.body)['data'];
final authToken = AuthToken.fromJson(data);

// Check if expired
if (authToken.isExpired) {
  // Redirect to login
  Navigator.pushReplacementNamed(context, '/login');
} else {
  // Use token
  print('Token valid for: ${authToken.timeUntilExpiry}');
}
```

### JavaScript Example

```javascript
class AuthToken {
  constructor(data) {
    this.token = data.token;
    this.expiresAt = new Date(data.expires_at);
    this.expiresInSeconds = data.expires_in_seconds;
  }

  get isExpired() {
    return new Date() > this.expiresAt;
  }

  get timeUntilExpiry() {
    return this.expiresAt - new Date();
  }
}

// Usage:
const response = await fetch(`${baseUrl}/api/auth/verify_otp`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    phone_number: phoneNumber,
    otp_code: otpCode,
  }),
});

const { data } = await response.json();
const authToken = new AuthToken(data);

// Check if expired
if (authToken.isExpired) {
  // Redirect to login
  window.location.href = '/login';
} else {
  // Use token
  console.log(`Token valid for: ${authToken.timeUntilExpiry}ms`);
}
```

## Token Refresh Strategy

### Option 1: Re-authenticate (Current Implementation)

When token expires, user must log in again:
1. Client detects expired token (401 response)
2. Redirect to login page
3. User enters phone number and OTP
4. Get new token

### Option 2: Refresh Token (Future Enhancement)

Implement refresh token mechanism:
1. Issue both access token (short-lived) and refresh token (long-lived)
2. When access token expires, use refresh token to get new access token
3. Only re-authenticate when refresh token expires

**Note:** Current implementation uses Option 1 (re-authenticate).

## Error Handling

### Expired Token Response

**Request:**
```
GET /api/auth/me
Authorization: Bearer <expired_token>
```

**Response:**
```json
{
  "status": false,
  "message": "Token noto'g'ri yoki muddati tugagan",
  "data": null
}
```

**Status Code:** `401 Unauthorized`

### Best Practices

1. **Store expiration time**: Save `expires_at` along with token
2. **Proactive refresh**: Refresh token before it expires (e.g., 5 minutes before)
3. **Handle 401 gracefully**: Automatically redirect to login on 401 errors
4. **Clear storage**: Remove expired tokens from local storage
5. **User notification**: Warn user before token expires (optional)

## Testing Token Expiration

### Quick Test (1 minute expiration)

1. Temporarily change configuration:
```json
{
  "Jwt": {
    "ExpirationHours": 0.0167  // ~1 minute
  }
}
```

2. Restart API
3. Login and get token
4. Wait 1 minute
5. Try accessing protected endpoint
6. Should receive `401 Unauthorized`

### Development Mode

For development, use longer expiration (24 hours or more) to avoid frequent re-authentication.

### Production Mode

For production, use security best practices:
- Mobile apps: 7-30 days (users don't want to login frequently)
- Web apps: 1-24 hours (shorter for security)
- Admin panels: 1-4 hours (higher security requirement)

## Security Considerations

1. **HTTPS Only**: Always use HTTPS in production
2. **Secure Storage**: Store tokens securely (Flutter: flutter_secure_storage)
3. **Token Blacklist**: Use logout endpoint to blacklist tokens
4. **Short Expiration**: Balance between UX and security
5. **No Sensitive Data**: Don't put sensitive data in JWT payload (it's Base64, not encrypted)

## Related Endpoints

- `POST /api/auth/verify_otp` - Get token with expiration info
- `GET /api/auth/me` - Validate token (returns user info if valid)
- `POST /api/auth/logout` - Blacklist token before expiration

## Troubleshooting

### Token expires too quickly
- Increase `Jwt:ExpirationHours` in appsettings.json
- Check server time is correct (UTC)

### Token never expires
- Check `ValidateLifetime = true` in Program.cs
- Verify token has `exp` claim (check JWT payload)

### Clock skew issues
- Server and client clocks must be synchronized
- JWT validation allows small clock skew (default: 5 minutes)

---

**Last Updated:** 2026-01-05
**Version:** 1.0
