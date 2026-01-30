using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Convoy.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IDeviceTokenService _deviceTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IDeviceTokenService deviceTokenService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _deviceTokenService = deviceTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Telefon raqamni tekshirish ()
    /// </summary>
    [HttpPost("verify_number")]
    public async Task<IActionResult> VerifyNumber([FromBody] VerifyNumberRequest request)
    {
        try
        {
            var result = await _authService.VerifyNumberAsync(request.PhoneNumber);

            var response = new
            {
                status = result.Status,
                message = result.Message,
                data = result.Data
            };

            if (!result.Status)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying phone number: {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new
            {
                status = false,
                message = "Internal server error",
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// OTP kod yuborish
    /// </summary>
    [HttpPost("send_otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        try
        {
            var result = await _authService.SendOtpAsync(request.PhoneNumber);

            var response = new
            {
                status = result.Status,
                message = result.Message,
                data = result.Data
            };

            if (!result.Status)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP to: {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new
            {
                status = false,
                message = "Internal server error",
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// OTP kodni tekshirish va JWT token olish
    /// </summary>
    [HttpPost("verify_otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        try
        {
            var result = await _authService.VerifyOtpAsync(request.PhoneNumber, request.OtpCode);

            var response = new
            {
                status = result.Status,
                message = result.Message,
                data = result.Data
            };

            if (!result.Status)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP for: {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new
            {
                status = false,
                message = "Internal server error",
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// Joriy foydalanuvchi ma'lumotlarini olish (JWT token orqali)
    /// Token'ni PHP API'ga forward qilib validate qiladi va user ma'lumotlarini qaytaradi
    /// Returns: user info + roles + grouped permissions (Flutter-friendly format)
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        try
        {
            // Authorization header'dan JWT token olish
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new
                {
                    status = false,
                    message = "Authorization header topilmadi",
                    data = (object?)null
                });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            // Token'ni PHP API'ga forward qilib user ma'lumotlarini olish
            var result = await _authService.GetMeAsync(token);

            var response = new
            {
                status = result.Status,
                message = result.Message,
                data = result.Data
            };

            if (!result.Status)
            {
                // PHP API 401 qaytarsa biz ham 401 qaytaramiz
                return Unauthorized(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new
            {
                status = false,
                message = "Internal server error",
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// Device token'ni saqlash yoki yangilash (login qilingandan keyin chaqiriladi)
    /// </summary>
    [HttpPost("save_device_token")]
    [Authorize]
    public async Task<IActionResult> SaveDeviceToken([FromBody] SaveDeviceTokenRequest request)
    {
        try
        {
            var result = await _deviceTokenService.SaveOrUpdateDeviceTokenAsync(request.UserId, request.DeviceInfo);

            return Ok(new
            {
                status = result,
                message = result ? "Device token saqlandi" : "Xatolik yuz berdi",
                data = (object?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving device token for user {UserId}", request.UserId);
            return StatusCode(500, new
            {
                status = false,
                message = "Internal server error",
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// Logout - tokenni bekor qilish (blacklist'ga qo'shish)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            // Authorization header'dan JWT token olish
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new
                {
                    status = false,
                    message = "Authorization header not found",
                    data = (object?)null
                });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();



            // Tokenni blacklist'ga qo'shish


            return Ok(new
            {
                status = true,
                message = "Muvaffaqiyatli logout qilindi",
                data = (object?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new
            {
                status = false,
                message = "Internal server error",
                data = (object?)null
            });
        }
    }
}

// Request DTOs
public class VerifyNumberRequest
{
    [JsonProperty("phone")]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class SendOtpRequest
{
    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class VerifyOtpRequest
{
    [JsonProperty("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonProperty("otp_code")]
    public string OtpCode { get; set; } = string.Empty;
}

public class SaveDeviceTokenRequest
{
    [JsonProperty("user_id")]
    public int UserId { get; set; }

    [JsonProperty("device_info")]
    public DeviceInfoDto DeviceInfo { get; set; } = null!;
}
