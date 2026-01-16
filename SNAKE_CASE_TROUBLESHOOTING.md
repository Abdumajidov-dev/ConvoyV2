# Snake Case JSON Troubleshooting Guide

## Overview

Convoy API barcha endpoint'larda **snake_case** JSON formatidan foydalanadi. Bu guide muammolarni hal qilish uchun yozilgan.

## Configuration

### Program.cs Configuration (Lines 176-185)

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Response serialization: PascalCase -> snake_case
        options.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
        options.JsonSerializerOptions.DictionaryKeyPolicy = new SnakeCaseNamingPolicy();

        // Request deserialization: snake_case, camelCase, PascalCase -> C# properties
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
```

### How It Works

1. **Response (C# → JSON)**:
   - C# property `UserId` → JSON field `user_id`
   - C# property `IsActive` → JSON field `is_active`
   - `SnakeCaseNamingPolicy` handles conversion

2. **Request (JSON → C#)**:
   - JSON field `user_id` → C# property `UserId` ✅
   - JSON field `userId` → C# property `UserId` ✅
   - JSON field `UserId` → C# property `UserId` ✅
   - `PropertyNameCaseInsensitive = true` allows all formats

## SnakeCaseNamingPolicy Implementation

**File**: `Convoy.Api/Helpers/SnakeCaseNamingPolicy.cs`

```csharp
public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        // PascalCase -> snake_case conversion
        // Example: "UserId" -> "user_id"
        // Example: "IsActive" -> "is_active"
    }
}
```

### Conversion Examples

| C# Property | JSON Field (snake_case) |
|-------------|------------------------|
| `Id` | `id` |
| `UserId` | `user_id` |
| `Name` | `name` |
| `PhoneNumber` | `phone_number` |
| `IsActive` | `is_active` |
| `CreatedAt` | `created_at` |
| `UpdatedAt` | `updated_at` |
| `TotalCount` | `total_count` |
| `PageSize` | `page_size` |
| `HasNextPage` | `has_next_page` |

## Common Issues

### Issue 1: Response fields are PascalCase instead of snake_case

**Symptom:**
```json
{
  "Status": true,
  "Message": "Success",
  "Data": { "UserId": 1, "Name": "John" }
}
```

**Expected:**
```json
{
  "status": true,
  "message": "Success",
  "data": { "user_id": 1, "name": "John" }
}
```

**Solution:**
- Check `SnakeCaseNamingPolicy` is registered in `Program.cs` (line 180)
- Ensure you're using `System.Text.Json` (not Newtonsoft.Json)
- Restart application after code changes

### Issue 2: Request deserialization fails

**Symptom:**
```
POST /api/users
Body: { "phone_number": "+998901234567", "is_active": true }
Response: 400 Bad Request - "Validation failed"
```

**Solution:**
- Add `PropertyNameCaseInsensitive = true` to JsonOptions (line 184)
- This allows snake_case, camelCase, and PascalCase in requests

### Issue 3: Some endpoints work, others don't

**Possible Causes:**

1. **Controller returns different types:**
   - Anonymous objects work ✅
   - DTO classes work ✅
   - But check if DTO has `[JsonPropertyName]` attributes

2. **Encryption middleware interference:**
   - Encryption middleware handles JSON separately
   - Check `Encryption:ExcludedRoutes` in appsettings.json

3. **SignalR messages:**
   - SignalR uses different serialization
   - May need separate configuration

## Testing

### Test Script: `test_user_snake_case.py`

Run this script to verify snake_case works correctly:

```bash
python test_user_snake_case.py
```

**What it tests:**
1. GET /api/users - Response should have `total_count`, `page_size`, etc.
2. GET /api/users/1 - Response should have `user_id`, `is_active`, `created_at`
3. POST /api/users - Accepts snake_case, camelCase, and PascalCase requests

### Manual Testing with curl

**Test 1: Get users (check response)**
```bash
curl http://localhost:5084/api/users?page=1&page_size=5
```

Expected response keys: `status`, `message`, `data`, `total_count`, `page`, `page_size`

**Test 2: Create user with snake_case**
```bash
curl -X POST http://localhost:5084/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "username": "testuser",
    "phone": "+998901234567",
    "is_active": true
  }'
