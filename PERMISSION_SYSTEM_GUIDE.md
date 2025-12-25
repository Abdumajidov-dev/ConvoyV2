# Permission System Guide

Complete guide for ASP.NET Core Identity + Permission system in Convoy GPS Tracking.

## 📋 Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Database Schema](#database-schema)
3. [Permission Naming Convention](#permission-naming-convention)
4. [Setup & Installation](#setup--installation)
5. [Usage Examples](#usage-examples)
6. [Testing](#testing)
7. [Best Practices](#best-practices)

---

## Architecture Overview

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                    Permission System                         │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  User ─┬─> UserRole ──> Role ─┬─> RolePermission ──> Permission
│        │                       │                              │
│        └─> UserRole ──> Role ─┘                              │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

**Key Features:**
- ✅ **Role-Based Access Control (RBAC)** - User'lar role orqali permission oladi
- ✅ **Many-to-Many Relationships** - User ko'p rolga, Role ko'p permission'ga ega bo'lishi mumkin
- ✅ **Attribute-Based Authorization** - `[HasPermission("users.view")]`
- ✅ **Centralized Permission Management** - `Convoy.Domain.Constants.Permissions`
- ✅ **Auto-Seeding** - Dastlabki role va permission'lar avtomatik yaratiladi

---

## Database Schema

### Tables

#### 1. `roles`
```sql
CREATE TABLE roles (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    display_name VARCHAR(200) NOT NULL,
    description VARCHAR(500),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ,
    delete_at TIMESTAMPTZ
);
```

#### 2. `permissions`
```sql
CREATE TABLE permissions (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,      -- users.view, locations.create
    display_name VARCHAR(200) NOT NULL,     -- View Users, Create Location
    resource VARCHAR(50) NOT NULL,          -- users, locations
    action VARCHAR(50) NOT NULL,            -- view, create
    description VARCHAR(500),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ,
    delete_at TIMESTAMPTZ
);
```

#### 3. `user_roles` (Junction Table)
```sql
CREATE TABLE user_roles (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES users(id),
    role_id BIGINT NOT NULL REFERENCES roles(id),
    assigned_at TIMESTAMPTZ NOT NULL,
    assigned_by BIGINT,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ,
    delete_at TIMESTAMPTZ,
    UNIQUE(user_id, role_id)
);
```

#### 4. `role_permissions` (Junction Table)
```sql
CREATE TABLE role_permissions (
    id BIGSERIAL PRIMARY KEY,
    role_id BIGINT NOT NULL REFERENCES roles(id),
    permission_id BIGINT NOT NULL REFERENCES permissions(id),
    granted_at TIMESTAMPTZ NOT NULL,
    granted_by BIGINT,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ,
    delete_at TIMESTAMPTZ,
    UNIQUE(role_id, permission_id)
);
```

---

## Permission Naming Convention

### Format
```
<resource>.<action>
```

### Examples
```
✅ users.view
✅ users.create
✅ locations.view_all
✅ reports.export
✅ dashboard.view_statistics
```

### Resource Categories
- `users` - Foydalanuvchilar
- `locations` - GPS lokatsiyalar
- `reports` - Hisobotlar
- `roles` - Rollar
- `permissions` - Ruxsatlar
- `dashboard` - Dashboard
- `settings` - Sozlamalar

### Action Types
- `view` - Ko'rish
- `create` - Yaratish
- `update` - Yangilash
- `delete` - O'chirish
- `export` - Export qilish
- `manage` - To'liq boshqarish
- `assign` - Tayinlash

---

## Setup & Installation

### 1. Run Database Migration

```bash
# PostgreSQL database'da migration'ni run qiling
psql -U postgres -d convoy_db -f add-permission-system.sql
```

Bu script:
- ✅ 4 ta yangi table yaratadi (roles, permissions, user_roles, role_permissions)
- ✅ 28 ta permission yaratadi
- ✅ 5 ta role yaratadi (SuperAdmin, Admin, Manager, Driver, Viewer)
- ✅ Role-Permission bog'lanishlarini yaratadi

### 2. Application Configuration

`Program.cs` da allaqachon konfiguratsiya qilingan:

```csharp
// Permission service
builder.Services.AddScoped<IPermissionService, PermissionService>();

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    var allPermissions = Permissions.GetAll();
    foreach (var (name, _, _, _, _) in allPermissions)
    {
        options.AddPolicy(name, policy =>
            policy.Requirements.Add(new PermissionRequirement(name)));
    }
});

// Authorization handler
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

// Seed service
builder.Services.AddHostedService<PermissionSeedService>();
```

### 3. Verify Installation

```bash
# Application'ni ishga tushiring
dotnet run --project Convoy.Api

# Loglarni tekshiring - quyidagilarni ko'rishingiz kerak:
# ✅ Permission seed completed successfully
# ✅ Permissions seeded: 28 permissions
# ✅ Roles seeded: 5 roles
```

---

## Usage Examples

### 1. Controller'da Permission Ishlatish

```csharp
using Convoy.Api.Authorization;
using Convoy.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/users")]
[Authorize] // Faqat authenticated userlar
public class UserController : ControllerBase
{
    // Faqat "users.view" permission'i bor userlar kirishi mumkin
    [HttpGet]
    [HasPermission(Permissions.Users.View)]
    public async Task<IActionResult> GetAllUsers()
    {
        // ...
    }

    // Faqat "users.create" permission'i bor userlar kirishi mumkin
    [HttpPost]
    [HasPermission(Permissions.Users.Create)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        // ...
    }

    // Faqat "users.delete" permission'i bor userlar kirishi mumkin
    [HttpDelete("{id}")]
    [HasPermission(Permissions.Users.Delete)]
    public async Task<IActionResult> DeleteUser(long id)
    {
        // ...
    }
}
```

### 2. User'ga Role Assign Qilish

```http
POST /api/permissions/users/123/roles/1
Authorization: Bearer {token}

Response:
{
  "status": true,
  "message": "Rol muvaffaqiyatli biriktirildi",
  "data": {
    "id": 1,
    "user_id": 123,
    "role_id": 1,
    "assigned_at": "2025-12-25T10:00:00Z"
  }
}
```

### 3. User'ning Permission'larini Olish

```http
GET /api/permissions/users/123
Authorization: Bearer {token}

Response:
{
  "status": true,
  "message": "Foydalanuvchi ruxsatlari olindi",
  "data": [
    "users.view",
    "users.create",
    "locations.view",
    "locations.create",
    "dashboard.view_own"
  ]
}
```

### 4. Service Layer'da Permission Tekshirish

```csharp
public class LocationService : ILocationService
{
    private readonly IPermissionService _permissionService;

    public async Task<ServiceResult<Location>> CreateLocationAsync(CreateLocationDto dto)
    {
        // Current user'ning permission'ini tekshirish
        var hasPermission = await _permissionService.UserHasPermissionAsync(
            dto.UserId,
            Permissions.Locations.Create
        );

        if (!hasPermission)
        {
            return ServiceResult<Location>.Forbidden("Lokatsiya yaratish uchun ruxsat yo'q");
        }

        // Create location logic...
    }
}
```

---

## Default Roles & Permissions

### SuperAdmin
**Barcha ruxsatlar** - 28 ta permission

### Admin
- `users.view`, `users.create`, `users.update`
- `locations.view`, `locations.view_all`, `locations.export`
- `reports.view`, `reports.export`, `reports.create`
- `dashboard.view_all`, `dashboard.view_statistics`
- `settings.view`

### Manager
- `users.view`
- `locations.view`, `locations.view_all`, `locations.export`
- `reports.view`, `reports.export`
- `dashboard.view_all`, `dashboard.view_statistics`

### Driver
- `locations.view`, `locations.create`
- `dashboard.view_own`

### Viewer
- `users.view`
- `locations.view`
- `reports.view`
- `dashboard.view_own`

---

## Testing

### 1. Test User Yaratish va Role Assign Qilish

```sql
-- 1. Test user yaratish
INSERT INTO users (name, username, phone, is_active, created_at)
VALUES ('Test Driver', 'driver1', '+998901234567', true, NOW());

-- 2. User ID'ni olish
SELECT id FROM users WHERE username = 'driver1';
-- Result: 1 (masalan)

-- 3. Driver role'ni topish
SELECT id FROM roles WHERE name = 'Driver';
-- Result: 4 (masalan)

-- 4. User'ga Driver role'ni assign qilish
INSERT INTO user_roles (user_id, role_id, assigned_at, created_at)
VALUES (1, 4, NOW(), NOW());
```

### 2. Permission Tekshirish

```sql
-- User'ning barcha permission'larini ko'rish
SELECT DISTINCT p.name, p.display_name
FROM users u
JOIN user_roles ur ON u.id = ur.user_id
JOIN role_permissions rp ON ur.role_id = rp.role_id
JOIN permissions p ON rp.permission_id = p.id
WHERE u.id = 1 AND p.is_active = true
ORDER BY p.name;
```

### 3. API Testing

```bash
# 1. Login qiling va JWT token oling
curl -X POST http://localhost:5084/api/auth/verify_otp \
  -H "Content-Type: application/json" \
  -d '{"phone_number": "+998901234567", "otp_code": "1234"}'

# Response:
# { "status": true, "data": { "token": "eyJhbGc..." } }

# 2. User'ning role'larini ko'ring
curl -X GET http://localhost:5084/api/permissions/users/1/roles \
  -H "Authorization: Bearer eyJhbGc..."

# 3. Permission-protected endpoint'ga kirish
curl -X GET http://localhost:5084/api/locations/user/1 \
  -H "Authorization: Bearer eyJhbGc..."
```

---

## Best Practices

### 1. ✅ Centralized Permission Names

**GOOD:**
```csharp
using Convoy.Domain.Constants;

[HasPermission(Permissions.Users.View)]
public async Task<IActionResult> GetUsers() { }
```

**BAD:**
```csharp
[HasPermission("users.view")] // Magic string - avoid!
public async Task<IActionResult> GetUsers() { }
```

### 2. ✅ Granular Permissions

**GOOD:**
```csharp
// Alohida permission'lar yaratish
Permissions.Locations.View       // O'z location'larini ko'rish
Permissions.Locations.ViewAll    // Barcha location'larni ko'rish
Permissions.Locations.Export     // Export qilish
```

**BAD:**
```csharp
// Juda umumiy permission - foydalanishni qiyinlashtiradi
Permissions.Locations.All
```

### 3. ✅ Role Hierarchy

```
SuperAdmin > Admin > Manager > Driver/Viewer
```

- SuperAdmin - **hamma narsaga** ruxsat
- Admin - **ko'pgina** operatsiyalarga ruxsat
- Manager - **ma'lum** operatsiyalarga ruxsat
- Driver/Viewer - **faqat o'qish** yoki **o'z ma'lumotlari**

### 4. ✅ Permission Caching

```csharp
// Future improvement: Permission'larni cache qilish
public class PermissionService : IPermissionService
{
    private readonly IMemoryCache _cache;

    public async Task<List<string>> GetUserPermissionsAsync(long userId)
    {
        var cacheKey = $"user_permissions_{userId}";

        if (_cache.TryGetValue(cacheKey, out List<string>? permissions))
            return permissions!;

        permissions = await LoadPermissionsFromDatabase(userId);

        _cache.Set(cacheKey, permissions, TimeSpan.FromMinutes(15));

        return permissions;
    }
}
```

### 5. ✅ Audit Logging

```csharp
// UserRole va RolePermission'da "assigned_by" / "granted_by" field'lari mavjud
await _permissionService.AssignRoleToUserAsync(
    userId: 123,
    roleId: 1,
    assignedBy: currentUserId // Kim assign qildi?
);
```

---

## API Endpoints

### Permission Management

| Method | Endpoint | Description | Required Permission |
|--------|----------|-------------|---------------------|
| GET | `/api/permissions` | Barcha permission'lar | `permissions.view` |
| GET | `/api/permissions/roles` | Barcha rollar | `roles.view` |
| GET | `/api/permissions/roles/{roleId}/permissions` | Role permission'lari | `roles.view` |
| GET | `/api/permissions/users/{userId}/roles` | User rollari | `users.view` |
| GET | `/api/permissions/users/{userId}` | User permission'lari | `users.view` |
| POST | `/api/permissions/users/{userId}/roles/{roleId}` | User'ga rol biriktirish | `users.manage` |
| DELETE | `/api/permissions/users/{userId}/roles/{roleId}` | User'dan rol o'chirish | `users.manage` |
| POST | `/api/permissions/roles/{roleId}/permissions/{permissionId}` | Role'ga permission berish | `roles.assign_permissions` |
| DELETE | `/api/permissions/roles/{roleId}/permissions/{permissionId}` | Role'dan permission o'chirish | `roles.assign_permissions` |

---

## Troubleshooting

### Issue: "User does NOT have permission"

**Solution:**
1. User'ga role assign qilinganini tekshiring:
   ```sql
   SELECT * FROM user_roles WHERE user_id = 123;
   ```

2. Role'ga permission assign qilinganini tekshiring:
   ```sql
   SELECT p.name
   FROM role_permissions rp
   JOIN permissions p ON rp.permission_id = p.id
   WHERE rp.role_id = 1;
   ```

3. JWT token'da user_id mavjudligini tekshiring:
   ```bash
   # JWT decode qiling: https://jwt.io
   ```

### Issue: "PermissionSeedService failed"

**Solution:**
1. Database connection'ni tekshiring
2. Migration script run qilinganini tekshiring:
   ```bash
   psql -U postgres -d convoy_db -c "\dt" | grep roles
   ```

---

## Migration from Old System

Agar eski sistemangiz bo'lsa:

### 1. Eski User'larga Default Role Berish

```sql
-- Barcha mavjud user'larga "Driver" role'ni assign qilish
INSERT INTO user_roles (user_id, role_id, assigned_at, created_at)
SELECT u.id, r.id, NOW(), NOW()
FROM users u
CROSS JOIN roles r
WHERE r.name = 'Driver'
  AND NOT EXISTS (
    SELECT 1 FROM user_roles ur
    WHERE ur.user_id = u.id AND ur.role_id = r.id
  );
```

### 2. Position ID'ga Asoslangan Role Assignment

```sql
-- Position ID'ga qarab role berish (example)
-- Position 86 = Manager
INSERT INTO user_roles (user_id, role_id, assigned_at, created_at)
SELECT u.id, r.id, NOW(), NOW()
FROM users u
CROSS JOIN roles r
WHERE r.name = 'Manager'
  AND u.position_id = 86
  AND NOT EXISTS (
    SELECT 1 FROM user_roles ur
    WHERE ur.user_id = u.id
  );
```

---

## Future Enhancements

- [ ] Permission caching for better performance
- [ ] Permission inheritance (child permissions)
- [ ] Time-based permissions (temporary access)
- [ ] Permission groups/categories
- [ ] Audit log for permission changes
- [ ] UI for permission management (Admin panel)

---

**Happy Coding! 🚀**
