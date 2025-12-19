using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Convoy.Api.Controllers;

/// <summary>
/// Location tracking API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
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
    /// Yangi location yaratish
    /// POST /api/location
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(LocationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationDto dto)
    {
        try
        {
            var result = await _locationService.CreateLocationAsync(dto);
            return CreatedAtAction(
                nameof(GetLocationById),
                new { id = result.Id, recordedAt = result.RecordedAt },
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating location");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Batch location yaratish
    /// POST /api/location/batch
    /// </summary>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLocationBatch([FromBody] CreateLocationBatchDto dto)
    {
        try
        {
            var count = await _locationService.CreateLocationBatchAsync(dto);
            return CreatedAtAction(nameof(CreateLocationBatch), new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating batch locations");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// User location'larini olish
    /// GET /api/location/user/{userId}
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(IEnumerable<LocationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUserLocations(
        int userId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? limit)
    {
        try
        {
            var query = new LocationQueryDto
            {
                UserId = userId,
                StartDate = startDate,
                EndDate = endDate,
                Limit = limit
            };

            var locations = await _locationService.GetUserLocationsAsync(query);
            return Ok(locations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user locations");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Oxirgi location'larni olish
    /// GET /api/location/user/{userId}/last
    /// </summary>
    [HttpGet("user/{userId}/last")]
    [ProducesResponseType(typeof(IEnumerable<LocationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLastLocations(
        int userId,
        [FromQuery] int count = 100)
    {
        try
        {
            var locations = await _locationService.GetLastLocationsAsync(userId, count);
            return Ok(locations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last locations");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Kunlik statistikalarni olish
    /// GET /api/location/user/{userId}/daily-statistics
    /// </summary>
    [HttpGet("user/{userId}/daily-statistics")]
    [ProducesResponseType(typeof(IEnumerable<DailyStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDailyStatistics(
        int userId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var query = new DailySummaryQueryDto
            {
                UserId = userId,
                StartDate = startDate,
                EndDate = endDate
            };

            var statistics = await _locationService.GetDailyStatisticsAsync(query);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily statistics");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// ID orqali location olish
    /// GET /api/location/{id}
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LocationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLocationById(
        long id,
        [FromQuery] DateTime recordedAt)
    {
        try
        {
            var location = await _locationService.GetLocationByIdAsync(id, recordedAt);
            if (location == null)
            {
                return NotFound(new { error = "Location not found" });
            }
            return Ok(location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location by ID");
            return BadRequest(new { error = ex.Message });
        }
    }
}
