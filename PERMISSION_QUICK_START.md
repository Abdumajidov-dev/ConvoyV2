# Permission System - Quick Start Guide

5 daqiqada Permission sistemasini ishga tushiring! 🚀

## 📝 Prerequisites

- ✅ PostgreSQL database running
- ✅ Convoy.Api application configured
- ✅ Basic understanding of ASP.NET Core

---

## 🚀 Setup (3 steps)

### Step 1: Run Database Migration

```bash
# PostgreSQL database'ga ulanish
psql -U postgres -d convoy_db

# Migration scriptni run qilish
\i add-permission-system.sql

# Yoki Windows CMD da:
psql -U postgres -d convoy_db -f add-permission-system.sql
```

**Result:**
```
✅ Created 4 tables: roles, permissions, user_roles, role_permissions
✅ Seeded 28 permissions
✅ Seeded 5 roles (SuperAdmin, Admin, Manager, Driver, Viewer)
✅ Created role-permission relationships
```

### Step 2: Verify Database

```sql
-- Rollarni ko'rish
SELECT name, display_name FROM roles;

-- Permission'larni ko'rish
SELECT resource, COUNT(*) FROM permissions GROUP BY resource;

-- Role-Permission bog'lanishlarni ko'rish
SELECT r.name, COUNT(rp.id)
FROM roles r
LEFT JOIN role_permissions rp ON r.id = rp.role_id
GROUP BY r.name;
```

### Step 3: Run Application

```bash
dotnet run --project Convoy.Api
```

**Check logs for:**
```
🌱 Permission seed service started
✅ Permissions seeded: 28 permissions
✅ Roles seeded: 5 roles
✅ Permission seed completed successfully
```

---

## 🎯 Usage Examples

### Example 1: User'ga Role Assign Qilish

```sql
-- 1. User yaratish (agar yo'q bo'lsa)
INSERT INTO users (name, username, phone, is_active, created_at)
VALUES ('John Driver', 'jdriver', '+998901234567', true, NOW())
RETURNING id;
-- Result: id = 1

-- 2. Driver role'ni topish
SELECT id FROM roles WHERE name = 'Driver';
-- Result: id = 4

-- 3. User'ga role assign qilish
INSERT INTO user_roles (user_id, role_id, assigned_at, created_at)
VALUES (1, 4, NOW(), NOW());
```

**Or via API:**
```bash
curl -X POST http://localhost:5084/api/permissions/users/1/roles/4 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Example 2: Controller'da Permission Ishlatish

```csharp
using Convoy.Api.Authorization;
using Convoy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/locations")]
public class LocationController : ControllerBase
{
    // Faqat "locations.view" permission'i bor userlar
    [HttpGet]
    [HasPermission(Permissions.Locations.View)]
    public async Task<IActionResult> GetLocations()
    {
        // Your logic here...
    }

    // Faqat "locations.create" permission'i bor userlar
    [HttpPost]
    [HasPermission(Permissions.Locations.Create)]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationDto dto)
    {
        // Your logic here...
    }

    // Faqat "locations.view_all" permission'i bor userlar
    [HttpGet("all")]
    [HasPermission(Permissions.Locations.ViewAll)]
    public async Task<IActionResult> GetAllLocations()
    {
        // Your logic here...
    }
}
```

### Example 3: Service Layer'da Permission Tekshirish

```csharp
public class LocationService : ILocationService
{
    private readonly IPermissionService _permissionService;

    public async Task<ServiceResult<Location>> DeleteLocationAsync(long locationId, long currentUserId)
    {
        // Permission tekshirish
        var hasPermission = await _permissionService.UserHasPermissionAsync(
            currentUserId,
            Permissions.Locations.Delete
        );

        if (!hasPermission)
        {
            return ServiceResult<Location>.Forbidden("Lokatsiyani o'chirish uchun ruxsat yo'q");
        }

        // Delete logic...
    }
}
```

---

## 📊 Default Roles & Permissions

| Role | Total Permissions | Key Permissions |
|------|-------------------|-----------------|
| **SuperAdmin** | 28 (ALL) | Everything |
| **Admin** | 11 | users.view/create/update, locations.view_all, reports.* |
| **Manager** | 7 | users.view, locations.view_all/export, reports.view/export |
| **Driver** | 3 | locations.view/create, dashboard.view_own |
| **Viewer** | 4 | *.view, dashboard.view_own |

### Permission Categories

**Users** (5):
- `users.view`, `users.create`, `users.update`, `users.delete`, `users.manage`

**Locations** (6):
- `locations.view`, `locations.create`, `locations.update`, `locations.delete`
- `locations.view_all`, `locations.export`

**Reports** (3):
- `reports.view`, `reports.export`, `reports.create`

**Roles** (5):
- `roles.view`, `roles.create`, `roles.update`, `roles.delete`, `roles.assign_permissions`

**Permissions** (2):
- `permissions.view`, `permissions.assign`

**Dashboard** (3):
- `dashboard.view_own`, `dashboard.view_all`, `dashboard.view_statistics`

**Settings** (2):
- `settings.view`, `settings.update`

---

## 🧪 Testing

### Test 1: User'ning Permission'larini Ko'rish

```sql
-- User 1'ning barcha permission'lari
SELECT DISTINCT p.name
FROM users u
JOIN user_roles ur ON u.id = ur.user_id
JOIN role_permissions rp ON ur.role_id = rp.role_id
JOIN permissions p ON rp.permission_id = p.id
WHERE u.id = 1 AND p.is_active = true
ORDER BY p.name;
```

### Test 2: API Testing

```bash
# 1. Login qilib JWT token oling
TOKEN=$(curl -s -X POST http://localhost:5084/api/auth/verify_otp \
  -H "Content-Type: application/json" \
  -d '{"phone_number": "+998901234567", "otp_code": "1234"}' \
  | jq -r '.data.token')

