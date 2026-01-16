# Permission System - Hard-coded Assignments Removed

## ✅ O'zgarishlar

### 1. `Roles.GetAll()` - Signature O'zgardi

**Eski (WRONG):**
```csharp
public static List<(string Name, string DisplayName, string Description, List<string> Permissions)> GetAll()
```

**Yangi (CORRECT):**
```csharp
public static List<(string Name, string DisplayName, string Description)> GetAll()
```

❌ **Hard-coded permission assignments olib tashlandi**
✅ **Faqat role metadata'si qaytariladi**

---

### 2. `PermissionSeedService` - Yangilandi

**O'zgarishlar:**

1. ✅ `SeedRolePermissionsAsync()` method **butunlay olib tashlandi**
2. ✅ `StartAsync()` method - role-permission seeding chaqirilmaydi
3. ✅ Log message qo'shildi: `"ℹ️ Role-Permission assignments should be done via Admin Panel"`

**Eski kod (REMOVED):**
```csharp
// Role-Permission bog'lanishlarini seed qilish
await SeedRolePermissionsAsync(context, cancellationToken);
```

**Yangi kod:**
```csharp
// Permissions'larni seed qilish
await SeedPermissionsAsync(context, cancellationToken);

// Roles'larni seed qilish (permissions bilan bog'lamay)
await SeedRolesAsync(context, cancellationToken);

_logger.LogInformation("✅ Permission seed completed successfully");
_logger.LogInformation("ℹ️  Role-Permission assignments should be done via Admin Panel");
```

---

## 📋 Endi Nima Qilinadi?

### Sistema Startup'da:

✅ **Permissions yaratiladi** - 26 ta permission database'ga qo'shiladi
✅ **Roles yaratiladi** - 5 ta role (SuperAdmin, Admin, Manager, Driver, Viewer)
❌ **Role-Permission bog'lanishlar yaratilmaydi** - Admin panel orqali qo'lda qilinadi

### Loglar:

```
info: 🌱 Permission seed service started
info: ✅ Permissions seeded: 26 permissions
info: ✅ Roles seeded: 5 roles (NO permissions assigned)
info: ✅ Permission seed completed successfully
info: ℹ️  Role-Permission assignments should be done via Admin Panel
```

---

## 🔧 Admin Panel orqali Permission Berish

### 1. API Endpoints (Mavjud)

#### Role'ga Permission Berish

```http
POST /api/permissions/roles/{roleId}/permissions/{permissionId}
Authorization: Bearer {admin_token}
```

**Example:**
```bash
# SuperAdmin role'ga (id=1) users.view permission'i (id=1) berish
curl -X POST http://localhost:5084/api/permissions/roles/1/permissions/1 \
  -H "Authorization: Bearer {admin_token}"
```

**Response:**
```json
{
  "status": true,
  "message": "Ruxsat muvaffaqiyatli biriktirildi",
  "data": {
    "id": 1,
    "role_id": 1,
    "permission_id": 1,
    "granted_at": "2025-12-28T05:00:00Z"
  }
}
```

#### User'ga Role Berish

```http
POST /api/permissions/users/{userId}/roles/{roleId}
Authorization: Bearer {admin_token}
```

**Example:**
```bash
# User (id=123)'ga Admin role (id=2) berish
curl -X POST http://localhost:5084/api/permissions/users/123/roles/2 \
  -H "Authorization: Bearer {admin_token}"
```

---

### 2. SQL orqali (Development/Testing)

#### Barcha Permission'larni Ko'rish

```sql
SELECT id, name, display_name, resource, action
FROM permissions
WHERE is_active = true
ORDER BY resource, action;
```

#### Barcha Role'larni Ko'rish

```sql
SELECT id, name, display_name, description
FROM roles
WHERE is_active = true;
```

#### Role'ga Permission Berish (Bulk)

```sql
-- Example: SuperAdmin'ga barcha permission'larni berish
INSERT INTO role_permissions (role_id, permission_id, granted_at, created_at)
SELECT
    1 as role_id,  -- SuperAdmin role_id
    p.id as permission_id,
    NOW() as granted_at,
    NOW() as created_at
FROM permissions p
WHERE p.is_active = true
ON CONFLICT (role_id, permission_id) DO NOTHING;
```

#### User'ga Role Berish

```sql
INSERT INTO user_roles (user_id, role_id, assigned_at, created_at)
VALUES (123, 1, NOW(), NOW())  -- User 123'ga SuperAdmin role
ON CONFLICT (user_id, role_id) DO NOTHING;
```

---

## 🎯 Best Practices

### ✅ DO (Qiling)

