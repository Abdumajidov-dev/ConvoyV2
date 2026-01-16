//using Convoy.Service.Interfaces;
//using Microsoft.AspNetCore.Mvc;

//namespace Convoy.Api.Controllers;

///// <summary>
///// Telegram service test controller
///// Telegram service'ni test qilish uchun
///// </summary>
//[ApiController]
//[Route("api/telegram-test")]
//public class TelegramTestController : ControllerBase
//{
//    private readonly ITelegramService _telegramService;
//    private readonly ILogger<TelegramTestController> _logger;

//    public TelegramTestController(
//        ITelegramService telegramService,
//        ILogger<TelegramTestController> logger)
//    {
//        _telegramService = telegramService;
//        _logger = logger;
//    }

//    /// <summary>
//    /// Oddiy text xabar yuborish
//    /// GET /api/telegram-test/send-simple?message=Hello
//    /// </summary>
//    [HttpGet("send-simple")]
//    public async Task<IActionResult> SendSimpleMessage([FromQuery] string message = "Test message from Convoy API")
//    {
//        var result = await _telegramService.SendMessageAsync(message);

//        return Ok(new
//        {
//            status = result,
//            message = result ? "Telegram xabar yuborildi" : "Telegram xabar yuborishda xatolik",
//            data = new { sent_message = message }
//        });
//    }

//    /// <summary>
//    /// Location ma'lumotini yuborish
//    /// POST /api/telegram-test/send-location
//    /// </summary>
//    [HttpPost("send-location")]
//    public async Task<IActionResult> SendLocationMessage([FromBody] TestLocationDto dto)
//    {
//        var result = await _telegramService.SendLocationDataAsync(
//            dto.UserId,
//            dto.UserName,
//            dto.Latitude,
//            dto.Longitude,
//            dto.RecordedAt
//        );

//        return Ok(new
//        {
//            status = result,
//            message = result ? "Location ma'lumoti Telegram'ga yuborildi" : "Xatolik yuz berdi",
//            data = dto
//        });
//    }

//    /// <summary>
//    /// Bulk location ma'lumotini yuborish
//    /// POST /api/telegram-test/send-bulk
//    /// </summary>
//    [HttpPost("send-bulk")]
//    public async Task<IActionResult> SendBulkMessage([FromBody] TestBulkLocationDto dto)
//    {
//        var result = await _telegramService.SendBulkLocationDataAsync(
//            dto.UserId,
//            dto.UserName,
//            dto.LocationCount,
//            dto.FirstLocation,
//            dto.LastLocation
//        );

//        return Ok(new
//        {
//            status = result,
//            message = result ? "Bulk ma'lumot Telegram'ga yuborildi" : "Xatolik yuz berdi",
//            data = dto
//        });
//    }

//    /// <summary>
//    /// Custom data yuborish
//    /// POST /api/telegram-test/send-data
//    /// </summary>
//    [HttpPost("send-data")]
//    public async Task<IActionResult> SendCustomData([FromBody] Dictionary<string, string> data)
//    {
//        var result = await _telegramService.SendDataAsync(data, "📊 Test Data Report");

//        return Ok(new
//        {
//            status = result,
//            message = result ? "Custom data Telegram'ga yuborildi" : "Xatolik yuz berdi",
//            data
//        });
//    }

//    /// <summary>
//    /// Alert yuborish
//    /// POST /api/telegram-test/send-alert
//    /// </summary>
//    [HttpPost("send-alert")]
//    public async Task<IActionResult> SendAlert([FromBody] TestAlertDto dto)
//    {
//        var result = await _telegramService.SendAlertAsync(dto.Message, dto.Level);

//        return Ok(new
//        {
//            status = result,
//            message = result ? "Alert Telegram'ga yuborildi" : "Xatolik yuz berdi",
//            data = dto
//        });
//    }
//}

//// Test DTO'lar
//public class TestLocationDto
//{
//    public int UserId { get; set; }
//    public string UserName { get; set; } = string.Empty;
//    public double Latitude { get; set; }
//    public double Longitude { get; set; }
//    public DateTime RecordedAt { get; set; }
//}

//public class TestBulkLocationDto
//{
//    public int UserId { get; set; }
//    public string UserName { get; set; } = string.Empty;
//    public int LocationCount { get; set; }
//    public DateTime FirstLocation { get; set; }
//    public DateTime LastLocation { get; set; }
//}

//public class TestAlertDto
//{
//    public string Message { get; set; } = string.Empty;
//    public string Level { get; set; } = "INFO"; // ERROR, WARNING, SUCCESS, INFO
//}