# 2. User'ning permission'larini ko'ring
curl -X GET http://localhost:5084/api/permissions/users/1 \
  -H "Authorization: Bearer $TOKEN"

# 3. Protected endpoint'ga kirish
curl -X GET http://localhost:5084/api/locations/user/1 \
  -H "Authorization: Bearer $TOKEN"
```

### Test 3: Authorization Test

```bash
# Permission BOR user - SUCCESS
curl -X GET http://localhost:5084/api/permissions/roles \
  -H "Authorization: Bearer $ADMIN_TOKEN"
# Response: 200 OK

# Permission YO'Q user - FORBIDDEN
curl -X GET http://localhost:5084/api/permissions/roles \
  -H "Authorization: Bearer $DRIVER_TOKEN"
# Response: 403 Forbidden
```

---

## 🔧 Common Tasks

### Task 1: Yangi Permission Qo'shish

```sql
-- 1. Permission yaratish
INSERT INTO permissions (name, display_name, resource, action, description, is_active, created_at)
VALUES ('invoices.view', 'View Invoices', 'invoices', 'view', 'Hisob-fakturalarni ko''rish', true, NOW())
RETURNING id;
-- Result: id = 29

-- 2. Admin role'ga berib qo'yish
INSERT INTO role_permissions (role_id, permission_id, granted_at, created_at)
SELECT r.id, 29, NOW(), NOW()
FROM roles r
WHERE r.name = 'Admin';
```

### Task 2: Yangi Role Yaratish

```sql
-- 1. Role yaratish
INSERT INTO roles (name, display_name, description, is_active, created_at)
VALUES ('Accountant', 'Accountant', 'Buxgalter - hisobotlar bilan ishlaydi', true, NOW())
RETURNING id;
-- Result: id = 6

-- 2. Permission'lar berish
INSERT INTO role_permissions (role_id, permission_id, granted_at, created_at)
SELECT 6, p.id, NOW(), NOW()
FROM permissions p
WHERE p.name IN ('reports.view', 'reports.export', 'reports.create', 'invoices.view');
```

### Task 3: User'ning Role'larini Ko'rish

```sql
SELECT r.name, r.display_name
FROM users u
JOIN user_roles ur ON u.id = ur.user_id
JOIN roles r ON ur.role_id = r.id
WHERE u.id = 1;
```

---

## ❌ Troubleshooting

### Problem: "User does NOT have permission"

**Solution 1:** User'ga role berilmagandir
```sql
SELECT * FROM user_roles WHERE user_id = 1;
-- Agar bo'sh bo'lsa, role assign qiling
INSERT INTO user_roles (user_id, role_id, assigned_at, created_at)
VALUES (1, 4, NOW(), NOW()); -- 4 = Driver
```

**Solution 2:** Role'ga permission berilmagandir
```sql
SELECT p.name
FROM role_permissions rp
JOIN permissions p ON rp.permission_id = p.id
WHERE rp.role_id = 4;
-- Kerakli permission yo'q bo'lsa, qo'shing
```

### Problem: "PermissionSeedService failed"

**Solution:** Database migration run qilinmagan
```bash
psql -U postgres -d convoy_db -f add-permission-system.sql
```

### Problem: "401 Unauthorized"

**Solution:** JWT token expired yoki invalid
```bash
# Yangi token oling
curl -X POST http://localhost:5084/api/auth/verify_otp \
  -H "Content-Type: application/json" \
  -d '{"phone_number": "+998901234567", "otp_code": "1234"}'
```

---

## 📚 Next Steps

1. **Read Full Documentation**: `PERMISSION_SYSTEM_GUIDE.md`
2. **Customize Permissions**: Add your own permissions in `Convoy.Domain.Constants.Permissions`
3. **Update Controllers**: Add `[HasPermission]` attributes to your endpoints
4. **Test Thoroughly**: Verify all role-permission combinations work correctly
5. **Monitor Logs**: Check permission authorization logs in production

---

## 🎉 Success!

Sizda endi professional RBAC (Role-Based Access Control) sistemasi tayyor!

**Key Points to Remember:**
- ✅ Permission naming: `<resource>.<action>` (e.g., `users.view`)
- ✅ Controller'larda: `[HasPermission(Permissions.Users.View)]`
- ✅ Service'larda: `await _permissionService.UserHasPermissionAsync(userId, permissionName)`
- ✅ Centralized constants: `Convoy.Domain.Constants.Permissions`

Happy Coding! 🚀
