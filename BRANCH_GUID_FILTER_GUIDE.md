# Branch GUID Filtering Guide

## Overview

Multiple users' location endpoint now supports filtering by **branch_guid** in addition to explicit user IDs. This allows retrieving locations for all users belonging to a specific branch without manually providing their user IDs.

## Endpoint

**POST** `/api/locations/multiple_users`

## Request Format

You can use **EITHER** `user_ids` OR `branch_guid`:

### Option 1: Filter by User IDs

```json
{
  "user_ids": [123, 456, 789],
  "date": "2026-01-07",
  "start_time": "09:30",
  "end_time": "17:45",
  "limit": 100
}
```

### Option 2: Filter by Branch GUID

```json
{
  "branch_guid": "abc-123-def-456",
  "date": "2026-01-07",
  "start_time": "09:30",
  "end_time": "17:45",
  "limit": 100
}
```

## Field Descriptions

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `user_ids` | `int[]` | Conditional* | Array of user IDs to retrieve locations for |
| `branch_guid` | `string` | Conditional* | Branch GUID to filter users by |
| `date` | `DateTime` | Yes | Single date to retrieve locations for (YYYY-MM-DD) |
| `start_time` | `string` | No | Start time filter in HH:MM format (e.g., "09:30") |
| `end_time` | `string` | No | End time filter in HH:MM format (e.g., "17:45") |
| `limit` | `int` | No | Maximum locations per user (default: 100) |

**Note:** Either `user_ids` OR `branch_guid` must be provided (not both required, but at least one is mandatory)

## Response Format

```json
{
  "status": true,
  "message": "3 ta user (branch_guid=abc-123-def) uchun 2026-01-07 kunida 150 ta location olindi",
  "data": [
    {
      "id": 1,
      "user_id": 123,
      "recorded_at": "2026-01-07T10:30:00Z",
      "latitude": 41.311151,
      "longitude": 69.279737,
      "accuracy": 10.5,
      "speed": 5.2,
      ...
    },
    ...
  ]
}
```

## How It Works

### Branch GUID Filtering Logic

1. **Check which filter is provided**:
   - If `user_ids` array is provided and not empty → use these IDs directly
   - Else if `branch_guid` is provided → lookup all active users belonging to this branch
   - Else → return 400 Bad Request error

2. **User lookup by branch_guid**:
   - Queries `users` table for all **active users** (`is_active = true`) with matching `branch_guid`
   - Extracts user IDs: `SELECT id FROM users WHERE branch_guid = @BranchGuid AND is_active = true`
   - Returns empty list if no users found for that branch

3. **Location retrieval**:
   - Converts `date` to date range (00:00:00 to 23:59:59)
   - Applies time filters if provided (`start_time`, `end_time`)
   - Retrieves locations for all users using PostgreSQL `ANY(ARRAY[...])` syntax
   - Limits results per user using `ROW_NUMBER() OVER (PARTITION BY user_id)`

## Examples

### Example 1: Get all locations for branch on a specific day

**Request:**
```http
POST /api/locations/multiple_users
Content-Type: application/json
Authorization: Bearer {token}

{
  "branch_guid": "e5f7a9b2-c3d4-4e5f-a6b7-c8d9e0f1a2b3",
  "date": "2026-01-07"
}
```

**Response:**
```json
{
  "status": true,
  "message": "5 ta user (branch_guid=e5f7a9b2...) uchun 2026-01-07 kunida 347 ta location olindi",
  "data": [
    { "id": 1, "user_id": 10, ... },
    { "id": 2, "user_id": 10, ... },
    { "id": 3, "user_id": 15, ... },
    ...
  ]
}
```

### Example 2: Get locations for branch during work hours

**Request:**
```http
POST /api/locations/multiple_users
Content-Type: application/json
Authorization: Bearer {token}

{
  "branch_guid": "e5f7a9b2-c3d4-4e5f-a6b7-c8d9e0f1a2b3",
  "date": "2026-01-07",
  "start_time": "09:00",
  "end_time": "18:00",
  "limit": 50
}
```

