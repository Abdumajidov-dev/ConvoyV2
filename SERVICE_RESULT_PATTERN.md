# Service Result Pattern

## Overview

Service layer va Controller layer orasidagi standart communication pattern.

## ServiceResult Wrapper

```csharp
public class ServiceResult<T>
{
    public bool Success { get; set; }         // true/false
    public string Message { get; set; }       // User-friendly message
    public T? Data { get; set; }              // Actual data or null
    public int StatusCode { get; set; }       // HTTP status code
}
```

## Service Layer Pattern

### ✅ Success Response

```csharp
// GET endpoints (200 OK)
return ServiceResult<LocationResponseDto>.Ok(
    data,
    "Location muvaffaqiyatli olindi");

// POST endpoints (200 OK - same as GET)
return ServiceResult<IEnumerable<LocationResponseDto>>.Created(
    createdData,
    "Location muvaffaqiyatli yaratildi");
```

### ❌ Error Response

```csharp
// Bad Request (400)
return ServiceResult<IEnumerable<LocationResponseDto>>.BadRequest(
    "Locations array bo'sh bo'lmasligi kerak");

// Not Found (404)
return ServiceResult<LocationResponseDto>.NotFound(
    "Location topilmadi");

// Server Error (500)
return ServiceResult<IEnumerable<LocationResponseDto>>.ServerError(
    "Location yaratishda xatolik yuz berdi");

// Unauthorized (401)
return ServiceResult<object>.Unauthorized(
    "Token noto'g'ri");
```

## Controller Layer Pattern

### Simple Mapping

```csharp
[HttpPost]
public async Task<IActionResult> CreateLocations([FromBody] UserLocationBatchDto dto)
{
    // Service'dan ServiceResult olish
    var result = await _locationService.CreateUserLocationBatchAsync(dto);

    // ApiResponse'ga mapping
    var apiResponse = new ApiResponse<IEnumerable<LocationResponseDto>>
    {
        Status = result.Success,
        Message = result.Message,
        Data = result.Data
    };

    // ServiceResult'dagi StatusCode ishlatish
    return StatusCode(result.StatusCode, apiResponse);
}
```

## Complete Example

### Service Method

```csharp
public async Task<ServiceResult<IEnumerable<LocationResponseDto>>> CreateUserLocationBatchAsync(UserLocationBatchDto dto)
{
    try
    {
        // Validation
        if (!dto.Locations.Any())
        {
            return ServiceResult<IEnumerable<LocationResponseDto>>.BadRequest(
                "Locations array bo'sh bo'lmasligi kerak");
        }

        // Business logic...
        var locations = ProcessLocations(dto);

        // Save to database
        await _repository.SaveAsync(locations);

        // Success response
        return ServiceResult<IEnumerable<LocationResponseDto>>.Created(
            locations,
            $"{locations.Count} ta location muvaffaqiyatli yaratildi");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating locations");

        // Error response
        return ServiceResult<IEnumerable<LocationResponseDto>>.ServerError(
            "Location yaratishda xatolik yuz berdi");
    }
}
```

### Controller Method

```csharp
[HttpPost]
[ProducesResponseType(typeof(ApiResponse<IEnumerable<LocationResponseDto>>), 200)]
[ProducesResponseType(typeof(ApiResponse<object>), 400)]
[ProducesResponseType(typeof(ApiResponse<object>), 500)]
public async Task<IActionResult> CreateLocations([FromBody] UserLocationBatchDto dto)
{
    var result = await _locationService.CreateUserLocationBatchAsync(dto);

    var apiResponse = new ApiResponse<IEnumerable<LocationResponseDto>>
    {
        Status = result.Success,
        Message = result.Message,
        Data = result.Data
    };

    return StatusCode(result.StatusCode, apiResponse);
}
```

### API Response

**Success (200 OK):**
```json
{
  "status": true,
  "message": "2 ta location muvaffaqiyatli yaratildi",
  "data": [
    {
      "id": 123,
      "user_id": 1,
      "latitude": 41.311151,
      ...
    }
  ]
}
```

**Error (400 Bad Request):**
```json
{
  "status": false,
  "message": "Locations array bo'sh bo'lmasligi kerak",
  "data": null
}
```

**Error (500 Internal Server Error):**
```json
{
  "status": false,
  "message": "Location yaratishda xatolik yuz berdi",
  "data": null
}
```

## Status Codes

| Code | Method | Description |
|------|--------|-------------|
| 200 | `ServiceResult<T>.Ok()` | GET success |
| 200 | `ServiceResult<T>.Created()` | POST success (returns 200, not 201) |
| 400 | `ServiceResult<T>.BadRequest()` | Validation error |
| 401 | `ServiceResult<T>.Unauthorized()` | Auth error |
| 404 | `ServiceResult<T>.NotFound()` | Resource not found |
| 500 | `ServiceResult<T>.ServerError()` | Internal server error |

## Benefits

1. ✅ **Consistent**: Har doim bir xil pattern
2. ✅ **Type-safe**: Generic wrapper
3. ✅ **Clear separation**: Service layer va Controller layer ajratilgan
4. ✅ **Exception handling**: Service layer'da exception'lar handle qilinadi
5. ✅ **HTTP codes**: To'g'ri status code'lar avtomatik
6. ✅ **User-friendly**: Message'lar tushunarli (O'zbek tilida)

## Migration Pattern

### Old (Exception-based)

```csharp
// Service
public async Task<IEnumerable<LocationResponseDto>> GetLocations()
{
    var data = await _repository.GetAsync();
    return data.Select(MapToDto);  // Exception throw qiladi
}

// Controller
try {
    var result = await _service.GetLocations();
    return Ok(result);  // Status code controller'da
} catch (Exception ex) {
    return BadRequest(ex.Message);  // Error handling controller'da
}
```

### New (ServiceResult-based)

```csharp
// Service
public async Task<ServiceResult<IEnumerable<LocationResponseDto>>> GetLocations()
{
    try {
        var data = await _repository.GetAsync();
        return ServiceResult<IEnumerable<LocationResponseDto>>.Ok(
            data.Select(MapToDto),
            "Ma'lumotlar muvaffaqiyatli olindi");
    } catch (Exception ex) {
        _logger.LogError(ex, "Error");
        return ServiceResult<IEnumerable<LocationResponseDto>>.ServerError(
            "Xatolik yuz berdi");
    }
}

// Controller
var result = await _service.GetLocations();
var apiResponse = new ApiResponse<IEnumerable<LocationResponseDto>> {
    Status = result.Success,
    Message = result.Message,
    Data = result.Data
};
return StatusCode(result.StatusCode, apiResponse);
```

---

**Happy Coding! 🚀**
