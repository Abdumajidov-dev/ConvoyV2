# Branch API Guide

## Overview

Bu endpoint PHP API'dan filiallar ro'yxatini olib keladi va Flutter ilovasiga taqdim etadi. Backend PHP API bilan bog'lanadi va ma'lumotlarni proxy qiladi.

## Architecture

```
Flutter Client → C# API → PHP API (branch-list)
                   ↓
            Basic Auth (appsettings.json)
```

## Configuration

### appsettings.json

```json
{
  "PhpApi": {
    "GlobalPathForSupport": "https://garant-hr.uz/api/",
    "Username": "login",
    "Password": "password"
  }
}
```

**IMPORTANT:** Bu credentials PHP API uchun Basic Authentication ishlatiladi.

## API Endpoint

### POST /api/branches/branch-list

Filiallar ro'yxatini olish (search bilan yoki search'siz).

**Authentication:** Talab qilinmaydi (public endpoint)

**Method:** POST (search parameter bilan)

**Request (Search bilan):**
```http
POST /api/branches/branch-list HTTP/1.1
Host: localhost:5084
Content-Type: application/json

{
  "search": "Наманган"
}
```

**Request (Barcha filiallar - turli variantlar):**

Quyidagi variantlarning barchasi HAM barcha filiallarni qaytaradi:

```http
# Variant 1: Bo'sh object
POST /api/branches/branch-list HTTP/1.1
Content-Type: application/json

{}

# Variant 2: Bo'sh string
POST /api/branches/branch-list HTTP/1.1
Content-Type: application/json

{"search": ""}

# Variant 3: Null
POST /api/branches/branch-list HTTP/1.1
Content-Type: application/json

{"search": null}

# Variant 4: Whitespace only
POST /api/branches/branch-list HTTP/1.1
Content-Type: application/json

{"search": "   "}
```

**Note:** `string.IsNullOrWhiteSpace()` orqali barcha bo'sh variantlar bitta kabi handle qilinadi.

**Response (Success with search):**
```json
{
  "status": true,
  "message": "'Наманган' bo'yicha 2 ta filial topildi",
  "data": [
    {
      "id": 1,
      "name": "Наманган филиали",
      "address": "Наманган ш., ...",
      "phone": "+998692345678",
      "guid": "550e8400-e29b-41d4-a716-446655440000",
      "is_active": true
    }
  ]
}
```

**Response (Success without search):**
```json
{
  "status": true,
  "message": "5 ta filial topildi",
  "data": [
    {
      "id": 1,
      "name": "Toshkent filiali",
      "address": "Toshkent sh., Chilonzor tumani",
      "phone": "+998712345678",
      "guid": "550e8400-e29b-41d4-a716-446655440000",
      "is_active": true
    },
    {
      "id": 2,
      "name": "Samarqand filiali",
      "address": "Samarqand sh., Registon ko'chasi",
      "phone": "+998662345678",
      "guid": "550e8400-e29b-41d4-a716-446655440001",
      "is_active": true
    }
  ]
}
```

**Response (Empty):**
```json
{
  "status": true,
  "message": "Filiallar topilmadi",
  "data": []
}
```

**Response (Error):**
```json
{
  "status": false,
  "message": "Filiallar ro'yxatini olishda xatolik yuz berdi",
  "data": []
}
```

**Status Codes:**
- `200 OK` - Muvaffaqiyatli (filiallar topildi yoki bo'sh ro'yxat)
- `500 Internal Server Error` - Server xatosi

## Data Structure

