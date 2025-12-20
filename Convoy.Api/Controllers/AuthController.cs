using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Convoy.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Telefon raqamni tekshirish (PHP API orqali)
    /// </summary>
    [HttpPost("verify_number")]
    public async Task<IActionResult> VerifyNumber([FromBody] VerifyNumberRequest request)
    {
        try
        {
            var result = await _authService.VerifyNumberAsync(request.PhoneNumber);

            if (!result.Status)
            {
                return BadRequest(new { error = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying phone number: {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new { error = "Internal server error" });
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

            if (!result.Status)
            {
                return BadRequest(new { error = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP to: {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new { error = "Internal server error" });
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

            if (!result.Status)
            {
                return BadRequest(new { error = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP for: {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Joriy foydalanuvchi ma'lumotlarini olish (JWT token orqali)
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        try
        {
            // JWT token'dan user_id olish
            var userIdClaim = User.FindFirst("user_id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var result = await _authService.GetMeAsync(userIdClaim);

            if (!result.Status)
            {
                return BadRequest(new { error = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

// Request DTOs
public class VerifyNumberRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
}

public class SendOtpRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
}

public class VerifyOtpRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
}
