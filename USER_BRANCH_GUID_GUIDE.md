# User Branch GUID Integration

## Overview

User table'ga `branch_guid` column qo'shildi va barcha user'larni olishda PHP API'dan branch ma'lumotlari ham qaytariladi.

## Changes Made

### 1. Database Migration

**File:** `add-branch-guid-to-users.sql`

```sql
ALTER TABLE users
ADD COLUMN IF NOT EXISTS branch_guid VARCHAR(255);

CREATE INDEX IF NOT EXISTS idx_users_branch_guid ON users(branch_guid);
```

**Execution:**
```bash
python run_add_branch_guid_migration.py
```

**Result:** ✅ Column `branch_guid` added successfully

### 2. User Entity Update

**File:** `Convoy.Domain/Entities/User.cs` (Line 21-22)

```csharp
[Column("branch_guid")]
public string? BranchGuid { get; set; }
```

### 3. UserResponseDto Update

**File:** `Convoy.Service/DTOs/UserDtos.cs` (Lines 58-62)

```csharp
[JsonPropertyName("branch_guid")]
public string? BranchGuid { get; set; }

[JsonPropertyName("branch")]
public BranchDto? Branch { get; set; }
```

### 4. UserService Update

**File:** `Convoy.Service/Services/UserService.cs`

#### Constructor Injection
```csharp
private readonly IPhpApiService _phpApiService;

public UserService(
    AppDbConText context,
    IRepository<User> userRepository,
    ILogger<UserService> logger,
    IMapper mapper,
    IPhpApiService phpApiService)  // ← Yangi dependency
{
    _phpApiService = phpApiService;
}
```

#### GetAllUsersAsync Logic (Lines 85-119)
```csharp
// 1. User'larni database'dan olish (branch_guid bilan)
var users = await usersQuery
    .Select(u => new UserResponseDto
    {
        BranchGuid = u.BranchGuid,  // ← Branch GUID ni olish
        // ... boshqa field'lar
    })
    .ToListAsync();

// 2. Branch ma'lumotlarini PHP API'dan olish
var branches = await _phpApiService.GetBranchesAsync();

// 3. User'larga branch ma'lumotlarini biriktirish
foreach (var user in users)
{
    if (branchDict.TryGetValue(user.BranchGuid, out var branch))
    {
        user.Branch = branch;  // ← Branch object to'ldirish
    }
}
```

### 5. AutoMapper Profile Update

**File:** `Convoy.Service/Mapping/MappingProfile.cs` (Lines 28-29)

```csharp
CreateMap<User, UserResponseDto>()
    .ForMember(dest => dest.BranchGuid, opt => opt.MapFrom(src => src.BranchGuid))
    .ForMember(dest => dest.Branch, opt => opt.Ignore())  // PHP API'dan olinadi
```

## API Response Format

### GET /api/users

**Request:**
```http
POST /api/users
Content-Type: application/json

{
  "page": 1,
  "page_size": 10
}
```

**Response:**
```json
{
  "status": true,
  "message": "10 ta user topildi",
  "data": [
    {
      "id": 1,
      "name": "John Doe",
      "phone": "+998901234567",
      "branch_guid": "abc123-def456-ghi789",
      "branch": {
        "id": 5,
        "name": "Toshkent Filial",
        "code": "abc123-def456-ghi789",
        "state_id": 1,
        "state_name": "Toshkent",
        "region_id": 10,
        "region_name": "Chilonzor",
        "address": "Chilonzor ko'chasi 123",
        "phone_number": "+998712345678",
        "location": "41.311151,69.279737",
        "responsible_worker": "Manager Name"
      },
      "is_active": true,
      "created_at": "2026-01-01T00:00:00Z",
      "updated_at": "2026-01-06T10:00:00Z"
    },
    {
      "id": 2,
      "name": "Jane Smith",
      "phone": "+998902345678",
      "branch_guid": null,  // Branch mavjud emas
      "branch": null,       // Branch ma'lumoti yo'q
      "is_active": true,
      "created_at": "2026-01-02T00:00:00Z",
      "updated_at": null
    }
  ],
  "total_count": 100,
  "page": 1,
  "page_size": 10,
  "total_pages": 10,
  "has_next_page": true,
  "has_previous_page": false
}
```

## How It Works

### 1. Database Query

```csharp
var users = await _context.Users
    .Select(u => new UserResponseDto
    {
        Id = u.Id,
        Name = u.Name,
        BranchGuid = u.BranchGuid  // Database'dan olinadi
    })
    .ToListAsync();
```

### 2. Branch Lookup

```csharp
// User'lardagi unique branch GUID'larni olish
var branchGuids = users
    .Where(u => !string.IsNullOrWhiteSpace(u.BranchGuid))
    .Select(u => u.BranchGuid!)
    .Distinct()
    .ToList();

// PHP API'dan branch'larni olish
var branches = await _phpApiService.GetBranchesAsync();

// Dictionary'ga joylash (tez lookup uchun)
var branchDict = branches
    .ToDictionary(b => b.Code!, b => b);  // Code = GUID
```

