# Testing /api/auth/me Endpoint - NEW Format Guide

## Yangi Format

`/api/auth/me` endpoint endi **Flutter uchun qulay** format'da permission'larni qaytaradi:

### Response Format

```json
{
  "status": true,
  "message": "Foydalanuvchi ma'lumotlari",
  "data": {
    "user_id": 1,
    "name": "Test User",
    "phone": "998901234567",
    "image": null,
    "role": ["Driver", "Admin"],
    "role_id": [4, 1],
    "permissions": [
      {
        "users": ["view", "create", "update"]
      },
      {
        "locations": ["view", "create", "export"]
      },
      {
        "dashboard": ["view_own", "view_statistics"]
      }
    ]
  }
}
```

## Test Qilish

### 1. API'ni ishga tushirish

```bash
cd C:\Users\abdum\source\repos\ConvoyV2
dotnet run --project Convoy.Api
```

### 2. User'ga Role Assign Qilish

#### Option A: SQL orqali (PostgreSQL client bilan)

```sql
-- 1. User yaratish (agar mavjud bo'lmasa)
INSERT INTO users (id, name, phone, is_active, created_at)
VALUES (1, 'Test User', '998901234567', true, NOW())
ON CONFLICT (id) DO UPDATE
SET name = 'Test User',
    phone = '998901234567',
    is_active = true,
    updated_at = NOW();

-- 2. Driver role'ni assign qilish (role_id = 4)
INSERT INTO user_roles (user_id, role_id, assigned_at, created_at)
VALUES (1, 4, NOW(), NOW())
ON CONFLICT (user_id, role_id) DO NOTHING;

-- 3. Verification
SELECT r.name, COUNT(rp.id) as permission_count
FROM users u
JOIN user_roles ur ON u.id = ur.user_id
JOIN roles r ON ur.role_id = r.id
LEFT JOIN role_permissions rp ON r.id = rp.role_id
WHERE u.id = 1
GROUP BY r.name;
```

#### Option B: API endpoint orqali

```bash
# User'ga Driver role (id=4) berish
curl -X POST http://localhost:5084/api/permissions/users/1/roles/4 \
  -H "Authorization: Bearer {admin_token}" \
  -H "Content-Type: application/json"
```

### 3. Authentication Flow

#### Step 1: Verify Number

```bash
curl -X POST http://localhost:5084/api/auth/verify_number \
  -H "Content-Type: application/json" \
  -d '{"phone_number": "998901234567"}'
```

#### Step 2: Send OTP

```bash
curl -X POST http://localhost:5084/api/auth/send_otp \
  -H "Content-Type: application/json" \
  -d '{"phone_number": "998901234567"}'
```

**Console log'larni ko'ring** - OTP kod oq rangda chiqadi (development mode).

#### Step 3: Verify OTP va Token Olish

```bash
curl -X POST http://localhost:5084/api/auth/verify_otp \
  -H "Content-Type: application/json" \
  -d '{"phone_number": "998901234567", "otp_code": "XXXX"}'
```

Response:
```json
{
  "status": true,
  "message": "Muvaffaqiyatli tizimga kirildi",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

#### Step 4: Test /api/auth/me

```bash
curl -X GET http://localhost:5084/api/auth/me \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

Response:
```json
{
  "status": true,
  "message": "Foydalanuvchi ma'lumotlari",
  "data": {
    "user_id": 1,
    "name": "Test User",
    "phone": "998901234567",
    "image": null,
    "role": ["Driver"],
    "role_id": [4],
    "permissions": [
      {
        "locations": ["create", "view"]
      },
      {
        "dashboard": ["view_own"]
      }
    ]
  }
}
```

### 4. Python Test Script

```bash
# 1. Token olish (step 3'dan)
# 2. test_auth_me_new_format.py faylida token o'rniga qo'yish
# 3. Script'ni run qilish

python test_auth_me_new_format.py
```

## Flutter Integration

### Dart Model

