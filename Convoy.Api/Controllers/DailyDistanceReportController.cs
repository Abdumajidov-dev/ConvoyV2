using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Convoy.Api.Controllers;

/// <summary>
/// Kunlik masofa hisoboti API controller (SODDALASHTIRILGAN)
/// </summary>
[ApiController]
[Route("api/daily_distance_reports")]
// [Authorize] // Temporarily disabled for testing
public class DailyDistanceReportController : ControllerBase
{
    private readonly IDailyDistanceReportService _reportService;
    private readonly ILogger<DailyDistanceReportController> _logger;

    public DailyDistanceReportController(
        IDailyDistanceReportService reportService,
        ILogger<DailyDistanceReportController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// [REAL-TIME TEST] Bugungi kun hozirgi vaqtgacha bo'lgan masofani hisoblash
    /// Database'ga saqlanmaydi, faqat hisoblangan natijani qaytaradi
    /// GET /api/daily_distance_reports/calculate_current?user_id=5277
    /// </summary>
    [HttpGet("calculate_current")]
    public async Task<IActionResult> CalculateCurrentDayDistance([FromQuery] int user_id)
    {
        try
        {
            if (user_id <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "user_id majburiy va 0 dan katta bo'lishi kerak",
                    data = (object?)null
                });
            }

            var result = await _reportService.CalculateCurrentDayDistanceAsync(user_id);

            return StatusCode(result.StatusCode, new
            {
                status = result.Success,
                message = result.Message,
                data = result.Data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CalculateCurrentDayDistance for user {UserId}", user_id);
            return StatusCode(500, new
            {
                status = false,
                message = "Xatolik yuz berdi",
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// [TEST] Bugungi kunlik hisobotni yaratish
    /// POST /api/daily_distance_reports/generate_today?user_id=5277
    /// </summary>
    [HttpPost("generate_today")]
    public async Task<IActionResult> GenerateTodayReport([FromQuery] int? user_id)
    {
        try
        {
            if (user_id == null)
            {
                // Barcha userlar uchun
                var result = await _reportService.GenerateDailyReportsForAllUsersAsync(DateTime.Today);
                return StatusCode(result.StatusCode, new
                {
                    status = result.Success,
                    message = result.Message,
                    data = result.Data
                });
            }
            else
            {
                // Bitta user uchun
                var result = await _reportService.GenerateDailyReportAsync(user_id.Value, DateTime.Today);
                return StatusCode(result.StatusCode, new
                {
                    status = result.Success,
                    message = result.Message,
                    data = result.Data
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GenerateTodayReport");
            return StatusCode(500, new
            {
                status = false,
                message = "Xatolik yuz berdi",
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// [TEST] Kechagi kunlik hisobotni yaratish
    /// POST /api/daily_distance_reports/generate_yesterday?user_id=5277
    /// </summary>
    [HttpPost("generate_yesterday")]
    public async Task<IActionResult> GenerateYesterdayReport([FromQuery] int? user_id)
    {
        try
        {
            var yesterday = DateTime.Today.AddDays(-1);

            if (user_id == null)
            {
                // Barcha userlar uchun
                var result = await _reportService.GenerateDailyReportsForAllUsersAsync(yesterday);
                return StatusCode(result.StatusCode, new
                {
                    status = result.Success,
                    message = result.Message,
                    data = result.Data
                });
            }
            else
            {
                // Bitta user uchun
                var result = await _reportService.GenerateDailyReportAsync(user_id.Value, yesterday);
                return StatusCode(result.StatusCode, new
                {
                    status = result.Success,
                    message = result.Message,
                    data = result.Data
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GenerateYesterdayReport");
            return StatusCode(500, new
            {
                status = false,
                message = "Xatolik yuz berdi",
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// [PRODUCTION] Hisobotlarni filter qilish
    /// POST /api/daily_distance_reports/filter
    /// Body: {
    ///   "branch_guid": "guid-string",  // optional
    ///   "user_ids": [5277, 5475],      // optional
    ///   "start_date": "2026-02-01",
    ///   "end_date": "2026-02-28"
    /// }
    /// </summary>
    [HttpPost("filter")]
    public async Task<IActionResult> GetFilteredReports([FromBody] DailyDistanceReportFilterDto filter)
    {
        try
        {
            // Validation
            if (filter.StartDate > filter.EndDate)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "start_date end_date'dan katta bo'lishi mumkin emas",
                    data = (object?)null
                });
            }

            var result = await _reportService.GetFilteredReportsAsync(filter);

            return StatusCode(result.StatusCode, new
            {
                status = result.Success,
                message = result.Message,
                data = result.Data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetFilteredReports");
            return StatusCode(500, new
            {
                status = false,
                message = "Xatolik yuz berdi",
                data = (object?)null
            });
        }
    }
}
