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
    private readonly ILogger<LocationController> _logger;
    private readonly Convoy.Service.Services.LocationClusteringService _clusteringService;

    public LocationController(
        ILocationService locationService,
        IUserService userService,
        ILogger<LocationController> logger,
        Convoy.Service.Services.LocationClusteringService clusteringService)
    {
        _locationService = locationService;
        _userService = userService;
        _logger = logger;
        _clusteringService = clusteringService;
    }

    /// <summary>
    /// Token'dan worker_id (user_id) ni olish helper method
    /// PhpTokenAuthenticationHandler tomonidan ClaimTypes.NameIdentifier'ga qo'shilgan
    /// </summary>
    private int? GetWorkerIdFromToken()
    {
        var workerIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(workerIdClaim) || !int.TryParse(workerIdClaim, out var workerId))
        {
            return null;
        }

        return workerId;
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
        // PHP tokendan worker_id ni olish
        var userId = GetWorkerIdFromToken();

        if (userId == null)
        {
            _logger.LogWarning("Invalid or missing worker_id claim in token");
            return Unauthorized(new ApiResponse<object>
            {
                Status = false,
                Message = "Token'da worker_id topilmadi yoki noto'g'ri",
                Data = null
            });
        }

        _logger.LogInformation("Creating location for worker_id={WorkerId}", userId);

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

        // Validate and sanitize battery level (0-100 oralig'ida bo'lishi kerak)
        int? batteryLevel = null;


        var result = await _locationService.CreateUserLocationAsync(userId.Value, request.Location);

        var apiResponse = new ApiResponse<LocationResponseDto>
        {
            Status = result.Success,
            Message = result.Message,
            Data = result.Data
        };

        return StatusCode(result.StatusCode, apiResponse);
    }

    /// <summary>
    /// 
    /// Ko'p location yaratish (wrapped format, user_id JWT tokendan
    /// <!--- Bu endpoint, Flutter'dan bir martada ko'p location yuborish uchun mo'ljallangan. -->
    /// </Summary>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(ApiResponse<IList<LocationResponseDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateLocations(
    [FromBody] LocationsRequestWrapperDto request)
    {
        var userId = GetWorkerIdFromToken();

        if (userId == null)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Status = false,
                Message = "Token'da worker_id topilmadi yoki noto'g'ri",
                Data = null
            });
        }

        if (request?.Locations == null || !request.Locations.Any())
        {
            return BadRequest(new ApiResponse<object>
            {
                Status = false,
                Message = "Locations ro'yxati bo'sh",
                Data = null
            });
        }

        if (request.Locations.Count > 1000)
        {
            return BadRequest(new ApiResponse<object>
            {
                Status = false,
                Message = "Bir martada maksimum 1000 ta location yuborish mumkin",
                Data = null
            });
        }

        var result = await _locationService
            .CreateUserLocationsAsync(userId.Value, request.Locations);

        var apiResponse = new ApiResponse<IList<LocationResponseDto>>
        {
            Status = result.Success,
            Message = result.Message,
            Data = result.Data
        };

        return StatusCode(result.StatusCode, apiResponse);
    }
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

    /// <summary>
    /// Clustered locationlarni olish - 10 metr oralig'dagi locationlarni groupalab beradi
    /// POST /api/locations/user/{userId}/clustered
    ///
    /// Admin hodimlar uchun - locationlarni 10 metr oralig'ida gruppalaydi
    /// Har bir group uchun stopped_time (o'sha joyda qancha vaqt turganini) hisoblab beradi
    ///
    /// BODY FORMAT:
    /// {
    ///   "date": "2026-01-30",
    ///   "start_time": "09:00",
    ///   "end_time": "18:00"
    /// }
    ///
    /// RESPONSE FORMAT:
    /// {
    ///   "status": true,
    ///   "message": "...",
    ///   "data": [
    ///     {
    ///       "cluster_id": 1,
    ///       "user_id": 123,
    ///       "latitude": 41.2995,
    ///       "longitude": 69.2401,
    ///       "start_time": "2026-01-30T09:15:00",
    ///       "end_time": "2026-01-30T09:45:00",
    ///       "stopped_time": 30,  // daqiqa
    ///       "location_count": 15,
    ///       "locations": [...]  // optional
    ///     }
    ///   ]
    /// }
    /// </summary>
    [HttpPost("user/{userId}/clustered")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ClusteredLocationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetClusteredLocations(int userId, [FromBody] SingleUserLocationQueryDto query)
    {
        // Validation
        if (!string.IsNullOrWhiteSpace(query.StartTime) && !IsValidTimeFormat(query.StartTime))
        {
            return BadRequest(new ApiResponse<object>
            {
                Status = false,
                Message = "start_time noto'g'ri formatda. Format: HH:MM (masalan: 09:30)",
                Data = null
            });
        }

        if (!string.IsNullOrWhiteSpace(query.EndTime) && !IsValidTimeFormat(query.EndTime))
        {
            return BadRequest(new ApiResponse<object>
            {
                Status = false,
                Message = "end_time noto'g'ri formatda. Format: HH:MM (masalan: 17:30)",
                Data = null
            });
        }

        // Get locations
        var result = await _locationService.GetSingleUserLocationsAsync(userId, query);

        if (!result.Success || result.Data == null || !result.Data.Any())
        {
            return StatusCode(result.StatusCode, new ApiResponse<object>
            {
                Status = false,
                Message = result.Message ?? "Locationlar topilmadi",
                Data = null
            });
        }

        // Apply clustering
        var clusteredLocations = _clusteringService.ClusterLocations(result.Data.ToList(), userId);

        var apiResponse = new ApiResponse<IEnumerable<ClusteredLocationDto>>
        {
            Status = true,
            Message = $"{clusteredLocations.Count} ta cluster topildi ({result.Data.Count()} location'dan)",
            Data = clusteredLocations
        };

        return Ok(apiResponse);
    }

    /// <summary>
    /// Locationlarni stopped_time bilan olish (clustering'siz)
    /// POST /api/locations/user/{userId}/with_stopped_time
    ///
    /// Har bir location uchun stopped_time hisoblab beradi
    /// Agar ketma-ket locationlar 10 metr ichida bo'lsa, stopped_time oshadi
    ///
    /// BODY FORMAT:
    /// {
    ///   "date": "2026-01-30",
    ///   "start_time": "09:00",
    ///   "end_time": "18:00"
    /// }
    ///
    /// RESPONSE FORMAT:
    /// {
    ///   "status": true,
    ///   "message": "...",
    ///   "data": [
    ///     {
    ///       "id": 1,
    ///       "user_id": 123,
    ///       "latitude": 41.2995,
    ///       "longitude": 69.2401,
    ///       "recorded_at": "2026-01-30T09:15:00",
    ///       "stopped_time": 15,  // daqiqa - o'sha joyda turgan vaqt
    ///       ...
    ///     }
    ///   ]
    /// }
    /// </summary>
    [HttpPost("user/{userId}/with_stopped_time")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LocationResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLocationsWithStoppedTime(int userId, [FromBody] SingleUserLocationQueryDto query)
    {
        // Validation
        if (!string.IsNullOrWhiteSpace(query.StartTime) && !IsValidTimeFormat(query.StartTime))
        {
            return BadRequest(new ApiResponse<object>
            {
                Status = false,
                Message = "start_time noto'g'ri formatda. Format: HH:MM (masalan: 09:30)",
                Data = null
            });
        }

        if (!string.IsNullOrWhiteSpace(query.EndTime) && !IsValidTimeFormat(query.EndTime))
        {
            return BadRequest(new ApiResponse<object>
            {
                Status = false,
                Message = "end_time noto'g'ri formatda. Format: HH:MM (masalan: 17:30)",
                Data = null
            });
        }

        // Get locations
        var result = await _locationService.GetSingleUserLocationsAsync(userId, query);

        if (!result.Success || result.Data == null)
        {
            return StatusCode(result.StatusCode, new ApiResponse<object>
            {
                Status = false,
                Message = result.Message ?? "Locationlar topilmadi",
                Data = null
            });
        }

        // Calculate stopped time
        var locationsWithStoppedTime = _clusteringService.CalculateStoppedTime(result.Data.ToList());

        var apiResponse = new ApiResponse<IEnumerable<LocationResponseDto>>
        {
            Status = true,
            Message = $"{locationsWithStoppedTime.Count} ta location topildi (stopped_time hisoblab)",
            Data = locationsWithStoppedTime
        };

        return Ok(apiResponse);
    }

    public class filter
    {
        [JsonPropertyName("date_time")]
        public DateTime? DateTime { get; set; }
    }
}