```

Should work ✅

**Test 3: Create user with camelCase**
```bash
curl -X POST http://localhost:5084/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "username": "testuser",
    "phone": "+998901234567",
    "isActive": true
  }'
```

Should also work ✅ (thanks to `PropertyNameCaseInsensitive`)

## Debugging Tips

### 1. Check Serialized Output

Add logging to see actual JSON output:

```csharp
var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
{
    PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
    WriteIndented = true
});
_logger.LogInformation("Response: {Json}", json);
```

### 2. Verify DTO Properties

Ensure DTO classes don't have conflicting attributes:

```csharp
// ❌ BAD - Don't use JsonPropertyName with SnakeCaseNamingPolicy
public class UserDto
{
    [JsonPropertyName("UserId")]  // This overrides SnakeCaseNamingPolicy
    public long Id { get; set; }
}

// ✅ GOOD - Let SnakeCaseNamingPolicy handle it
public class UserDto
{
    public long Id { get; set; }  // Will become "id" in JSON
}
```

### 3. Check Controller Return Type

```csharp
// ✅ GOOD - Returns anonymous object, SnakeCaseNamingPolicy applies
return Ok(new { status = true, user_id = 123 });

// ✅ GOOD - Returns DTO, SnakeCaseNamingPolicy applies
return Ok(new UserResponseDto { Id = 123 });

// ⚠️ CHECK - Custom serialization
return new JsonResult(data, new JsonSerializerOptions { /* ... */ });
```

## Architecture Notes

### Why snake_case?

1. **Flutter/Dart convention**: Dart uses snake_case for JSON
2. **Python convention**: Python API clients use snake_case
3. **PostgreSQL convention**: Database columns are snake_case
4. **Consistency**: Same naming across frontend, backend, database

### Where SnakeCaseNamingPolicy is Applied

✅ **Applied:**
- Controller responses (anonymous objects and DTOs)
- Model binding from request body
- Query parameters (via case-insensitive matching)

❌ **NOT Applied:**
- Route parameters (use `[FromRoute(Name = "user_id")]` explicitly)
- Header names
- SignalR hub methods (needs separate config)
- Swagger/OpenAPI schema (may need custom filter)

## Configuration Files

### appsettings.json

No special snake_case configuration needed. SnakeCaseNamingPolicy is code-based.

### Swagger Configuration

If Swagger UI shows PascalCase instead of snake_case, add custom schema filter:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<SnakeCaseSchemaFilter>();
});
```

(Not implemented yet - add if needed)

## Related Documentation

- **Main Guide**: `SNAKE_CASE_API_GUIDE.md` - Complete API contract
- **Encryption**: `ENCRYPTION_EXCLUDED_ROUTES_GUIDE.md` - Encryption + snake_case
- **API Examples**: `API-EXAMPLES.http` - Real request/response examples

## Summary

✅ **What works now:**
- All controller responses use snake_case
- Request deserialization accepts snake_case, camelCase, PascalCase
- User endpoints: `/api/users`, `/api/users/{id}`, etc.
- Location endpoints: `/api/locations/user/{user_id}`, etc.
- Auth endpoints: `/api/auth/verify_number`, `/api/auth/send_otp`, etc.

✅ **Configuration is automatic:**
- No need to add attributes to DTOs
- No need to manually specify JSON property names
- Just define C# properties in PascalCase, they'll be snake_case in JSON

✅ **Client-side flexibility:**
- Flutter can send `is_active` or `isActive` or `IsActive` - all work
- Response always uses snake_case for consistency
