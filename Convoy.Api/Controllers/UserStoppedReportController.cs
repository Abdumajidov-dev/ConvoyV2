using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Convoy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // PHP token bilan authentication
public class UserStoppedReportController : ControllerBase
{
    private readonly IUserStoppedReportService _service;
    private readonly ILogger<UserStoppedReportController> _logger;

    public UserStoppedReportController(
        IUserStoppedReportService service,
        ILogger<UserStoppedReportController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Yangi user stopped report yaratish (manual)
    /// User o'zi to'xtab qolganini bildirishi mumkin
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserStoppedReportDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);

            return Ok(new
            {
                status = true,
                message = "Stopped report muvaffaqiyatli yaratildi",
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stopped report");
            return StatusCode(500, new
            {
                status = false,
                message = "Stopped report yaratishda xatolik",
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// User uchun aktiv stopped report borligini tekshirish
    /// </summary>
    [HttpGet("active/{userId}")]
    public async Task<IActionResult> GetActiveReport(int userId)
    {
        try
        {
            var result = await _service.GetActiveStoppedReportAsync(userId);

            if (result == null)
            {
                return Ok(new
                {
                    status = true,
                    message = "Aktiv stopped report yo'q",
                    data = (object?)null
                });
            }

            return Ok(new
            {
                status = true,
                message = "Aktiv stopped report topildi",
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active stopped report for user {UserId}", userId);
            return StatusCode(500, new
            {
                status = false,
                message = "Stopped report olishda xatolik",
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// User'ning stopped report'ini resolve qilish (harakat davom etganda)
    /// </summary>
    [HttpPut("resolve/{userId}")]
    public async Task<IActionResult> Resolve(int userId)
    {
        try
        {
            var success = await _service.ResolveStoppedReportAsync(userId);

            if (!success)
            {
                return NotFound(new
                {
                    status = false,
                    message = "Aktiv stopped report topilmadi",
                    data = (object?)null
                });
            }

            return Ok(new
            {
                status = true,
                message = "Stopped report resolve qilindi",
                data = new { user_id = userId, resolved = true }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving stopped report for user {UserId}", userId);
            return StatusCode(500, new
            {
                status = false,
                message = "Stopped report resolve qilishda xatolik",
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// Sanaga qarab stopped reportlarni olish
    /// </summary>
    [HttpGet("by-date")]
    public async Task<IActionResult> GetByDate([FromQuery] string date)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(date))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "date parametri bo'sh bo'lmasligi kerak",
                    data = (object?)null
                });
            }

            if (!DateTime.TryParse(date, out var parsedDate))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "date formati noto'g'ri",
                    data = (object?)null
                });
            }

            var result = await _service.GetStoppedReportsByDateAsync(parsedDate);

            return Ok(new
            {
                status = true,
                message = $"{result.Count} ta stopped report topildi",
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stopped reports by date");
            return StatusCode(500, new
            {
                status = false,
                message = "Stopped reportlar olishda xatolik",
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// User ID'ga qarab barcha stopped reportlarni olish
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserReports(int userId)
    {
        try
        {
            var result = await _service.GetUserStoppedReportsAsync(userId);

            return Ok(new
            {
                status = true,
                message = $"{result.Count} ta stopped report topildi",
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stopped reports for user {UserId}", userId);
            return StatusCode(500, new
            {
                status = false,
                message = "User stopped reportlari olishda xatolik",
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// Aktiv stopped report'lari bo'lgan userlarni olish
    /// </summary>
    [HttpGet("active-users")]
    public async Task<IActionResult> GetActiveUsers()
    {
        try
        {
            var userIds = await _service.GetUserIdsWithActiveStoppedReportsAsync();

            return Ok(new
            {
                status = true,
                message = $"{userIds.Count} ta user aktiv stopped report'ga ega",
                data = new { user_ids = userIds, count = userIds.Count }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users with active stopped reports");
            return StatusCode(500, new
            {
                status = false,
                message = "Userlarni olishda xatolik",
                data = (object?)null
            });
        }
    }
}