1. **Admin Panel orqali permission bering** - API endpoints yoki admin UI
2. **SuperAdmin role'ni birinchi yarating** - Bu role bilan boshqa rollarni boshqarasiz
3. **Permission'larni log qiling** - Kim, qachon, qaysi permission'ni berdi
4. **Role'larni saqlab qoling** - Production'da role o'chirish o'rniga `is_active = false` qiling

### ❌ DON'T (Qilmang)

1. ❌ **Hard-coded permission assignments** - Hech qachon kod ichida permission bog'lamang
2. ❌ **Database migration'da role-permission** - Faqat structure uchun migration, data uchun emas
3. ❌ **Default permission berish** - Har bir role uchun permission'lar admin tomonidan beriladi
4. ❌ **Permission constants'ni o'chirish** - `Permissions` class faqat reference uchun, o'chirish kerak emas

---

## 📊 Misol: Production Setup

### Step 1: Sistema Startup

```bash
dotnet run --project Convoy.Api
# Logs:
# ✅ Permissions seeded: 26 permissions
# ✅ Roles seeded: 5 roles (NO permissions assigned)
```

### Step 2: SuperAdmin Yaratish

```sql
-- 1. User yaratish
INSERT INTO users (name, phone, is_active, created_at)
VALUES ('Super Admin', '998901111111', true, NOW())
RETURNING id;  -- Masalan: 1

-- 2. SuperAdmin role'ni topish
SELECT id FROM roles WHERE name = 'SuperAdmin';  -- Masalan: 1

-- 3. User'ga SuperAdmin role berish
INSERT INTO user_roles (user_id, role_id, assigned_at, created_at)
VALUES (1, 1, NOW(), NOW());

-- 4. SuperAdmin role'ga barcha permission'larni berish
INSERT INTO role_permissions (role_id, permission_id, granted_at, created_at)
SELECT 1, p.id, NOW(), NOW()
FROM permissions p
WHERE p.is_active = true;
```

### Step 3: Boshqa Role'larga Permission Berish

```sql
-- Admin role'ga kerakli permission'larni berish
INSERT INTO role_permissions (role_id, permission_id, granted_at, created_at)
SELECT
    (SELECT id FROM roles WHERE name = 'Admin'),
    p.id,
    NOW(),
    NOW()
FROM permissions p
WHERE p.name IN (
    'users.view',
    'users.create',
    'users.update',
    'locations.view',
    'locations.view_all',
    'reports.view'
)
ON CONFLICT (role_id, permission_id) DO NOTHING;
```

### Step 4: User'larga Role Berish

API endpoint orqali yoki SQL:

```sql
-- User 123'ga Driver role berish
INSERT INTO user_roles (user_id, role_id, assigned_at, created_at)
SELECT 123, id, NOW(), NOW()
FROM roles
WHERE name = 'Driver';
```

---

## 🔍 Verification

### Role'ning Permission'larini Tekshirish

```sql
SELECT
    r.name as role_name,
    p.name as permission_name,
    p.display_name,
    rp.granted_at
FROM roles r
JOIN role_permissions rp ON r.id = rp.role_id
JOIN permissions p ON rp.permission_id = p.id
WHERE r.name = 'SuperAdmin'
ORDER BY p.resource, p.action;
```

### User'ning Barcha Permission'larini Ko'rish

```sql
SELECT DISTINCT
    u.name as user_name,
    r.name as role_name,
    p.name as permission_name,
    p.display_name
FROM users u
JOIN user_roles ur ON u.id = ur.user_id
JOIN roles r ON ur.role_id = r.id
JOIN role_permissions rp ON r.id = rp.role_id
JOIN permissions p ON rp.permission_id = p.id
WHERE u.id = 123  -- Your user ID
ORDER BY r.name, p.resource, p.action;
```

---

## 📝 Summary

### O'zgarishlar:

1. ✅ `Roles.GetAll()` - Permission list olib tashlandi
2. ✅ `PermissionSeedService` - Auto assignment olib tashlandi
3. ✅ Loglar yangilandi - "Admin Panel orqali qiling" xabari qo'shildi

### Endi Qanday Ishlaydi:

1. **Startup**: Faqat permissions va roles yaratiladi
2. **Manual Setup**: Admin panel yoki SQL orqali permission berish
3. **Flexible**: Har qanday role'ga istalgan permission'ni berish mumkin
4. **Auditable**: Kim, qachon permission berganini kuzatish mumkin

### Migration Path:

Agar sizda mavjud hard-coded assignments bo'lsa:

```sql
-- Barcha role-permission'larni o'chirish
DELETE FROM role_permissions;

-- Yangi permission'larni admin panel orqali berish
-- yoki yuqoridagi SQL query'larni ishlatish
```

---

**Happy Permission Managing! 🔐**
