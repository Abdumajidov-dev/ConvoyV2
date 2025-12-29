using Convoy.Api.Models;
using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        if (flutter.Coords == null)
        {
            return BadRequest(new ApiResponse<object>
            {
                Status = false,
                Message = "Coords object bo'sh yoki mavjud emas",
                Data = null
            });
        }

        // Validate and sanitize battery level (0-100 oralig'ida bo'lishi kerak)
        int? batteryLevel = null;
        if (flutter.Battery?.Level != null)
        {
            var level = flutter.Battery.Level.Value;
            if (level >= 0 && level <= 100)
            {
                batteryLevel = level;
            }
            else
            {
                _logger.LogWarning("Invalid battery level {Level} for UserId={UserId}, setting to null", level, userId.Value);
            }
        }

        // Map Flutter format to LocationDataDto
        var locationData = new LocationDataDto
        {
            // Core location from coords
            Latitude = flutter.Coords.Latitude,
            Longitude = flutter.Coords.Longitude,
            Accuracy = flutter.Coords.Accuracy,
            Speed = flutter.Coords.Speed,
            Heading = flutter.Coords.Heading,
            Altitude = flutter.Coords.Altitude,

            // Extended coords
            EllipsoidalAltitude = flutter.Coords.EllipsoidalAltitude,
            HeadingAccuracy = flutter.Coords.HeadingAccuracy,
            SpeedAccuracy = flutter.Coords.SpeedAccuracy,
            AltitudeAccuracy = flutter.Coords.AltitudeAccuracy,

            // Activity
            ActivityType = flutter.Activity?.Type,
            ActivityConfidence = flutter.Activity?.Confidence,
            IsMoving = flutter.IsMoving,

            // Battery (validated)
            BatteryLevel = batteryLevel,
            IsCharging = flutter.Battery?.IsCharging,

            // Metadata
            RecordedAt = flutter.RecordedAt,
            Timestamp = flutter.Timestamp,
            Age = flutter.Age,
            Odometer = flutter.Odometer,
            Uuid = flutter.Uuid,
            Extras = flutter.Extras != null ? System.Text.Json.JsonSerializer.Serialize(flutter.Extras) : null
        };

        _logger.LogInformation("Creating location for UserId={UserId}, Lat={Lat}, Lon={Lon}",
            userId.Value, locationData.Latitude, locationData.Longitude);

        var result = await _locationService.CreateUserLocationAsync((int)userId.Value, locationData);

        var apiResponse = new ApiResponse<LocationResponseDto>
        {
            Status = result.Success,
            Message = result.Message,
            Data = result.Data
        };

        return StatusCode(result.StatusCode, apiResponse);
    }

    /// <summary>
    /// User location'larini olish
    /// GET /api/locations/user/{userId}
    /// </summary>
    [HttpGet("user/{user_id}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LocationResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserLocations(
        [FromRoute(Name = "user_id")] int userId,
        [FromQuery(Name = "start_date")] DateTime? startDate,
        [FromQuery(Name = "end_date")] DateTime? endDate,
        [FromQuery] int? limit)
    {
        var query = new LocationQueryDto
        {
            UserId = userId,
            StartDate = startDate,
            EndDate = endDate,
            Limit = limit
        };

        var result = await _locationService.GetUserLocationsAsync(query);

        var apiResponse = new ApiResponse<IEnumerable<LocationResponseDto>>
        {
            Status = result.Success,
            Message = result.Message,
            Data = result.Data
        };

        return StatusCode(result.StatusCode, apiResponse);
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
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserWithLatestLocationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllUsersLatestLocations()
    {
        try
        {
            // 1. Barcha active userlarni olish
            var users = await _userService.GetAllActiveUsersAsync();

            // 2. Barcha oxirgi locationlarni olish
            var locationsResult = await _locationService.GetAllUsersLatestLocationsAsync();

            if (!locationsResult.Success)
            {
                return StatusCode(locationsResult.StatusCode, new ApiResponse<object>
                {
                    Status = false,
                    Message = locationsResult.Message,
                    Data = null
                });
            }

            // 3. User va location ma'lumotlarini birlashtirish
            var locations = locationsResult.Data ?? new List<LocationResponseDto>();
            var locationsByUserId = locations.ToDictionary(l => l.UserId, l => l);

            var result = users.Select(user => new UserWithLatestLocationDto
            {
                UserId = (int)user.Id,
                Name = user.Name,
                Phone = user.Phone,
                Image = null, // User entity'da image yo'q, kerak bo'lsa qo'shish mumkin
                IsActive = user.IsActive,
                LatestLocation = locationsByUserId.TryGetValue((int)user.Id, out var location) ? location : null
            }).ToList();

            var apiResponse = new ApiResponse<IEnumerable<UserWithLatestLocationDto>>
            {
                Status = true,
                Message = $"Barcha userlarning ma'lumotlari va oxirgi locationlari ({result.Count} ta user)",
                Data = result
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
}