**Response:**
```json
{
  "status": true,
  "message": "5 ta user (branch_guid=e5f7a9b2...) uchun 2026-01-07 kunida 187 ta location olindi",
  "data": [
    { "id": 1, "user_id": 10, "recorded_at": "2026-01-07T09:15:00Z", ... },
    { "id": 2, "user_id": 10, "recorded_at": "2026-01-07T09:30:00Z", ... },
    ...
  ]
}
```

### Example 3: No users found for branch

**Request:**
```http
POST /api/locations/multiple_users
Content-Type: application/json
Authorization: Bearer {token}

{
  "branch_guid": "non-existent-guid",
  "date": "2026-01-07"
}
```

**Response:**
```json
{
  "status": true,
  "message": "BranchGuid=non-existent-guid uchun userlar topilmadi",
  "data": []
}
```

### Example 4: Missing both user_ids and branch_guid

**Request:**
```http
POST /api/locations/multiple_users
Content-Type: application/json
Authorization: Bearer {token}

{
  "date": "2026-01-07"
}
```

**Response:**
```json
{
  "status": false,
  "message": "user_ids yoki branch_guid berilishi kerak",
  "data": null
}
```

## Database Schema

### Users Table

```sql
CREATE TABLE users (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    phone VARCHAR(20),
    branch_guid VARCHAR(100),  -- Branch identifier
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    deleted_at TIMESTAMPTZ
);

-- Index for fast branch lookups
CREATE INDEX idx_users_branch_guid ON users(branch_guid) WHERE is_active = true;
```

## Implementation Details

### Service Layer (LocationService.cs)

```csharp
public async Task<ServiceResult<IEnumerable<LocationResponseDto>>> GetMultipleUsersLocationsAsync(
    MultipleUsersLocationQueryDto query,
    IUserService? userService = null)
{
    List<int> userIds;

    // Step 1: Determine user IDs
    if (query.UserIds != null && query.UserIds.Any())
    {
        userIds = query.UserIds;
        _logger.LogInformation("Using provided user_ids: {UserIds}", string.Join(",", userIds));
    }
    else if (!string.IsNullOrWhiteSpace(query.BranchGuid))
    {
        if (userService == null)
        {
            return ServiceResult<IEnumerable<LocationResponseDto>>.ServerError(
                "UserService not available for branch lookup");
        }

        userIds = await userService.GetUserIdsByBranchGuidAsync(query.BranchGuid);

        if (!userIds.Any())
        {
            _logger.LogWarning("No users found for BranchGuid={BranchGuid}", query.BranchGuid);
            return ServiceResult<IEnumerable<LocationResponseDto>>.Ok(
                Enumerable.Empty<LocationResponseDto>(),
                $"BranchGuid={query.BranchGuid} uchun userlar topilmadi");
        }

        _logger.LogInformation("Found {Count} users for BranchGuid={BranchGuid}",
            userIds.Count, query.BranchGuid);
    }
    else
    {
        return ServiceResult<IEnumerable<LocationResponseDto>>.BadRequest(
            "user_ids yoki branch_guid berilishi kerak");
    }

    // Step 2: Convert date to range
    var startDate = query.Date.Date;
    var endDate = startDate.AddDays(1);

    // Step 3: Retrieve locations
    var locations = await _locationRepository.GetMultipleUsersLocationsAsync(
        userIds,
        startDate,
        endDate,
        query.StartTime,
        query.EndTime,
        query.Limit
    );

    var result = _mapper.Map<IEnumerable<LocationResponseDto>>(locations);

    var filterInfo = query.UserIds != null && query.UserIds.Any()
        ? $"{userIds.Count} ta user (user_ids)"
        : $"{userIds.Count} ta user (branch_guid={query.BranchGuid})";

    return ServiceResult<IEnumerable<LocationResponseDto>>.Ok(
        result,
        $"{filterInfo} uchun {query.Date:yyyy-MM-dd} kunida {result.Count()} ta location olindi");
}
```

