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
    private readonly ILogger<AuthController> _logger;
    private readonly ITokenService _tokenService;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, ITokenService tokenService)
    {
        _authService = authService;
        _logger = logger;
        _tokenService = tokenService;
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
    /// Returns: user info + roles + grouped permissions (Flutter-friendly format)
    /// </summary>
    [HttpGet("me")]
    [Authorize]
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
                    message = "Authorization header not found",
                    data = (object?)null
                });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            var result = await _authService.GetMeAsync(token);

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

            // User ID olish
            var userId = _tokenService.GetUserIdFromClaims(User);
            if (userId == null)
            {
                return Unauthorized(new
                {
                    status = false,
                    message = "Invalid token",
                    data = (object?)null
                });
            }

            // Tokenni blacklist'ga qo'shish
            var success = await _tokenService.BlacklistTokenAsync(token, userId.Value, "logout");

            if (!success)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "Logout qilishda xatolik yuz berdi",
                    data = (object?)null
                });
            }

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
    [JsonProperty("phone_number")]
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
