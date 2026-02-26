using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Convoy.Api.Controllers;

/// <summary>
/// Kunlik masofa hisoboti API controller
/// </summary>
[ApiController]
[Route("api/daily_distance_reports")]
[Authorize] // Barcha endpoint'lar authentication talab qiladi
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
    /// Foydalanuvchining ma'lum sana uchun hisobotini olish
    /// GET /api/daily_distance_reports/{userId}?date=2026-02-15
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetByUserAndDate(
        [FromRoute] long userId,
        [FromQuery] DateTime date)
    {
        var result = await _reportService.GetByUserAndDateAsync(userId, date);

        return StatusCode(result.StatusCode, new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        });
    }

    /// <summary>
    /// Foydalanuvchining ma'lum sana oralig'idagi hisobotlarini olish
    /// GET /api/daily_distance_reports/{userId}/range?start_date=2026-02-01&end_date=2026-02-28
    /// </summary>
    [HttpGet("{userId}/range")]
    public async Task<IActionResult> GetByUserAndDateRange(
        [FromRoute] long userId,
        [FromQuery] DateTime start_date,
        [FromQuery] DateTime end_date)
    {
        var result = await _reportService.GetByUserAndDateRangeAsync(userId, start_date, end_date);

        return StatusCode(result.StatusCode, new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        });
    }

    /// <summary>
    /// Barcha foydalanuvchilar uchun ma'lum sana oralig'idagi hisobotlarni olish
    /// GET /api/daily_distance_reports/all?start_date=2026-02-01&end_date=2026-02-28
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllByDateRange(
        [FromQuery] DateTime start_date,
        [FromQuery] DateTime end_date)
    {
        var result = await _reportService.GetByDateRangeAsync(start_date, end_date);

        return StatusCode(result.StatusCode, new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        });
    }

    /// <summary>
    /// Ma'lum sana uchun eng ko'p masofa bosgan foydalanuvchilarni olish
    /// GET /api/daily_distance_reports/top?date=2026-02-15&count=10
    /// </summary>
    [HttpGet("top")]
    public async Task<IActionResult> GetTopDistancesByDate(
        [FromQuery] DateTime date,
        [FromQuery] int count = 10)
    {
        var result = await _reportService.GetTopDistancesByDateAsync(date, count);

        return StatusCode(result.StatusCode, new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        });
    }

    /// <summary>
    /// Bitta foydalanuvchi uchun kunlik hisobotni yaratish yoki yangilash
    /// POST /api/daily_distance_reports/generate
    /// Body: { "user_id": 5277, "report_date": "2026-02-15" }
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateDailyReport([FromBody] GenerateDailyReportRequestDto request)
    {
        if (request.UserId == null)
        {
            return BadRequest(new
            {
                status = false,
                message = "user_id majburiy maydon",
                data = (object?)null
            });
        }

        var result = await _reportService.GenerateDailyReportAsync(request.UserId.Value, request.ReportDate);

        return StatusCode(result.StatusCode, new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        });
    }

    /// <summary>
    /// Barcha foydalanuvchilar uchun ma'lum sana uchun hisobotlarni yaratish
    /// POST /api/daily_distance_reports/generate_all
    /// Body: { "report_date": "2026-02-15" }
    /// </summary>
    [HttpPost("generate_all")]
    public async Task<IActionResult> GenerateAllDailyReports([FromBody] GenerateDailyReportRequestDto request)
    {
        var result = await _reportService.GenerateDailyReportsForAllUsersAsync(request.ReportDate);

        return StatusCode(result.StatusCode, new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        });
    }

    /// <summary>
    /// Kunlik statistikani olish
    /// GET /api/daily_distance_reports/statistics?date=2026-02-15
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetDailyStatistics([FromQuery] DateTime date)
    {
        var result = await _reportService.GetDailyStatisticsAsync(date);

        return StatusCode(result.StatusCode, new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        });
    }

    /// <summary>
    /// Foydalanuvchi umumiy statistikasini olish
    /// GET /api/daily_distance_reports/{userId}/summary?start_date=2026-02-01&end_date=2026-02-28
    /// </summary>
    [HttpGet("{userId}/summary")]
    public async Task<IActionResult> GetUserSummary(
        [FromRoute] long userId,
        [FromQuery] DateTime start_date,
        [FromQuery] DateTime end_date)
    {
        var result = await _reportService.GetUserSummaryAsync(userId, start_date, end_date);

        return StatusCode(result.StatusCode, new
        {
            status = result.Success,
            message = result.Message,
            data = result.Data
        });
    }

    /// <summary>
    /// Bugungi kunlik hisobotni yaratish (quick action)
    /// POST /api/daily_distance_reports/generate_today?user_id=5277
    /// </summary>
    [HttpPost("generate_today")]
    public async Task<IActionResult> GenerateTodayReport([FromQuery] long? user_id)
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

    /// <summary>
    /// Kechagi kunlik hisobotni yaratish (quick action)
    /// POST /api/daily_distance_reports/generate_yesterday?user_id=5277
    /// </summary>
    [HttpPost("generate_yesterday")]
    public async Task<IActionResult> GenerateYesterdayReport([FromQuery] long? user_id)
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
}
