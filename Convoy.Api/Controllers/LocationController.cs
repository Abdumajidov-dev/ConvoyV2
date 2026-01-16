using Convoy.Api.Models;
using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Convoy.Api.Controllers;

/// <summary>
/// Location tracking API controller
/// </summary>
[ApiController]
[Route("api/locations")]
[Authorize] // Barcha endpoint'lar authentication talab qiladi
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LocationController> _logger;

    public LocationController(
        ILocationService locationService,
        IUserService userService,
        ITokenService tokenService,
        ILogger<LocationController> logger)
    {
        _locationService = locationService;
        _userService = userService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Location yaratish (wrapped format, user_id JWT tokendan olinadi)
    /// POST /api/locations
    ///
    /// ENCRYPTION:
    /// - Agar encryption enabled bo'lsa: body shifrlangan JSON object (middleware yechib beradi)
    /// - Agar encryption disabled bo'lsa: body oddiy JSON object
    ///
    /// BODY FORMAT: Wrapped format (Flutter default)
    /// {
    ///   "locations": {
    ///     "latitude": 41.311151,
    ///     "longitude": 69.279737,
    ///     ...
    ///   }
    /// }
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LocationResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateLocation([FromBody] LocationRequestWrapperDto request)
    {
        // JWT tokendan user_id ni olish (TokenService orqali)
        var userId = _tokenService.GetUserIdFromClaims(User);
        if (userId == null || userId == 0)
        {
            _logger.LogWarning("Invalid or missing user_id claim in JWT token");
            return Unauthorized(new ApiResponse<object>
            {
                Status = false,
                Message = "Noto'g'ri yoki mavjud bo'lmagan user_id token'da",
                Data = null
            });
        }

        // Validate request
        if (request?.Location == null)
        {
            return BadRequest(new ApiResponse<object>
            {
                Status = false,
                Message = "Location object bo'sh yoki mavjud emas",
                Data = null
            });
        }

        var flutter = request.Location;

        // Validate coords
        //if (flutter.Coords == null)
        //{
        //    return BadRequest(new ApiResponse<object>
        //    {
        //        Status = false,
        //        Message = "Coords object bo'sh yoki mavjud emas",
        //        Data = null
        //    });
        //}

        // Validate and sanitize battery level (0-100 oralig'ida bo'lishi kerak)
        int? batteryLevel = null;
        //if (flutter.Battery?.Level != null)
        //{
        //    var level = flutter.Battery.Level.Value;
        //    if (level >= 0 && level <= 100)
        //    {
        //        batteryLevel = level;
        //    }
        //    else
        //    {
        //        _logger.LogWarning("Invalid battery level {Level} for UserId={UserId}, setting to null", level, userId.Value);
        //    }
        //}

        // Map Flutter format to LocationDataDto
        //var locationData = new LocationDataDto
        //{
        //    // Core location from coords
        //    Latitude = flutter.Coords.Latitude,
        //    Longitude = flutter.Coords.Longitude,
        //    Accuracy = flutter.Coords.Accuracy,
        //    Speed = flutter.Coords.Speed,
        //    Heading = flutter.Coords.Heading,
        //    Altitude = flutter.Coords.Altitude,

        //    // Extended coords
        //    EllipsoidalAltitude = flutter.Coords.EllipsoidalAltitude,
        //    HeadingAccuracy = flutter.Coords.HeadingAccuracy,
        //    SpeedAccuracy = flutter.Coords.SpeedAccuracy,
        //    AltitudeAccuracy = flutter.Coords.AltitudeAccuracy,

        //    // Activity
        //    ActivityType = flutter.Activity?.Type,
        //    ActivityConfidence = flutter.Activity?.Confidence,
        //    IsMoving = flutter.IsMoving,

        //    // Battery (validated)
        //    //BatteryLevel = batteryLevel,
        //    //IsCharging = flutter.Battery?.IsCharging,

        //    // Metadata
        //    RecordedAt = flutter.RecordedAt.Value,
        //    Timestamp = flutter.Timestamp.Value,
        //    Age = flutter.Age,
        //    Odometer = flutter.Odometer,
        //    Uuid = flutter.Uuid,
        //    Extras = flutter.Extras != null ? System.Text.Json.JsonSerializer.Serialize(flutter.Extras) : null
        //};

        //_logger.LogInformation("Creating location for UserId={UserId}, Lat={Lat}, Lon={Lon}",
        //    userId.Value, locationData.Latitude, locationData.Longitude);

        var result = await _locationService.CreateUserLocationAsync((int)userId.Value, request.Location);

        var apiResponse = new ApiResponse<LocationResponseDto>
        {
            Status = result.Success,
            Message = result.Message,
            Data = result.Data
        };

        return StatusCode(result.StatusCode, apiResponse);
    }

    /// <summary>
    /// [DEPRECATED] User location'larini olish (GET - query params)
    /// GET /api/locations/user/{userId}?start_date=...&end_date=...&start_time=09:30&end_time=17:45
    ///
    /// DEPRECATED: Iltimos POST /api/locations/user/{user_id}/query endpoint'ini ishlating
    /// </summary>
    //[HttpGet("user/{user_id}")]
    //[ProducesResponseType(typeof(ApiResponse<IEnumerable<LocationResponseDto>>), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    //public async Task<IActionResult> GetUserLocations(
    //    [FromRoute(Name = "user_id")] int userId,
    //    [FromQuery(Name = "start_date")] DateTime? startDate,
    //    [FromQuery(Name = "end_date")] DateTime? endDate,
    //    [FromQuery(Name = "start_time")] string? startTime,
    //    [FromQuery(Name = "end_time")] string? endTime,
    //    [FromQuery] int? limit)
    //{
    //    // Vaqt formatini validatsiya qilish (HH:MM formatida bo'lishi kerak)
    //    if (!string.IsNullOrWhiteSpace(startTime))
    //    {
    //        if (!IsValidTimeFormat(startTime))
    //        {
    //            return BadRequest(new ApiResponse<object>
    //            {
    //                Status = false,
    //                Message = "start_time noto'g'ri formatda. Format: HH:MM (masalan: 09:30, 14:45)",
    //                Data = null
    //            });
    //        }
    //    }

    //    if (!string.IsNullOrWhiteSpace(endTime))
    //    {
    //        if (!IsValidTimeFormat(endTime))
    //        {
    //            return BadRequest(new ApiResponse<object>
    //            {
    //                Status = false,
    //                Message = "end_time noto'g'ri formatda. Format: HH:MM (masalan: 17:30, 23:59)",
    //                Data = null
    //            });
    //        }
    //    }

    //    var query = new LocationQueryDto
    //    {
    //        UserId = userId,
    //        StartDate = startDate,
    //        EndDate = endDate,
    //        StartTime = startTime,
    //        EndTime = endTime,
    //        Limit = limit
    //    };

    //    var result = await _locationService.GetUserLocationsAsync(query);

    //    var apiResponse = new ApiResponse<IEnumerable<LocationResponseDto>>
    //    {
    //        Status = result.Success,
    //        Message = result.Message,
    //        Data = result.Data
    //    };

    //    return StatusCode(result.StatusCode, apiResponse);
    //}

    /// <summary>
    /// Bitta userning locationlarini olish (POST - body orqali filterlar)
    /// POST /api/locations/user/{user_id}
    ///
    /// FAQAT BIR KUNLIK locationlar
    ///
    /// BODY FORMAT:
    /// {
    ///   "date": "2026-01-07",
    ///   "start_time": "09:30",
    ///   "end_time": "17:45"
    /// }
    /// </summary>
    [HttpPost("user/{user_id}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LocationResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSingleUserLocations(
        [FromRoute(Name = "user_id")] int userId,
        [FromBody] SingleUserLocationQueryDto query)
    {
        // Vaqt formatini validatsiya qilish (agar berilgan bo'lsa)
        if (!string.IsNullOrWhiteSpace(query.StartTime))
        {
            if (!IsValidTimeFormat(query.StartTime))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Status = false,
                    Message = "start_time noto'g'ri formatda. Format: HH:MM (masalan: 09:30, 14:45)",
                    Data = null
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(query.EndTime))
        {
            if (!IsValidTimeFormat(query.EndTime))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Status = false,
                    Message = "end_time noto'g'ri formatda. Format: HH:MM (masalan: 17:30, 23:59)",
                    Data = null
                });
            }
        }

        var result = await _locationService.GetSingleUserLocationsAsync(userId, query);

        var apiResponse = new ApiResponse<IEnumerable<LocationResponseDto>>
        {
            Status = result.Success,
            Message = result.Message,
            Data = result.Data
        };

        return StatusCode(result.StatusCode, apiResponse);
    }

    /// <summary>
    /// Vaqt formatini validatsiya qilish (HH:MM)
    /// </summary>
    private static bool IsValidTimeFormat(string time)
    {
        if (string.IsNullOrWhiteSpace(time))
            return false;

        var parts = time.Split(':');
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], out var hour) || !int.TryParse(parts[1], out var minute))
            return false;

        return hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59;
    }

    /// <summary>
    /// Oxirgi location'larni olish
    /// GET /api/locations/user/{user_id}/last
    /// </summary>
    [HttpGet("user/{user_id}/last")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LocationResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLastLocations(
        [FromRoute(Name = "user_id")] int userId,
        [FromQuery] int count = 100)
    {
        var result = await _locationService.GetLastLocationsAsync(userId, count);

        var apiResponse = new ApiResponse<IEnumerable<LocationResponseDto>>
        {
            Status = result.Success,
            Message = result.Message,
            Data = result.Data
        };

        return StatusCode(result.StatusCode, apiResponse);
    }

    /// <summary>
    /// Kunlik statistikalarni olish
    /// GET /api/locations/user/{user_id}/daily_statistics
    /// </summary>
    [HttpGet("user/{user_id}/daily_statistics")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DailyStatisticsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDailyStatistics(
        [FromRoute(Name = "user_id")] int userId,
        [FromQuery(Name = "start_date")] DateTime startDate,
        [FromQuery(Name = "end_date")] DateTime endDate)
    {
        var query = new DailySummaryQueryDto
        {
            UserId = userId,
            StartDate = startDate,
            EndDate = endDate
        };

        var result = await _locationService.GetDailyStatisticsAsync(query);

        var apiResponse = new ApiResponse<IEnumerable<DailyStatisticsDto>>
        {
            Status = result.Success,
            Message = result.Message,
            Data = result.Data
        };

        return StatusCode(result.StatusCode, apiResponse);
    }

    /// <summary>
    /// Barcha userlarning oxirgi location'larini olish (user ma'lumotlari bilan birga)
    /// GET /api/locations/latest_all
    /// </summary>
    [HttpGet("latest_all")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllUsersLatestLocations()
    {
        try
        {
            // UserService endi avtomatik ravishda latest_location'ni qo'shadi
            var users = await _userService.GetAllActiveUsersAsync();

            var apiResponse = new ApiResponse<IEnumerable<UserResponseDto>>
            {
                Status = true,
                Message = $"Barcha userlarning ma'lumotlari va oxirgi locationlari ({users.Count()} ta user)",
                Data = users
            };

            return Ok(apiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users with latest locations");
            return StatusCode(500, new ApiResponse<object>
            {
                Status = false,
                Message = "Xatolik yuz berdi",
                Data = null
            });
        }
    }

    /// <summary>
    /// ID orqali location olish
    /// GET /api/locations/{id}
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<LocationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLocationById(
        long id,
        [FromQuery(Name = "recorded_at")] DateTime recordedAt)
    {
        var result = await _locationService.GetLocationByIdAsync(id, recordedAt);

        var apiResponse = new ApiResponse<LocationResponseDto>
        {
            Status = result.Success,
            Message = result.Message,
            Data = result.Data
        };

        return StatusCode(result.StatusCode, apiResponse);
    }

    /// <summary>
    /// Ko'p userlarning locationlarini olish (POST - body orqali user_ids YOKI branch_guid va filterlar)
    /// POST /api/locations/multiple_users
    ///
    /// FAQAT BIR KUNLIK locationlar
    /// user_ids, branch_guid yoki ikkalasi ham null bo'lsa BARCHA active userlar
    /// User ma'lumotlari bilan birga locations array qaytaradi
    ///
    /// BODY FORMAT (user_ids bilan):
    /// {
    ///   "user_ids": [123, 456, 789],
    ///   "date": "2026-01-07 03:54:32.302400",
    ///   "start_time": "09:30",
    ///   "end_time": "17:45",
    ///   "limit": 100
    /// }
    ///
    /// BODY FORMAT (branch_guid bilan):
    /// {
    ///   "branch_guid": "abc-123-def",
    ///   "date": "2026-01-07",
    ///   "start_time": "09:30",
    ///   "end_time": "17:45",
    ///   "limit": 100
    /// }
    ///
    /// BODY FORMAT (BARCHA userlar uchun):
    /// {
    ///   "date": "2026-01-07 03:54:32.302400",
    ///   "start_time": "09:30",
    ///   "end_time": "17:45",
    ///   "limit": 100
    /// }
    ///
    /// RESPONSE FORMAT:
    /// {
    ///   "status": true,
    ///   "message": "...",
    ///   "data": [
    ///     {
    ///       "id": 123,
    ///       "name": "User Name",
    ///       "phone": "+998901234567",
    ///       "branch_guid": "...",
    ///       "image": "...",
    ///       "is_active": true,
    ///       "locations": [
    ///         { "id": 1, "latitude": 41.0, "longitude": 69.0, ... },
    ///         { "id": 2, "latitude": 41.1, "longitude": 69.1, ... }
    ///       ]
    ///     }
    ///   ]
    /// }
    /// </summary>
    [HttpPost("multiple_users")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserWithLocationsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMultipleUsersLocations([FromBody] MultipleUsersLocationQueryDto query)
    {
        // Vaqt formatini validatsiya qilish (agar berilgan bo'lsa)
        if (!string.IsNullOrWhiteSpace(query.StartTime))
        {
            if (!IsValidTimeFormat(query.StartTime))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Status = false,
                    Message = "start_time noto'g'ri formatda. Format: HH:MM (masalan: 09:30, 14:45)",
                    Data = null
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(query.EndTime))
        {
            if (!IsValidTimeFormat(query.EndTime))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Status = false,
                    Message = "end_time noto'g'ri formatda. Format: HH:MM (masalan: 17:30, 23:59)",
                    Data = null
                });
            }
        }

        var result = await _locationService.GetMultipleUsersLocationsAsync(query, _userService);

        var apiResponse = new ApiResponse<IEnumerable<UserWithLocationsDto>>
        {
            Status = result.Success,
            Message = result.Message,
            Data = result.Data
        };

        return StatusCode(result.StatusCode, apiResponse);
    }
    public class filter
    {
        [JsonPropertyName("date_time")]
        public DateTime? DateTime { get; set; }
    }
}