### BranchDto

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | int | Yes | Filial ID (PHP database'dan) |
| `name` | string | Yes | Filial nomi |
| `code` | string | No | Filial kodi (masalan: "000000003") |
| `state_id` | int | No | Viloyat ID |
| `state_name` | string | No | Viloyat nomi |
| `region_id` | int | No | Tuman ID |
| `region_name` | string | No | Tuman nomi |
| `address` | string | No | Filial manzili |
| `phone_number` | string | No | Filial telefon raqami |
| `target` | string | No | Manzil orientiri (landmark) |
| `location` | string | No | Google Maps havolasi |
| `responsible_worker` | string | No | Mas'ul xodim |
| `date` | string | No | Yaratilgan sana (format: "DD.MM.YYYY HH:mm") |

## PHP API Integration

### PHP Endpoint

**With Search (POST):**
```
POST https://garant-hr.uz/api/branch-list
Authorization: Basic {base64(username:password)}
Content-Type: application/json

{
  "search": "Наманган"
}
```

**Without Search (GET fallback):**
```
GET https://garant-hr.uz/api/branch-list
Authorization: Basic {base64(username:password)}
```

### PHP Response Format

PHP API `snake_case` formatda JSON qaytaradi va `{status, data}` wrapper'da:

```json
{
  "status": true,
  "data": [
    {
      "id": 3,
      "name": "Наманган",
      "code": "000000003",
      "state_id": 3,
      "state_name": "Наманган",
      "region_id": 35,
      "region_name": "Наманган",
      "address": "Namangan shaxar Hamrox kochasi 96 A uy",
      "phone_number": null,
      "target": "Obl bolnitsa yoki Jinoyat Sudi oldida...",
      "location": "https://maps.google.com/maps?q=41.003424,71.659217...",
      "responsible_worker": null,
      "date": "22.02.2023 07:56"
    }
  ]
}
```

**Note:** C# backend avtomatik ravishda:
1. `{status, data}` wrapper'ni parse qiladi
2. snake_case → camelCase konvertatsiya qiladi (JsonNamingPolicy.SnakeCaseLower)
3. Faqat `data` array'ini qaytaradi

## Flutter Integration

### Dart Model

```dart
class Branch {
  final int id;
  final String name;
  final String? code;
  final int? stateId;
  final String? stateName;
  final int? regionId;
  final String? regionName;
  final String? address;
  final String? phoneNumber;
  final String? target;
  final String? location;
  final String? responsibleWorker;
  final String? date;

  Branch({
    required this.id,
    required this.name,
    this.code,
    this.stateId,
    this.stateName,
    this.regionId,
    this.regionName,
    this.address,
    this.phoneNumber,
    this.target,
    this.location,
    this.responsibleWorker,
    this.date,
  });

  factory Branch.fromJson(Map<String, dynamic> json) {
    return Branch(
      id: json['id'],
      name: json['name'],
      code: json['code'],
      stateId: json['state_id'],
      stateName: json['state_name'],
      regionId: json['region_id'],
      regionName: json['region_name'],
      address: json['address'],
      phoneNumber: json['phone_number'],
      target: json['target'],
      location: json['location'],
      responsibleWorker: json['responsible_worker'],
      date: json['date'],
    );
  }

  /// Parse Google Maps URL and return LatLng
  LatLng? get coordinates {
    if (location == null) return null;

    // Extract coordinates from URL like:
    // https://maps.google.com/maps?q=41.003424,71.659217...
    final regex = RegExp(r'q=([-\d.]+),([-\d.]+)');
    final match = regex.firstMatch(location!);

    if (match != null) {
      final lat = double.tryParse(match.group(1) ?? '');
      final lng = double.tryParse(match.group(2) ?? '');
      if (lat != null && lng != null) {
        return LatLng(lat, lng);
      }
    }

    return null;
  }
}
```

### Service Class

```dart
class BranchService {
  final String baseUrl;
  final http.Client client;

  BranchService({required this.baseUrl, required this.client});

  /// Get all branches or search by term
  Future<List<Branch>> getBranches({String? searchTerm}) async {
    try {
      final response = await client.post(
        Uri.parse('$baseUrl/api/branches/branch-list'),
        headers: {'Content-Type': 'application/json'},
        body: searchTerm != null && searchTerm.isNotEmpty
            ? jsonEncode({'search': searchTerm})
            : jsonEncode({}),
      );

      if (response.statusCode == 200) {
        final Map<String, dynamic> jsonResponse = jsonDecode(response.body);

        if (jsonResponse['status'] == true) {
          final List<dynamic> branchesJson = jsonResponse['data'];
          return branchesJson.map((json) => Branch.fromJson(json)).toList();
        } else {
          throw Exception(jsonResponse['message']);
        }
      } else {
        throw Exception('Failed to load branches: ${response.statusCode}');
      }
    } catch (e) {
      print('Error fetching branches: $e');
      rethrow;
    }
  }

  /// Get all branches
  Future<List<Branch>> getAllBranches() async {
    return getBranches();
  }

  /// Search branches by term
  Future<List<Branch>> searchBranches(String searchTerm) async {
    return getBranches(searchTerm: searchTerm);
  }

  /// Get only active branches
  Future<List<Branch>> getActiveBranches({String? searchTerm}) async {
    final branches = await getBranches(searchTerm: searchTerm);
    return branches.where((branch) => branch.isActive).toList();
  }
}
```

### UI Example

```dart
class BranchListScreen extends StatefulWidget {
  @override
  _BranchListScreenState createState() => _BranchListScreenState();
}

class _BranchListScreenState extends State<BranchListScreen> {
  late Future<List<Branch>> _branchesFuture;
  final BranchService _branchService = BranchService(
    baseUrl: 'http://your-api-url.com',
    client: http.Client(),
  );

  @override
  void initState() {
    super.initState();
    _branchesFuture = _branchService.getBranches();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Filiallar')),
      body: FutureBuilder<List<Branch>>(
        future: _branchesFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return Center(child: CircularProgressIndicator());
          }

          if (snapshot.hasError) {
            return Center(
              child: Text('Xatolik: ${snapshot.error}'),
            );
          }

          if (!snapshot.hasData || snapshot.data!.isEmpty) {
            return Center(child: Text('Filiallar topilmadi'));
          }

          final branches = snapshot.data!;
          return ListView.builder(
            itemCount: branches.length,
            itemBuilder: (context, index) {
              final branch = branches[index];
              return ListTile(
                leading: Icon(
                  branch.isActive ? Icons.check_circle : Icons.cancel,
                  color: branch.isActive ? Colors.green : Colors.red,
                ),
                title: Text(branch.name),
                subtitle: branch.address != null
                    ? Text(branch.address!)
                    : null,
                trailing: branch.phone != null
                    ? Text(branch.phone!)
                    : null,
              );
            },
          );
        },
      ),
    );
  }
}
```

## Error Handling

### Common Errors

1. **PHP API Unreachable**
   - Symptom: `500 Internal Server Error`
   - Log: "Error calling PHP API branch-list endpoint"
   - Solution: Check `PhpApi:GlobalPathForSupport` in appsettings.json

2. **Authentication Failed**
   - Symptom: `401 Unauthorized` from PHP
   - Log: "PHP API returned status code 401"
   - Solution: Check `PhpApi:Username` and `PhpApi:Password`

3. **Invalid JSON Response**
   - Symptom: Empty data array
   - Log: "No branches found in PHP API response"
   - Solution: Check PHP API response format

### Logging

Service logs useful information:

```csharp
// Success
_logger.LogInformation("Successfully retrieved {Count} branches from PHP API", branches.Count);

// Warning
_logger.LogWarning("PHP API returned status code {StatusCode} for branch-list", response.StatusCode);

// Error
_logger.LogError(ex, "Error calling PHP API branch-list endpoint");
```

## Testing

### Manual Test (curl)

```bash
# Test C# API - All branches (empty body)
curl -X POST http://localhost:5084/api/branches/branch-list \
  -H "Content-Type: application/json" \
  -d '{}'

# Test C# API - Search by term
curl -X POST http://localhost:5084/api/branches/branch-list \
  -H "Content-Type: application/json" \
  -d '{"search": "Наманган"}'

# Test PHP API directly - All branches (GET)
curl -X GET https://garant-hr.uz/api/branch-list \
  -H "Authorization: Basic $(echo -n 'login:password' | base64)"

# Test PHP API directly - Search (POST)
curl -X POST https://garant-hr.uz/api/branch-list \
  -H "Authorization: Basic $(echo -n 'login:password' | base64)" \
  -H "Content-Type: application/json" \
  -d '{"search": "Наманган"}'
```

### Expected Behavior

1. ✅ Returns list of branches
2. ✅ Returns empty array if no branches
3. ✅ Returns 500 if PHP API fails
4. ✅ Logs all requests/responses
5. ✅ Handles network errors gracefully

## Caching (Optional Enhancement)

Currently, endpoint fetches fresh data on every request. Consider adding caching:

```csharp
// In-memory cache (10 minutes)
private static List<BranchDto>? _cachedBranches;
private static DateTime _cacheExpiry = DateTime.MinValue;

public async Task<List<BranchDto>> GetBranchesAsync()
{
    // Check cache
    if (_cachedBranches != null && DateTime.UtcNow < _cacheExpiry)
    {
        _logger.LogInformation("Returning {Count} branches from cache", _cachedBranches.Count);
        return _cachedBranches;
    }

    // Fetch from PHP API
    var branches = await FetchBranchesFromPhpApi();

    // Update cache
    _cachedBranches = branches;
    _cacheExpiry = DateTime.UtcNow.AddMinutes(10);

    return branches;
}
```

## Security Notes

1. **Public Endpoint**: No authentication required (accessible by anyone)
2. **PHP Credentials**: Stored in appsettings.json (server-side only)
3. **HTTPS**: Always use HTTPS in production
4. **Rate Limiting**: Consider adding rate limiting for production

## Related Documentation

- `CLAUDE.md` - Project architecture and patterns
- `API_RESPONSE_FORMAT.md` - Standard API response format
- `SNAKE_CASE_API_GUIDE.md` - JSON naming conventions

---

**Last Updated:** 2026-01-05
**Version:** 1.0