### 3. Branch Assignment

```csharp
foreach (var user in users)
{
    if (branchDict.TryGetValue(user.BranchGuid, out var branch))
    {
        user.Branch = branch;  // Match topilsa, assign qilinadi
    }
    // Match topilmasa, Branch = null bo'lib qoladi
}
```

## Field Descriptions

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `branch_guid` | string? | No | PHP API'dan kelgan branch GUID (nullable) |
| `branch` | BranchDto? | No | Branch to'liq ma'lumotlari (PHP API'dan olinadi) |

### BranchDto Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | int | Branch ID (PHP database'dan) |
| `name` | string | Branch nomi (e.g., "Toshkent Filial") |
| `code` | string | Branch GUID (user.branch_guid bilan match qilish uchun) |
| `state_id` | int? | Viloyat ID |
| `state_name` | string? | Viloyat nomi |
| `region_id` | int? | Tuman ID |
| `region_name` | string? | Tuman nomi |
| `address` | string? | Manzil |
| `phone_number` | string? | Telefon raqam |
| `location` | string? | GPS coordinates |
| `responsible_worker` | string? | Mas'ul xodim |

## Error Handling

### PHP API Unavailable

Agar PHP API ishlamasa yoki branch'larni olmasa:

```csharp
try
{
    var branches = await _phpApiService.GetBranchesAsync();
    // ... branch assignment logic
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Branch ma'lumotlarini olishda xatolik");
    // User'lar branch'siz qaytariladi (Branch = null)
}
```

**Result:** User'lar qaytariladi, lekin `branch` field `null` bo'ladi.

### Branch Not Found

Agar user'ning `branch_guid` mavjud, lekin PHP API'da topilmasa:

```json
{
  "id": 3,
  "name": "User Name",
  "branch_guid": "invalid-guid-123",
  "branch": null,  // Match topilmadi
  "is_active": true
}
```

## Performance Considerations

### Optimization

1. ✅ **Bulk Fetch**: Barcha branch'lar bir marta olinadi (N+1 problem yo'q)
2. ✅ **Dictionary Lookup**: O(1) lookup time using GUID as key
3. ✅ **Distinct GUIDs**: Faqat unique GUID'lar uchun lookup
4. ✅ **Null Check**: Branch'siz user'lar uchun API call'siz skip

### Query Counts

```
Users count: N users
Branch GUIDs: M unique branch_guid values (M <= N)
API calls: 1 call to GetBranchesAsync() (all branches)
Dictionary lookups: N lookups (very fast)
```

**Example:**
- 100 users
- 10 unique branch_guid values
- 1 PHP API call (fetches all branches)
- 100 dictionary lookups (instant)

## Database Schema

```sql
users
├── id SERIAL PRIMARY KEY
├── name VARCHAR(200)
├── username VARCHAR(100)
├── phone VARCHAR(20)
├── branch_guid VARCHAR(255) NULL  ← NEW COLUMN
├── is_active BOOLEAN
├── created_at TIMESTAMPTZ
├── updated_at TIMESTAMPTZ
└── deleted_at TIMESTAMPTZ

-- Index for faster lookups
CREATE INDEX idx_users_branch_guid ON users(branch_guid);
```

## Migration Files

1. **add-branch-guid-to-users.sql** - SQL migration script
2. **run_add_branch_guid_migration.py** - Python migration runner

## Testing

### Test User Without Branch

```sql
UPDATE users
SET branch_guid = NULL
WHERE id = 1;
```

**Expected:** `branch` field should be `null` in response

### Test User With Branch

```sql
UPDATE users
SET branch_guid = 'valid-guid-from-php-api'
WHERE id = 2;
```

**Expected:** `branch` field should contain full branch data

### Test PHP API Failure

Stop PHP API or set wrong URL.

**Expected:** Users returned with `branch = null`, warning logged

## Related Documentation

- **BranchDto.cs** - Branch DTO definition
- **IPhpApiService.cs** - PHP API service interface
- **API_RESPONSE_FORMAT.md** - API response examples
- **SNAKE_CASE_API_GUIDE.md** - JSON naming convention

## Summary

✅ **Database:** `branch_guid` column added to users table
✅ **Entity:** User entity updated with BranchGuid property
✅ **DTO:** UserResponseDto includes branch_guid and branch fields
✅ **Service:** UserService fetches branch data from PHP API
✅ **Mapping:** AutoMapper configured to map branch_guid
✅ **Response:** Users returned with full branch information

**Branch ma'lumotlari PHP API'dan avtomatik olinadi va user'larga biriktiriladi!**