```dart
class UserPermissionsResponse {
  final bool status;
  final String message;
  final UserPermissionsData? data;

  UserPermissionsResponse({
    required this.status,
    required this.message,
    this.data,
  });

  factory UserPermissionsResponse.fromJson(Map<String, dynamic> json) {
    return UserPermissionsResponse(
      status: json['status'] as bool,
      message: json['message'] as String,
      data: json['data'] != null
          ? UserPermissionsData.fromJson(json['data'])
          : null,
    );
  }
}

class UserPermissionsData {
  final int userId;
  final String name;
  final String phone;
  final String? image;
  final List<String> role;
  final List<int> roleId;
  final List<Map<String, List<String>>> permissions;

  UserPermissionsData({
    required this.userId,
    required this.name,
    required this.phone,
    this.image,
    required this.role,
    required this.roleId,
    required this.permissions,
  });

  factory UserPermissionsData.fromJson(Map<String, dynamic> json) {
    return UserPermissionsData(
      userId: json['user_id'] as int,
      name: json['name'] as String,
      phone: json['phone'] as String,
      image: json['image'] as String?,
      role: List<String>.from(json['role'] as List),
      roleId: List<int>.from(json['role_id'] as List),
      permissions: (json['permissions'] as List)
          .map((p) => Map<String, List<String>>.from(
                (p as Map).map((key, value) =>
                    MapEntry(key, List<String>.from(value as List))),
              ))
          .toList(),
    );
  }

  // Helper method - Permission tekshirish
  bool hasPermission(String resource, String action) {
    for (var permGroup in permissions) {
      if (permGroup.containsKey(resource)) {
        return permGroup[resource]!.contains(action);
      }
    }
    return false;
  }

  // Helper method - Barcha action'larni olish
  List<String> getActions(String resource) {
    for (var permGroup in permissions) {
      if (permGroup.containsKey(resource)) {
        return permGroup[resource]!;
      }
    }
    return [];
  }
}
```

### Usage Example

```dart
Future<void> loadUserData() async {
  final token = await storage.read(key: 'auth_token');

  final response = await http.get(
    Uri.parse('$baseUrl/api/auth/me'),
    headers: {
      'Authorization': 'Bearer $token',
      'Content-Type': 'application/json',
    },
  );

  if (response.statusCode == 200) {
    final data = UserPermissionsResponse.fromJson(
      jsonDecode(response.body),
    );

    if (data.status && data.data != null) {
      final user = data.data!;

      print('User: ${user.name}');
      print('Roles: ${user.role}');

      // Permission tekshirish
      if (user.hasPermission('locations', 'create')) {
        print('User can create locations');
      }

      // Specific resource'ning barcha action'lari
      final locationActions = user.getActions('locations');
      print('Location actions: $locationActions');

      // UI'da ko'rsatish
      setState(() {
        currentUser = user;
      });
    }
  }
}
```

## Expected Results

### Driver Role (Default)

```json
{
  "permissions": [
    {
      "locations": ["create", "view"]
    },
    {
      "dashboard": ["view_own"]
    }
  ]
}
```

### Admin Role

```json
{
  "permissions": [
    {
      "users": ["create", "update", "view"]
    },
    {
      "locations": ["export", "view", "view_all"]
    },
    {
      "reports": ["create", "export", "view"]
    },
    {
      "dashboard": ["view_all", "view_statistics"]
    },
    {
      "settings": ["view"]
    }
  ]
}
```

### SuperAdmin Role

```json
{
  "permissions": [
    // ... barcha 26 ta permission grouped by resource
  ]
}
```

## Troubleshooting

### Issue: permissions array bo'sh

**Solution:**
```sql
-- User'ga role assign qilinganini tekshiring
SELECT * FROM user_roles WHERE user_id = 1;

-- Agar bo'sh bo'lsa, role assign qiling
INSERT INTO user_roles (user_id, role_id, assigned_at, created_at)
VALUES (1, 4, NOW(), NOW());
```

### Issue: Token expired

**Solution:** Yangi token oling (steps 1-3'ni takrorlang)

### Issue: 401 Unauthorized

**Solution:**
- Token to'g'ri format'da borligini tekshiring: `Bearer {token}`
- Token blacklist'da yo'qligini tekshiring
- Token muddati tugamaganligini tekshiring

---

**Happy Testing! 🚀**