### User Service (UserService.cs)

```csharp
public async Task<List<int>> GetUserIdsByBranchGuidAsync(string branchGuid)
{
    var userIds = await _context.Users
        .Where(u => u.BranchGuid == branchGuid && u.IsActive)
        .Select(u => (int)u.Id)
        .ToListAsync();

    _logger.LogInformation("Found {Count} users for BranchGuid={BranchGuid}",
        userIds.Count, branchGuid);

    return userIds;
}
```

### Controller Layer (LocationController.cs)

```csharp
[HttpPost("multiple_users")]
public async Task<IActionResult> GetMultipleUsersLocations([FromBody] MultipleUsersLocationQueryDto query)
{
    // Validation - user_ids YOKI branch_guid berilishi kerak
    var hasUserIds = query.UserIds != null && query.UserIds.Any();
    var hasBranchGuid = !string.IsNullOrWhiteSpace(query.BranchGuid);

    if (!hasUserIds && !hasBranchGuid)
    {
        return BadRequest(new ApiResponse<object>
        {
            Status = false,
            Message = "user_ids yoki branch_guid berilishi kerak",
            Data = null
        });
    }

    // Time format validation...

    var result = await _locationService.GetMultipleUsersLocationsAsync(query, _userService);

    var apiResponse = new ApiResponse<IEnumerable<LocationResponseDto>>
    {
        Status = result.Success,
        Message = result.Message,
        Data = result.Data
    };

    return StatusCode(result.StatusCode, apiResponse);
}
```

## Performance Considerations

1. **Branch Lookup**: Query uses index on `(branch_guid, is_active)` for fast user lookup
2. **Active Users Only**: Only retrieves active users to avoid deleted/inactive accounts
3. **Partition Pruning**: Single date filter ensures only one partition is scanned
4. **Per-User Limits**: `ROW_NUMBER()` efficiently limits results per user

## Common Use Cases

1. **Branch Manager Dashboard**: View all drivers in a branch during work hours
2. **Fleet Management**: Track all vehicles assigned to a specific branch
3. **Report Generation**: Generate daily activity reports for all branch employees
4. **Compliance Monitoring**: Verify work hours for all branch staff

## Error Handling

| Status Code | Scenario | Message |
|-------------|----------|---------|
| 400 | Neither user_ids nor branch_guid provided | "user_ids yoki branch_guid berilishi kerak" |
| 400 | Invalid time format | "start_time noto'g'ri formatda. Format: HH:MM" |
| 200 | No users found for branch | "BranchGuid=... uchun userlar topilmadi" (empty data array) |
| 200 | Success | "{count} ta user (branch_guid=...) uchun ... location olindi" |
| 500 | UserService unavailable | "UserService not available for branch lookup" |

## Testing

### Test 1: Valid branch_guid

```bash
curl -X POST "https://your-api.com/api/locations/multiple_users" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "branch_guid": "abc-123",
    "date": "2026-01-07",
    "start_time": "09:00",
    "end_time": "18:00"
  }'
```

### Test 2: Non-existent branch_guid

```bash
curl -X POST "https://your-api.com/api/locations/multiple_users" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "branch_guid": "non-existent",
    "date": "2026-01-07"
  }'
```

### Test 3: Missing both filters

```bash
curl -X POST "https://your-api.com/api/locations/multiple_users" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-01-07"
  }'
```

## Notes

- Only **active users** (`is_active = true`) are included in branch lookups
- Branch GUID matching is **exact** (case-sensitive)
- Empty results are returned with 200 OK status (not 404)
- All other filters (date, time, limit) apply equally to both user_ids and branch_guid modes
- The endpoint returns the same data structure regardless of which filter is used
