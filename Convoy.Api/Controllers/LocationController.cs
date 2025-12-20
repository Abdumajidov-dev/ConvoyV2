using Convoy.Api.Models;
using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Convoy.Api.Controllers;

/// <summary>
/// Location tracking API controller
/// </summary>
[ApiController]
[Route("api/locations")]
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationController> _logger;

    public LocationController(
        ILocationService locationService,
        ILogger<LocationController> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    /// <summary>
    /// Location yaratish (user_id + locations array)
    /// POST /api/locations
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LocationResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
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
